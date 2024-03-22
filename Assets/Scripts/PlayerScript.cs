using System;
using Aura2API;
using Mirror;
using UnityEngine;
using Random = UnityEngine.Random;

public class PlayerScript : NetworkBehaviour
{
    [Header("Movement")] public CharacterController cr;
    public float runSpeed = 7f;
    public float walkSpeed = 5f;
    public float jumpHeight = 4f;
    public float movementControl = 0.1f;
    public float airControl = 0.02f;
    [SyncVar] public bool canMove = true;
    [SyncVar] public bool isRunning = false;

    // Private movement
    private float inputX = 0, inputZ = 0;
    private float moveX = 0, moveZ = 0;
    public float velocityY = 0;
    private float animatorInputX = 0, animatorInputZ = 0;
    private float fpGlobalAnimatorInput = 0;

    [Header("Camera")] public float unzoomedFOV = 90.0f;
    public float zoomedFOV = 30.0f;
    public float zoomRate = 0.3f;
    public float unzoomed2ndCamFOV = 60.0f;
    public float zoomed2ndCamFOV = 40.0f;
    public Camera mainCamera;
    public Camera armsCamera;
    public Camera tPCamera;

    // Private camera
    private float verticalAngle = 0.0f;

    [Header("Animations")] public Animator animator;
    public Animator fpAnimator;
    public Animator fpGlobalAnimator;
    public float fpGlobalAninmatorChangeSpeed = 0.1f;
    public float ragdollLifetime = 60.0f;

    [Header("Meshes")] public GameObject arms;
    public GameObject body;
    public WeaponPrefabList weaponPrefabList;

    [Header("Layers")] public String armsLayer;
    public String hiddenLayer = "Hidden";

    [Header("Slots")] public GameObject gunSlotFP;
    public GameObject gunSlotTP;

    [Header("FX")] public ParticleSystem shootFX;
    public GameObject hitParticleFX;
    public GameObject bloodParticleFX;
    public Light shootLight;

    [Header("Animation params")] public float swayOffsetRate = 0.1f;
    public float swayStrength = 0.1f;
    public float recoilAmount = 0.1f;

    [Header("UI")] public PlayerUI playerUI;

    // Animation private
    private float recoilZ = 0;
    private Vector3 originalArmsPos;
    private Vector2 swayOffset = Vector2.zero;

    [Header("State")] public Weapon[] weapons;
    public Grenade[] grenades;
    [SyncVar] public float respawnTime = 5;
    public WeaponBehaviour weaponBehaviour;
    public GameObject grenadePrefab;

    [SyncVar] public bool isDed = false;
    [SyncVar] public float health = 100f;
    [SyncVar] public int currentWeapon = 0;
    [SyncVar] public int currentGrenade = 0;

    public float timeUntilShoot = 0;
    [SyncVar] private float timeUntilRespawn = 0;

    [Header("Interactions")] public LayerMask includeHit;
    [SyncVar] public bool canShoot = true;
    public int ammoUsed = 0;
    public float timeUntilReload = 0;
    public bool reloading = false;
    public Vector2 recoil;

