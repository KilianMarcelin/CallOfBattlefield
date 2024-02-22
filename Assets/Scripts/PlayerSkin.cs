using System;
using UnityEngine;
using Mirror;

public class PlayerSkin : NetworkBehaviour
{
    [Header("Meshes")] public GameObject arms;
    public GameObject body;
    public WeaponPrefabList weaponPrefabList;

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
    private Vector3 originalAmrsPos;
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
        originalAmrsPos = arms.transform.localPosition;
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
            arms.transform.localPosition = originalAmrsPos + new Vector3(swayOffset.x,
                swayOffset.y + Mathf.Sin(Time.time * breathSpeed) * breathStrength, -recoil) * swayStrength;
        }
        else
        {
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