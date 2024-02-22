using System;
using Mirror;
using UnityEngine;
using Random = UnityEngine.Random;

public class PlayerScript : NetworkBehaviour
{
    public Rigidbody rb;
    public float speed = 4f;
    public float airControl = 0.1f;
    public float jumpForce = .3f;
    public Animator animator;
    public Collider playerCollider;
    public SphereCollider groundCollider;

    public GameObject arms;
    public GameObject body;
    
    // Temp fix
    public GameObject weaponModel;
    
    public Camera mainCamera;
    public Camera armsCamera;
    public String armsLayer;
    public String hiddenLayer;
    public GameObject gunSlotFP;
    public GameObject gunSlotTP;
    public WeaponBehaviour weaponBehaviour;
    public ParticleSystem shootFX;
    public GameObject hitParticleFX;
    public GameObject bloodParticleFX;
    public Light shootLight;
    public float swayOffsetRate = 0.1f;
    public float swayStrength = 0.1f;
    public float breathSpeed = 1.0f;
    public float breathStrength = 0.1f;
    public float recoilAmount = 0.1f;
    public float unzoomedFOV = 90.0f;
    public float zoomedFOV = 30.0f;
    public float unzoomed2ndCamFOV = 60.0f;
    public float zoomed2ndCamFOV = 40.0f;
    public float zoomRate = 0.3f;
    
    private float recoil = 0.0f;
    public PlayerUI playerUI;
    private Vector2 swayOffset = Vector2.zero;
    private Vector3 originalAmrsPos;

    public float respawnTime = 5;
    [SyncVar] public bool isDed = false;
    [SyncVar(hook = nameof(OnHealthChanged))] public float health = 100f;

    [SyncVar] public int currentWeapon = 0;
    public Weapon[] weapons;

    [SyncVar] private float timeUntilShoot = 0;
    [SyncVar] private int ammoLeft = 0;

    private int isInAir_count = 0;

    private bool isInAir
    {
        get
        {
            return isInAir_count <= 0;
        }
    }