    public override void OnStartAuthority()
    {
        mainCamera.gameObject.tag = "MainCamera";
        mainCamera.gameObject.SetActive(true);
        Cursor.lockState = CursorLockMode.Locked;
        if (QualitySettings.GetQualityLevel() == 0)
        {
            mainCamera.GetComponent<AuraCamera>().enabled = false;
        }
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

    private void Start()
    {
        ShowModels();
        originalArmsPos = arms.transform.localPosition;

        if (isServer)
        {
            ServerRespawn();
            ServerChangeWeapon();
        }

        // Fix for other players not being started, thus resulting in a crash
        PlayerScript[] playerStates = FindObjectsOfType<PlayerScript>();
        foreach (PlayerScript playerState in playerStates)
        {
            if (playerState != this)
            {
                playerState.ChangeWeaponSkin(playerState.weapons[playerState.currentWeapon]);
            }
        }
    }

    [Server]
    public void ServerChangeWeapon(int weaponIndex = 0)
    {
        currentWeapon = weaponIndex;
        RpcChangeWeapon(weaponIndex);
    }

    [ClientRpc]
    public void RpcChangeWeapon(int weaponIndex)
    {
        ChangeWeaponSkin(weapons[weaponIndex]);
    }

    [Client]
    public void ChangeWeaponSkin(Weapon w)
    {
        Debug.Log("Changed weapon skin");
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
        GameObject weapon = Instantiate(weaponPrefabList.weaponPrefabs[w.weaponModelIndex]);
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

    [Server]
    public void ServerDamage(float damage)
    {
        if (isDed) return;

        health -= damage;
        // We need a rpc cause our Rpc call function can't take parameters.
        RpcHealthChanged(health);
        if (health <= 0)
        {
            ServerDie();
        }
    }

    [ClientRpc]
    public void RpcHealthChanged(float health)
    {
        if (isOwned)
            playerUI.UpdateHealth(health);
    }

    [Server]
    public void ServerRespawn()
    {
        isDed = false;
        health = 100;
        RpcHealthChanged(health);
        RpcResetPos();
        canMove = true;
        canShoot = true;
        RpcRespawn();
    }

    [ClientRpc]
    public void RpcResetPos()
    {
        transform.position = NetworkManager.singleton.GetStartPosition().position;
        moveX = 0;
        moveZ = 0;
    }

    [Server]
    public void ServerDie()
    {
        RpcDie();
        RpcCreateRagdoll();
        isDed = true;
        timeUntilRespawn = respawnTime;
        canMove = false;
        canShoot = false;
    }

    [ClientRpc]
    public void RpcCreateRagdoll()
    {
        GameObject ragdoll = Instantiate(body);
        ragdoll.transform.position = body.transform.position;
        ragdoll.transform.rotation = body.transform.rotation;
        ragdoll.SetActive(true);
        SetGameLayerRecursive(ragdoll, LayerMask.NameToLayer("Default"));
        ragdoll.GetComponent<NetworkAnimator>().enabled = false;
        ragdoll.GetComponent<Animator>().enabled = false;
        ragdoll.GetComponentInChildren<Rigidbody>().isKinematic = false;

        Vector3 force = transform.rotation * new Vector3(moveX / Time.deltaTime, velocityY, moveZ / Time.deltaTime) * 10.0f;
        ragdoll.GetComponentInChildren<Rigidbody>().AddForce(force, ForceMode.VelocityChange);
        Destroy(ragdoll, ragdollLifetime);
    }

    [ClientRpc]
    public void RpcDie()
    {
        HideModels();
        cr.enabled = false;
        tPCamera.enabled = true;
        mainCamera.enabled = false;
    }

    [Command]
    public void CmdKill()
    {
        Debug.Log("CmdKill");
        ServerDamage(1000000f);
    }

    [Client]
    public void ShowModels()
    {
        if (isOwned)
        {
            arms.SetActive(true);
            SetGameLayerRecursive(shootFX.gameObject, LayerMask.NameToLayer(armsLayer));
            SetGameLayerRecursive(body.gameObject, LayerMask.NameToLayer(hiddenLayer));
        }
        else
        {
            SetGameLayerRecursive(shootFX.gameObject, LayerMask.NameToLayer("Default"));
            SetGameLayerRecursive(body.gameObject, LayerMask.NameToLayer("Default"));
            body.SetActive(true);
        }
    }

    [ClientRpc]
    public void RpcRespawn()
    {
        cr.enabled = true;
        tPCamera.enabled = false;
        mainCamera.enabled = true;
        ShowModels();
    }

    [Client]
    public void HideModels()
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

    [Command]
    public void CmdSetCanShoot(bool value)
    {
        canShoot = value;
    }

    private void Update()
    {
        if (isOwned)
        {
            //
            // Movement
            //
            if (canMove)
            {
                // Calc input values
                bool localIsRunning = Input.GetButton("Run");

                float newInputX = Input.GetAxis("Horizontal");
                float newInputZ = Input.GetAxis("Vertical");

                inputX = Mathf.Lerp(inputX, newInputX, cr.isGrounded ? movementControl : airControl);
                inputZ = Mathf.Lerp(inputZ, newInputZ, cr.isGrounded ? movementControl : airControl);

                // CmdUpdateWalkValue(Mathf.Sqrt(inputX*inputX + inputZ*inputZ) * (localIsRunning ? 2.0f : 1.0f));

                // Update the fp global animator (breathing/walk/run)
                fpGlobalAnimatorInput = Mathf.Lerp(fpGlobalAnimatorInput,
                    // Min so the input direction is never greater than 1
                    Mathf.Min(Mathf.Sqrt(inputX * inputX + inputZ * inputZ), 1f)
                    * (localIsRunning ? 2.0f : 1.0f)
                    * (cr.isGrounded ? 1.0f : 0.0f),
                    fpGlobalAninmatorChangeSpeed);

                fpGlobalAnimator.SetFloat("speed", fpGlobalAnimatorInput);

                // Calc movement values
                moveX = inputX * Time.deltaTime * (localIsRunning ? runSpeed : walkSpeed);
                moveZ = inputZ * Time.deltaTime * (localIsRunning ? runSpeed : walkSpeed);

                // moveX = Mathf.Lerp(moveX, newMoveX, isInAir ? airControl : groundControl);
                // moveZ = Mathf.Lerp(moveZ, newMoveZ, isInAir ? airControl : groundControl);

                // Update the third person animator (will automatically be updated on remote clients)
                animatorInputX = Mathf.Lerp(animatorInputX, -inputX * (localIsRunning ? 1.5f : 1.0f), movementControl);
                animatorInputZ = Mathf.Lerp(animatorInputZ, inputZ * (localIsRunning ? 1.5f : 1.0f), movementControl);

                animator.SetFloat("forward", animatorInputZ);
                animator.SetFloat("left", animatorInputX);
                animator.SetBool("isInAir", !cr.isGrounded);

                // Jumping
                if (cr.isGrounded && Input.GetButtonDown("Jump"))
                {
                    velocityY = jumpHeight;
                }
                else if (cr.isGrounded)
                {
                    velocityY = -1f;
                }
                else
                {
                    velocityY += Physics.gravity.y * Time.deltaTime;
                }

                // Apply movement
                Vector3 movement = new Vector3(moveX, velocityY * Time.deltaTime, moveZ);
                movement = transform.rotation * movement;
                cr.Move(movement);
            }

            //
            // Skin
            //
            {
                float mouseX = Input.GetAxis("Mouse X");
                float mouseY = Input.GetAxis("Mouse Y");

                swayOffset.x = Mathf.Lerp(swayOffset.x, -mouseX, swayOffsetRate);
                swayOffset.y = Mathf.Lerp(swayOffset.y, -mouseY, swayOffsetRate);

                recoilZ = Mathf.Lerp(recoilZ, 0, 0.1f);
                arms.transform.localPosition = originalArmsPos +
                                               new Vector3(swayOffset.x, swayOffset.y) * swayStrength +
                                               new Vector3(0, 0, -recoilZ);
            }

            //
            // Interactions
            //
            {
                recoil = Vector2.Lerp(recoil, Vector2.zero, weapons[currentWeapon].recoilRecovery);
                timeUntilShoot -= Time.deltaTime;
                if (reloading)
                {
                    timeUntilReload -= Time.deltaTime;
                    if (timeUntilReload <= 0)
                    {
                        ammoUsed = 0;
                        reloading = false;
                        CmdFinishedReload();
                    }
                }

                if (Input.GetButtonDown("Power word kill") && !isDed)
                {
                    CmdKill();
                }

                if (Input.GetButtonDown("Reload") && !isDed && !reloading && ammoUsed > 0)
                {
                    timeUntilReload = weapons[currentWeapon].reloadTime;
                    reloading = true;
                    CmdReload();
                }

                if (Input.GetButton("Fire1") && !isRunning && ammoUsed < weapons[currentWeapon].maxAmmo && !reloading &&
                    canShoot)
                {
                    Ray ray = mainCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
                    // ClientPlayShootAnimation();
                    ClientShoot(ray.origin, ray.direction);
                    recoil = new Vector2(
                        weapons[currentWeapon].recoilCurve.Evaluate(recoil.y +
                                                                    weapons[currentWeapon].recoilUp),
                        recoil.y + weapons[currentWeapon].recoilUp);
                }
            }

            //
            // Camera
            //
            {
                float mouseX = Input.GetAxis("Mouse X");
                float mouseY = Input.GetAxis("Mouse Y");

                verticalAngle += mouseY;
                if (verticalAngle < -90f) verticalAngle = -90f;
                else if (verticalAngle > 90f) verticalAngle = 90f;

                animator.SetFloat("aim", verticalAngle / 90f);

                // Camera rotation 2
                mainCamera.transform.localRotation = Quaternion.AngleAxis(verticalAngle + recoil.y, Vector3.left);
                // transform.Rotate(0, mouseX, 0);
                transform.Rotate(0, mouseX + recoil.x, 0);

                if (Input.GetButton("Fire2"))
                {
                    mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, zoomedFOV, zoomRate);
                    armsCamera.fieldOfView = Mathf.Lerp(armsCamera.fieldOfView, zoomed2ndCamFOV, zoomRate);
                }
                else
                {
                    mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, unzoomedFOV, zoomRate);
                    armsCamera.fieldOfView = Mathf.Lerp(armsCamera.fieldOfView, unzoomed2ndCamFOV, zoomRate);
                }
            }
        }

        if (isServer)
        {
            if (isDed)
            {
                timeUntilRespawn -= Time.deltaTime;
                if (timeUntilRespawn <= 0)
                {
                    ServerRespawn();
                }
            }
        }
    }

