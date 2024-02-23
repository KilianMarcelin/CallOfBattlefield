using System;
using Mirror;
using UnityEngine;
using UnityEngine.Events;

public class PlayerState : NetworkBehaviour
{
    public Weapon[] weapons;
    public float respawnTime = 5;
    
    [SyncVar] public bool isDed = false;
    [SyncVar] public float health = 100f;
    [SyncVar] public int currentWeapon = 0;

    [SyncVar] public float timeUntilShoot = 0;
    [SyncVar] public float timeUntilReloadEnd = 0;
    [SyncVar] private float timeUntilRespawn = 0;

    public Rigidbody rb;
    public CapsuleCollider collider;

    public UnityEvent OnDeathServerCallbacks;
    public UnityEvent OnDeathClientCallbacks;
    public UnityEvent<float> OnHealthChangeServerCallbacks;
    public UnityEvent<float> OnHealthChangeClientCallbacks;
    public UnityEvent OnRespawnServerCallbacks;
    public UnityEvent OnRespawnClientCallbacks;
    
    public UnityEvent<Weapon> OnWeaponChangeServerCallbacks;
    public UnityEvent<Weapon> OnWeaponChangeClientCallbacks;

    public void Start()
    {
        if (isServer)
        {
            ServerRespawn();
            ServerChangeWeapon();
        }
        
        PlayerState[] playerStates = FindObjectsOfType<PlayerState>();
        foreach (PlayerState playerState in playerStates)
        {
            if (playerState != this)
            {
                playerState.ClientInvokeChangeWeaponCallbacks(playerState.currentWeapon);
            }
        }
    }

    [Server]
    public void ServerDamage(float damage)
    {
        health -= damage;
        // We need a rpc cause our Rpc call function can't take parameters.
        RpcRunClientInvokeHealthCallbacks(health);
        OnHealthChangeServerCallbacks.Invoke(health);
        if (health <= 0)
        {
            ServerDie();
        }
    }

    [Command]
    public void CmdKill()
    {
        Debug.Log("CmdKill");
        ServerDamage(1000000f);
    }
    
    // We need a rpc cause our Rpc call function can't take parameters.
    [ClientRpc]
    private void RpcRunClientInvokeHealthCallbacks(float _health)
    {
        ClientInvokeHealthCallbacks(_health);
    }

    [Client]
    private void ClientInvokeHealthCallbacks(float health)
    {
        OnHealthChangeClientCallbacks.Invoke(health);
    }
    
    [Server]
    public void ServerRespawn()
    {
        isDed = false;
        health = 100;
        RpcRunRemote(nameof(ClientInvokeRespawnCallbacks));
        RpcRunClientInvokeHealthCallbacks(health);
        OnRespawnServerCallbacks.Invoke();
    }
    
    [Client]
    private void ClientInvokeRespawnCallbacks()
    {
        OnRespawnClientCallbacks.Invoke();
    }

    [Server]
    public void ServerDie()
    {
        if (!isServer) return;

        isDed = true;
        timeUntilRespawn = respawnTime;
        RpcRunRemote(nameof(ClientInvokeDeathCallbacks));
        OnDeathServerCallbacks.Invoke();
    }
    
    [Client]
    private void ClientInvokeDeathCallbacks()
    {
        OnDeathClientCallbacks.Invoke();
    }
    
    [Server]
    public void ServerChangeWeapon(int weaponIndex = 0)
    {
        currentWeapon = weaponIndex;
        RpcClientInvokeChangeWeaponCallbacks(weaponIndex);
        OnWeaponChangeServerCallbacks.Invoke(weapons[weaponIndex]);
    }
    
    [ClientRpc]
    public void RpcClientInvokeChangeWeaponCallbacks(int weaponIndex)
    {
        ClientInvokeChangeWeaponCallbacks(weaponIndex);
    }
    
    [Client]
    public void ClientInvokeChangeWeaponCallbacks(int weaponIndex)
    {
        Debug.Log("Client change weapon to " + weaponIndex);
        OnWeaponChangeClientCallbacks.Invoke(weapons[weaponIndex]);
    }

    [Server]
    public void ServerSetCanCollide(bool canCollide)
    {
        collider.isTrigger = !canCollide;
    }

    [Server]
    public void ServerSetGravityEnabled(bool gravityEnabled)
    {
        rb.useGravity = gravityEnabled;
    }

    private void Update()
    {
        if (!isServer) return;

        if (isDed)
        {
            timeUntilRespawn -= Time.deltaTime;
            if (timeUntilRespawn <= 0)
            {
                ServerRespawn();
            }
        }
        else
        {
            timeUntilReloadEnd -= Time.deltaTime;
            timeUntilShoot -= Time.deltaTime;
        }
    }

    public bool CanShoot()
    {
        return timeUntilShoot <= 0 && !isDed;
    }

    public void HasShooted()
    {
        timeUntilShoot = 1 / weapons[currentWeapon].fireRate;
    }
    
    public Weapon GetCurrentWeapon()
    {
        return weapons[currentWeapon];
    }

    [Command]
    public void CmdRunRemote(String function)
    {
        Invoke(function, 0);
    }
    
    [ClientRpc]
    public void RpcRunRemote(String function)
    {
        Invoke(function, 0);
    }
    
    public void DebugPrint(String text)
    {
        Debug.Log(text);
    }
}