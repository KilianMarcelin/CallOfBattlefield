using System;
using Mirror;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;


[RequireComponent(typeof(PlayerState))]
public class PlayerInteractions : NetworkBehaviour
{
    PlayerState playerState;

    [FormerlySerializedAs("ignoreHit")] public LayerMask includeHit;
    public Camera mainCamera;
    public UnityEvent<Vector3, Vector3> OnClientShoot;
    public UnityEvent<Vector3, Vector3> OnServerShoot;
    public UnityEvent<Vector3> OnClientHitFX;
    public UnityEvent<Vector3> OnClientBloodFX;

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

            if (canShoot && Input.GetButton("Fire1"))
            {
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

    [ClientRpc]
    private void RpcInvokeClientBloodFX(Vector3 origin)
    {
        OnClientBloodFX.Invoke(origin);
    }

    [ClientRpc]
    private void RpcInvokeClientHitFX(Vector3 origin)
    {
        OnClientHitFX.Invoke(origin);
    }

    [Command]
    public void CmdShoot(Vector3 origin, Vector3 direction)
    {
        // Better to check on the server
        if (!playerState.CanShoot()) return;

        playerState.HasShooted();

        OnServerShoot.Invoke(origin, direction);
        RpcInvokeOnClientShoot(origin, direction);

        RaycastHit[] hits;
        hits = Physics.RaycastAll(origin, direction, 100f, includeHit);
        
        // Sort hits by distance
        Array.Sort(hits, ((hit, raycastHit) =>
        {
            return (int) (hit.distance - raycastHit.distance);
        }));
        
        foreach (RaycastHit hit in hits)
        {
            Debug.Log(hit.distance);
            PlayerState player = GetPlayerStateInParent(hit.transform);

            // If we hit a player and it isn't us
            if (player != null && player != playerState)
            {
                player.ServerDamage(playerState.GetCurrentWeapon().damage);
                // RpcPlayHitFXBlood(hit.point);
                RpcInvokeClientBloodFX(hit.point + hit.normal * 0.1f);
                break;
            }
            
            // If we didn't hit a player
            if (player == null)
            {
                // RpcPlayHitFXHit(hit.point + hit.normal * 0.1f);
                RpcInvokeClientHitFX(hit.point);
                break;
            }
            
            // we're here if we hit ourselves, so we get to the next hit
        }
    }

    private PlayerState GetPlayerStateInParent(Transform go)
    {
        if (go == null) return null;

        PlayerState ps = go.GetComponent<PlayerState>();

        if (ps != null) return ps;

        return GetPlayerStateInParent(go.transform.parent);
    }
}