    [ClientRpc]
    public void RpcFinishedReloadAnimation()
    {
        animator.SetBool("reloading", false);
        if (isOwned)
        {
            fpAnimator.SetBool("reloading", false);
        }
    }

    [Command]
    public void CmdReload()
    {
        RpcPlayReloadAnimation(weapons[currentWeapon].reloadTime);
    }

    [Command]
    public void CmdFinishedReload()
    {
        RpcFinishedReloadAnimation();
    }

    [ClientRpc]
    public void RpcPlayReloadAnimation(float reloadDuration)
    {
        Debug.Log("Reloading: " + reloadDuration);
        animator.SetFloat("oneOverReloadDuration", 1.0f / reloadDuration);
        animator.SetBool("reloading", true);

        if (isOwned)
        {
            fpAnimator.SetFloat("oneOverReloadDuration", 1.0f / reloadDuration);
            fpAnimator.SetBool("reloading", true);
        }
    }

    [ClientRpc]
    public void RpcPlayShootAnimation()
    {
        if (isOwned) return;

        shootLight.enabled = true;
        shootFX.transform.position = weaponBehaviour.fxSlot.transform.position;
        shootFX.time = 0.0f;
        shootFX.Play();
        Invoke(nameof(TurnOffLight), 0.02f);
        recoilZ += recoilAmount;
    }