    private float moveX = 0, moveZ = 0;

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
        // Fix because sync var doesn't seem to work.
        isDed = true;
        rb.useGravity = false;
        playerCollider.enabled = false;
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
        // Fix because sync var doesn't seem to work.
        rb.useGravity = false;
        playerCollider.enabled = false;
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
        // Fix because sync var doesn't seem to work.
        isDed = false;
        rb.useGravity = true;
        playerCollider.enabled = true;
        transform.position = NetworkManager.singleton.GetStartPosition().position;
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
        // Fix because sync var doesn't seem to work.
        isDed = false;
        rb.useGravity = true;
        playerCollider.enabled = true;
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
        isInAir_count += 1;
    }

    private void OnTriggerExit(Collider other)
    {
        isInAir_count -= 1;
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
        ammoLeft = weapons[index].maxAmmo;
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
        
        ammoLeft = weapons[index].maxAmmo;

        // Add new weapon
        GameObject weapon = Instantiate(weaponModel);
        if (!isOwned)
        {
            weapon.transform.SetParent(gunSlotTP.transform);
        }
        else
        {
            weapon.transform.SetParent(gunSlotFP.transform);
            SetGameLayerRecursive(weapon, LayerMask.NameToLayer("ArmsLayer"));
        }

        weapon.transform.localPosition = Vector3.zero;
        weapon.transform.localEulerAngles = Vector3.zero;

        weaponBehaviour = weapon.GetComponent<WeaponBehaviour>();
        shootFX.transform.SetParent(weaponBehaviour.fxSlot.transform);
    }

    void Update()
    {
        Debug.Log(isDed);
        if (isOwned && !isDed)
        {
            // Movement
            if (!isInAir)
            {
                moveX = Input.GetAxis("Horizontal");
                moveZ = Input.GetAxis("Vertical");
            
                moveX *= Time.deltaTime * speed;
                moveZ *= Time.deltaTime * speed;
            }
            else
            {
                // Inertia
                moveX += Input.GetAxis("Horizontal") * airControl * Time.deltaTime * speed;
                moveZ += Input.GetAxis("Vertical") * airControl * Time.deltaTime * speed;
            }

            // Update animator
            animator.SetFloat("forward", Input.GetAxis("Vertical"));
            animator.SetFloat("left", -Input.GetAxis("Horizontal"));
            animator.SetBool("isInAir", isInAir);

            // Mouse input
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            // Procedural arms sway and other stuff
            swayOffset.x = Mathf.Lerp(swayOffset.x, mouseX, swayOffsetRate);
            swayOffset.y = Mathf.Lerp(swayOffset.y, mouseY, swayOffsetRate);
            
            recoil = Mathf.Lerp(recoil, 0, 0.1f);
            arms.transform.localPosition = originalAmrsPos + new Vector3(swayOffset.x, swayOffset.y + Mathf.Sin(Time.time * breathSpeed) * breathStrength, -recoil) * swayStrength;

            
            transform.Translate(moveX, 0, moveZ);

            // Clamping
            verticalAngle += mouseY;
            if (verticalAngle < -90f) verticalAngle = -90f;
            else if (verticalAngle > 90f) verticalAngle = 90f;

            // Update aim anim
            animator.SetFloat("aim", verticalAngle / 90f);

            // Camera rotation 2
            mainCamera.transform.localRotation = Quaternion.AngleAxis(verticalAngle, Vector3.left);
            transform.Rotate(0, mouseX, 0);

            // Jumping
            if (!isInAir && Input.GetButtonDown("Jump"))
            {
                rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
            }

            // Shooting
            if (timeUntilShoot <= 0 && Input.GetButton("Fire1"))
            {
                Vector3 origin = mainCamera.transform.position;
                Vector3 direction = mainCamera.transform.forward;
                CmdShoot(origin, direction);
                recoil += recoilAmount;
            }

            if (Input.GetButton("Fire2"))
            {
                mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, zoomedFOV, zoomRate);
                armsCamera.fieldOfView = Mathf.Lerp(armsCamera.fieldOfView, zoomed2ndCamFOV, zoomRate);
            } else {
                mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, unzoomedFOV, zoomRate);
                armsCamera.fieldOfView = Mathf.Lerp(armsCamera.fieldOfView, unzoomed2ndCamFOV, zoomRate);
            } 

            // kill
            if (Input.GetButton("Power word kill"))
            {
                Debug.Log("You should kill yourself, NOW!");
                CmdCallServerHit(10f);
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
        //RpcDebugShowHitTrace(origin, direction, 100f);
        if (Physics.Raycast(origin, direction, out hit, 100f))
        {
            PlayerScript player = hit.transform.GetComponent<PlayerScript>();
            if (player != null && player != this)
            {
                player.ServerHit(weapons[currentWeapon].damage);
                RpcPlayHitFXBlood(hit.point);
            }
            else
            {
                RpcPlayHitFXHit(hit.point + hit.normal * 0.1f);
            }
        }
    }

    [ClientRpc]
    void RpcPlayHitFXBlood(Vector3 position)
    {
        Instantiate(bloodParticleFX, position, Quaternion.identity);
    }
    
    [ClientRpc]
    void RpcPlayHitFXHit(Vector3 position)
    {
        Instantiate(hitParticleFX, position, Quaternion.identity);
    }

    [ClientRpc]
    void RpcDebugShowHitTrace(Vector3 origin, Vector3 direction, float distance)
    {
        Debug.DrawLine(origin, origin + direction * distance, Color.red, 1f);
    }

    [ClientRpc]
    public void RpcPlayShootAnim()
    {
        //animator.SetTrigger("shoot");
        shootLight.enabled = true;
        shootFX.transform.position = weaponBehaviour.fxSlot.transform.position;
        shootFX.time = 0.0f;
        shootFX.Play();
        Invoke(nameof(TurnOffLight), 0.02f);
    }

    public void TurnOffLight()
    {
        shootLight.enabled = false;
    }
    
    public void OnHealthChanged(float oldHealth, float newHealth)
    {
        Debug.Log("Health changed from " + oldHealth + " to " + newHealth);
        if (isOwned)
        {
            playerUI.UpdateHealth(newHealth);
        }
    }
}