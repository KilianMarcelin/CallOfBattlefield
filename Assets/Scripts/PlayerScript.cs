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
    public SphereCollider groundCollider;

    public GameObject arms;
    public GameObject body;

    public Camera mainCamera;
    public Camera armsCamera;
    public String armsLayer;
    public String hiddenLayer;
    public GameObject gunSlotFP;
    public GameObject gunSlotTP;
    public WeaponBehaviour weaponBehaviour;
    public ParticleSystem shootFX;
    public float swayOffsetRate = 0.1f;
    public float swayStrength = 0.1f;
    public float breathSpeed = 1.0f;
    public float breathStrength = 0.1f;
    public float recoilAmount = 0.1f;
    private float recoil = 0.0f;
    private Vector2 swayOffset = Vector2.zero;
    private Vector3 originalAmrsPos;

    public float respawnTime = 5;
    [SyncVar] public bool isDed = false;
    [SyncVar] public float health = 100f;

    [SyncVar] public int currentWeapon = 0;
    public Weapon[] weapons;

    [SyncVar] private float timeUntilShoot = 0;

    private bool isInAir = false;

    private float verticalAngle = 0.0f;

    [SyncVar] private float timeUntilRespawn = 0;

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
        shootFX.transform.SetParent(null);
        if (isOwned)
        {
            arms.SetActive(true);
            
            // Set main camera
            mainCamera.gameObject.tag = "MainCamera";
            mainCamera.gameObject.SetActive(true);

            // Camera.main.transform.SetParent(transform);
            // Camera.main.transform.localPosition = new Vector3(0, 1.5f, 0);
            // Camera.main.cullingMask &= ~(1 << LayerMask.NameToLayer("ArmsLayer"));
            // armsCamera.transform.SetParent(Camera.main.transform);
            // armsCamera.transform.localPosition = Vector3.zero;
            // arms.transform.SetParent(Camera.main.transform);
            // arms.transform.localPosition = new Vector3(0, -1.65f, 0.11f);

            name = "LocalPlayer" + Random.Range(1, 10000);

            // Lock cursor
            Cursor.lockState = CursorLockMode.Locked;

            CmdCallChangeWeapon(0);

            PlayerScript[] players = FindObjectsOfType<PlayerScript>();
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i] != this)
                {
                    players[i].ClientChangeWeapon(players[i].currentWeapon);
                }
            }
            
            SetGameLayerRecursive(shootFX.gameObject, LayerMask.NameToLayer("ArmsLayer"));

            originalAmrsPos = arms.transform.localPosition;
            
            SetGameLayerRecursive(body.gameObject, LayerMask.NameToLayer(hiddenLayer));
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

    [Command]
    public void CmdCallChangeWeapon(int index)
    {
        ServerChangeWeapon(index);
    }

    [Server]
    public void ServerChangeWeapon(int index)
    {
        currentWeapon = index;
        RpcCallClientChangeWeapon(index);
    }

    [ClientRpc]
    public void RpcCallClientChangeWeapon(int index)
    {
        ClientChangeWeapon(index);
    }

    private void SetGameLayerRecursive(GameObject _go, int _layer)
    {
        _go.layer = _layer;
        foreach (Transform child in _go.transform)
        {
            child.gameObject.layer = _layer;
     
            Transform _HasChildren = child.GetComponentInChildren<Transform>();
            if (_HasChildren != null)
                SetGameLayerRecursive(child.gameObject, _layer);
                 
        }
    }

    
    [Client]
    public void ClientChangeWeapon(int index)
    {
        Debug.Log("Changed weapon to " + index);
        // Remove all childs
        foreach (Transform child in gunSlotFP.transform)
        {
            Destroy(child.gameObject);
        }

        foreach (Transform child in gunSlotTP.transform)
        {
            Destroy(child.gameObject);
        }

        // Add new weapon
        GameObject weapon = Instantiate(weapons[index].weaponModel);
        if (!isOwned) weapon.transform.SetParent(gunSlotTP.transform);
        else
        {
            weapon.transform.SetParent(gunSlotFP.transform);
            SetGameLayerRecursive(weapon, LayerMask.NameToLayer("ArmsLayer"));
        }

        weapon.transform.localPosition = Vector3.zero;
        weapon.transform.localEulerAngles = Vector3.zero;

        weaponBehaviour = weapon.GetComponent<WeaponBehaviour>();
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

            // Procedural arms sway and other stuff
            swayOffset.x = Mathf.Lerp(swayOffset.x, mouseX, swayOffsetRate);
            swayOffset.y = Mathf.Lerp(swayOffset.y, mouseY, swayOffsetRate);
            
            recoil = Mathf.Lerp(recoil, 0, 0.1f);
            arms.transform.localPosition = originalAmrsPos + new Vector3(swayOffset.x, swayOffset.y + Mathf.Sin(Time.time * breathSpeed) * breathStrength, -recoil) * swayStrength;

            transform.Rotate(0, moveX, 0);
            transform.Translate(moveX, 0, moveZ);


            verticalAngle += mouseY;
            if (verticalAngle < -90f) verticalAngle = -90f;
            else if (verticalAngle > 90f) verticalAngle = 90f;

            animator.SetFloat("aim", verticalAngle / 90f);

            mainCamera.transform.localRotation = Quaternion.AngleAxis(verticalAngle, Vector3.left);
            transform.Rotate(0, mouseX, 0);

            if (!isInAir && Input.GetButtonDown("Jump"))
            {
                rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
            }

            if (timeUntilShoot <= 0 && Input.GetButton("Fire1"))
            {
                Vector3 origin = mainCamera.transform.position;
                Vector3 direction = mainCamera.transform.forward;
                CmdShoot(origin, direction);
                recoil += recoilAmount;
            }

            if (Input.GetButton("Power word kill"))
            {
                Debug.Log("You should kill yourself, NOW!");
                CmdCallServerHit(100000f);
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

        RpcPlayShootAnim();

        timeUntilShoot = 1 / weapons[currentWeapon].fireRate;
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

    [ClientRpc]
    public void RpcPlayShootAnim()
    {
        //animator.SetTrigger("shoot");
        shootFX.transform.position = weaponBehaviour.fxSlot.transform.position;
        shootFX.time = 0.0f;
        shootFX.Play();
    }
}