    [Command]
    public void CmdPlayRemoteShootAnim()
    {
        RpcPlayShootAnimation();
    }

    [Client]
    public void ClientPlayShootAnimation()
    {
        shootLight.enabled = true;
        shootFX.transform.position = weaponBehaviour.fxSlot.transform.position;
        shootFX.time = 0.0f;
        shootFX.Play();
        Invoke(nameof(TurnOffLight), 0.02f);
        recoilZ += recoilAmount;
    }

    // Needed for coroutine
    public void TurnOffLight()
    {
        shootLight.enabled = false;
    }

    [Client]
    public void ClientShoot(Vector3 origin, Vector3 direction)
    {
        if (!canShoot || timeUntilShoot > 0) return;

        timeUntilShoot = 1 / weapons[currentWeapon].fireRate;
        ammoUsed++;

        ClientPlayShootAnimation();
        CmdPlayRemoteShootAnim();

        RaycastHit[] hits;
        hits = Physics.RaycastAll(origin, direction, 100f, includeHit);

        // Sort hits by distance
        Array.Sort(hits, ((hit, raycastHit) => { return (int)(hit.distance - raycastHit.distance); }));

        foreach (RaycastHit hit in hits)
        {
            PlayerScript player = GetPlayerScriptInParent(hit.transform);

            // If we hit a player and it isn't us
            if (player != null && player != this)
            {
                if (player.isDed) continue;

                player.CmdDamage(weapons[currentWeapon].damage);
                // Play for all remote client and then yourself
                playerUI.Hit();
                CmdPlayBloodFX(hit.point);
                ClientPlayBloodFX(hit.point);
                break;
            }

            // If we didn't hit a player
            if (player == null)
            {
                // Play for all remote client and then yourself
                // playerUI.Hit();

                CmdPlayHitFX(hit.point + hit.normal * 0.1f);
                ClientPlayHitFX(hit.point + hit.normal * 0.1f);
                break;
            }

            // we're here if we hit ourselves, so we get to the next hit
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdDamage(float damage)
    {
        ServerDamage(damage);
    }

    [Command]
    public void CmdPlayHitFX(Vector3 position)
    {
        RpcPlayHitFX(position);
    }

    [ClientRpc]
    public void RpcPlayHitFX(Vector3 position)
    {
        if (isOwned) return;

        ClientPlayHitFX(position);
    }

    [Client]
    public void ClientPlayHitFX(Vector3 position)
    {
        GameObject hitFX = Instantiate(hitParticleFX);
        hitFX.transform.position = position;
        Destroy(hitFX, 1.0f);
    }

    [Command]
    public void CmdPlayBloodFX(Vector3 position)
    {
        RpcPlayBloodFX(position);
    }

    [ClientRpc]
    public void RpcPlayBloodFX(Vector3 position)
    {
        if (isOwned) return;

        ClientPlayBloodFX(position);
    }

    [Client]
    public void ClientPlayBloodFX(Vector3 position)
    {
        GameObject bloodFX = Instantiate(bloodParticleFX);
        bloodFX.transform.position = position;
        Destroy(bloodFX, 1.0f);
    }

    private PlayerScript GetPlayerScriptInParent(Transform go)
    {
        if (go == null) return null;

        PlayerScript ps = go.GetComponent<PlayerScript>();

        if (ps != null) return ps;

        return GetPlayerScriptInParent(go.transform.parent);
    }
}