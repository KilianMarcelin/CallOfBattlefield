using System;
using Mirror;
using UnityEngine;
using Random = UnityEngine.Random;

public class PlayerScript : NetworkBehaviour
{
    public Rigidbody rb;
    public float speed = 4f;
    public float jumpForce = .3f;
    public Animator animator;
    public GameObject arms;
    public GameObject body;
    public GameObject gunSlot;
    public SphereCollider groundCollider;
    public WeaponBehaviour weaponBehaviour;
    public float respawnTime = 5;

    public int currentWeapon = 0;
    public Weapon[] weapons;
        
    [SyncVar]
    private float timeUntilShoot = 0;
    
    [SyncVar] public bool isDed = false;
    [SyncVar] public float health = 100f;

    private bool hasJumped = false;
    private bool isInAir = false;

    private float verticalAngle = 0.0f;
    
    [SyncVar]
    private float timeUntilRespawn = 0;

    public override void OnStartAuthority()
    {
        ClientSpawn();
        CmdCallServerSpawn();
    }
    
    [Command]
    public void CmdCallServerHit(float damage)
    {
        ServerHit(damage);
    }
    
    [Server]
    public void ServerHit(float damage)
    {
        health -= damage;
        Debug.Log("Took damage, health: " + health);
        
        if (health <= 0)
        {
            ServerDie();
            RpcCallClientDie();
        }
    }
    

    [Client]
    public void ClientDie()
    {
        if (isOwned)
        {
            arms.SetActive(false);
        }
        else
        {
            body.SetActive(false);
        }
    }

    [Server]
    public void ServerDie()
    {
        isDed = true;
        timeUntilRespawn = respawnTime;
    }
    
    [ClientRpc]
    public void RpcCallClientDie()
    {
        ClientDie();
    }

    [Client]
    public void ClientSpawn()
    {
        if (isOwned)
        {
            animator.gameObject.SetActive(false);
            arms.SetActive(true);

            Camera.main.transform.SetParent(transform);
            Camera.main.transform.localPosition = new Vector3(0, 1.7f, 0);
            arms.transform.SetParent(Camera.main.transform);
            arms.transform.localPosition = new Vector3(0, -1.65f, 0.11f);

            name = "LocalPlayer" + Random.Range(1, 1000);

            // Lock cursor
            Cursor.lockState = CursorLockMode.Locked;

            ChangeWeapon(0);
        }
        else
        {
            body.SetActive(true);
        }
    }
    
    [Server]
    public void ServerSpawn()
    {
        isDed = false;
        transform.position = NetworkManager.singleton.GetStartPosition().position;
        health = 100f;
    }

    [ClientRpc]
    public void RpcCallClientSpawn()
    {
        ClientSpawn();
    }
    
    [Command]
    public void CmdCallServerSpawn()
    {
        ServerSpawn();
    }

    private void OnTriggerEnter(Collider other)
    {
        isInAir = false;
    }

    private void OnTriggerExit(Collider other)
    {
        isInAir = true;
    }

    public void ChangeWeapon(int index)
    {
        if (index < 0 || index >= weapons.Length) return;
        currentWeapon = index;
        // Remove all childs
        foreach (Transform child in gunSlot.transform)
        {
            Destroy(child.gameObject);
        }
        // Add new weapon
        GameObject weapon = Instantiate(weapons[currentWeapon].weaponModel, gunSlot.transform);
        weaponBehaviour = weapon.AddComponent<WeaponBehaviour>();
    }
    
    void Update()
    {
        if (isOwned && !isDed)
        {
            float moveX = Input.GetAxis("Horizontal");
            float moveZ = Input.GetAxis("Vertical");

            moveX *= Time.deltaTime * speed;
            moveZ *= Time.deltaTime * speed;

            animator.SetFloat("forward", Input.GetAxis("Vertical"));
            animator.SetFloat("left", -Input.GetAxis("Horizontal"));

            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            transform.Rotate(0, moveX, 0);
            transform.Translate(moveX, 0, moveZ);


            verticalAngle += mouseY;
            if (verticalAngle < -90f) verticalAngle = -90f;
            else if (verticalAngle > 90f) verticalAngle = 90f;

            animator.SetFloat("aim", verticalAngle / 90f);

            Camera.main.transform.localRotation = Quaternion.AngleAxis(verticalAngle, Vector3.left);
            transform.Rotate(0, mouseX, 0);
        
            if (!isInAir && Input.GetButtonDown("Jump"))
            {
                rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
            }

            if (timeUntilShoot <= 0 && Input.GetButton("Fire1"))
            {
                Vector3 origin = Camera.main.transform.position;
                Vector3 direction = Camera.main.transform.forward;
                CmdShoot(origin, direction);
            }

            if (Input.GetButton("Power word kill"))
            {
                Debug.Log("You should kill yourself, NOW!");
                CmdCallServerHit(1000f);
            }
        }

        if (isServer)
        {
            if (timeUntilShoot > 0) timeUntilShoot -= Time.deltaTime;
            
            // Respawning
            if (isDed && timeUntilRespawn > 0)
            {
                timeUntilRespawn -= Time.deltaTime;
                if (timeUntilRespawn <= 0)
                {
                    ServerSpawn();
                    RpcCallClientSpawn();
                }
            }
        }
    }

    [Command]
    public void CmdShoot(Vector3 origin, Vector3 direction)
    {
        if (timeUntilShoot > 0) return;
        
        timeUntilShoot = 1/weapons[currentWeapon].fireRate;
        RaycastHit hit;
        if (Physics.Raycast(origin, direction, out hit, 100f))
        {
            PlayerScript player = hit.transform.GetComponent<PlayerScript>();
            if (player != null)
            {
                player.ServerHit(10f);
            }
        }
    }
}