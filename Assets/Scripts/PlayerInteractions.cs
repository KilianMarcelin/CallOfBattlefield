using System;
using Mirror;
using UnityEngine;
using UnityEngine.Events;


[RequireComponent(typeof(PlayerState))]
public class PlayerInteractions : NetworkBehaviour
{
    PlayerState playerState;

    public Camera mainCamera;
    public UnityEvent<Vector3, Vector3> OnClientShoot;
    public UnityEvent<Vector3, Vector3> OnServerShoot; 
    
    [SyncVar] public bool canShoot = true;
    
    [Server]
    public void ServerSetCanShoot(bool value)
    {
        canShoot = value;
    }

    private void Start()
    {
        playerState = GetComponent<PlayerState>();
    }

    private void Update()
    {
        if (isOwned)
        {
            if (!playerState.isDed && Input.GetButtonDown("Power word kill"))
            {
                playerState.CmdKill();
            }
            
            if (canShoot && playerState.CanShoot() && Input.GetButton("Fire1"))
            {
                playerState.HasShooted();

                Debug.Log("Shooting");
                Ray ray = mainCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
                CmdShoot(ray.origin, ray.direction);
            }
        }
    }

    [ClientRpc]
    private void RpcInvokeOnClientShoot(Vector3 origin, Vector3 direction)
    {
        OnClientShoot.Invoke(origin, direction);
    }

    [Command]
    public void CmdShoot(Vector3 origin, Vector3 direction)
    {
        OnServerShoot.Invoke(origin, direction);
        RpcInvokeOnClientShoot(origin, direction);
        
        RaycastHit hit;
        //RpcDebugShowHitTrace(origin, direction, 100f);
        if (Physics.Raycast(origin, direction, out hit, 100f))
        {
            PlayerScript player = hit.transform.GetComponent<PlayerScript>();
            if (player != null && player != this)
            {
                player.ServerHit(playerState.GetCurrentWeapon().damage);
                // RpcPlayHitFXBlood(hit.point);
            }
            else
            {
                // RpcPlayHitFXHit(hit.point + hit.normal * 0.1f);
            }
        }
    }
}