using System;
using UnityEngine;
using Mirror;

public class PlayerSkin : NetworkBehaviour
{
    [Header("Meshes")] public GameObject arms;
    public GameObject body;
    public WeaponPrefabList weaponPrefabList;
    public Animator animator;
    public Animator fpAnimator;

    [Header("Layers")] public String armsLayer;
    public String hiddenLayer;

    [Header("Slots")] public GameObject gunSlotFP;
    public GameObject gunSlotTP;

    [Header("FX")] public ParticleSystem shootFX;
    public GameObject hitParticleFX;
    public GameObject bloodParticleFX;
    public Light shootLight;

    [Header("Animation params")] public float swayOffsetRate = 0.1f;
    public float swayStrength = 0.1f;
    public float breathSpeed = 1.0f;
    public float breathStrength = 0.1f;
    public float recoilAmount = 0.1f;

    [Header("UI")] public PlayerUI playerUI;

    private float health = 0;
    private WeaponBehaviour weaponBehaviour;
    private float recoil = 0;
    private Vector3 originalArmsPos;
    private Vector3 targetArmsPos;
    private Quaternion targetArmsRot;
    private Vector2 swayOffset = Vector2.zero;

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

    [Client]
    public void ClientStartRun()
    {
        animator.SetLayerWeight(1, 0.0f);

        if (isOwned)
        {
            targetArmsRot = Quaternion.Euler(0, -36, 0);
        }
    }

    [Client]
    public void ClientStopRun()
    {
        animator.SetLayerWeight(1, 1.0f);

        if (isOwned)
        {
            targetArmsRot = Quaternion.Euler(0, 0, 0);
        }
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

    private void Update()
    {
        if (isOwned)
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            swayOffset.x = Mathf.Lerp(swayOffset.x, mouseX, swayOffsetRate);
            swayOffset.y = Mathf.Lerp(swayOffset.y, mouseY, swayOffsetRate);

            recoil = Mathf.Lerp(recoil, 0, 0.1f);
            arms.transform.localPosition = originalArmsPos + new Vector3(swayOffset.x,
                swayOffset.y + Mathf.Sin(Time.time * breathSpeed) * breathStrength, -recoil) * swayStrength;
            arms.transform.localRotation = Quaternion.Lerp(arms.transform.localRotation, targetArmsRot, 0.1f);
        }
    }

    // Not really necessary
    [Client]
    public void HealthChanged(float health)
    {
        playerUI.UpdateHealth(health);
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

    [Client]
    public void PlayShootAnimation()
    {
        shootLight.enabled = true;
        shootFX.transform.position = weaponBehaviour.fxSlot.transform.position;
        shootFX.time = 0.0f;
        shootFX.Play();
        Invoke(nameof(TurnOffLight), 0.02f);
        recoil += recoilAmount;
    }

    [Client]
    public void PlayReloadAnimation(float reloadDuration)
    {
        Debug.Log("Reloading: " + reloadDuration);
        animator.SetFloat("oneOverReloadDuration", 1.0f/reloadDuration);
        animator.SetBool("reloading", true);

        if (isOwned)
        {
            fpAnimator.SetFloat("oneOverReloadDuration", 1.0f/reloadDuration);
            fpAnimator.SetBool("reloading", true);
        }
    }

    [Client]
    public void FinishedReloadAnimation()
    {
        animator.SetBool("reloading", false);
        if (isOwned)
        {
            fpAnimator.SetBool("reloading", false);
        }
    }

    [Client]
    public void ClientPlayHitFX(Vector3 position)
    {
        GameObject hitFX = Instantiate(hitParticleFX);
        hitFX.transform.position = position;
        Destroy(hitFX, 1.0f);
    }
    
    [Client]
    public void ClientPlayBloodFX(Vector3 position)
    {
        GameObject bloodFX = Instantiate(bloodParticleFX);
        bloodFX.transform.position = position;
        Destroy(bloodFX, 1.0f);
    }
    
    public void TurnOffLight()
    {
        shootLight.enabled = false;
    }

    [Client]
    public void SpawnCallback()
    {
        ShowModels();
    }

    [Client]
    public void DieCallback()
    {
        HideModels();
    }
}