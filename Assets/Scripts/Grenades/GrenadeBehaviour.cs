﻿using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class GrenadeBehaviour : NetworkBehaviour
{
    public EffectPrefabList effects;
    [SyncVar]
    public Grenade grenade;
    
    [SyncVar]
    private bool exploded = false;
    
    [SyncVar]
    private float timer = 0;
    
    [Server]
    public void SetForce(Vector3 force)
    {
        GetComponent<Rigidbody>().AddForce(force, ForceMode.VelocityChange);
    }

    [Server]
    public void SetGrenade(Grenade grenade)
    {
        this.grenade = grenade;
    }

    public void Update()
    {
        if (!isServer) return;
        
        timer += Time.deltaTime;

        if (timer >= grenade.fuseTime && !exploded)
        {
            Explode();
        }
    }

    public void OnCollisionEnter(Collision other)
    {
        if (!isServer) return;
        
        if (grenade.impactDetonate && !exploded)
        {
            Explode();
        }
    }

    [Server]
    public void Explode()
    {
        exploded = true;
        RpcPlayExplosion(transform.position);
        Collider[] others = Physics.OverlapSphere(transform.position, grenade.damageRange);
        
        foreach (Collider col in others)
        {
            PlayerScript ps;
            if (col.TryGetComponent(out ps))
            {
                ps.ServerDamage(grenade.damage);
            }
        }
        
        // Destorying instantly result in an explosion not playing
        Invoke(nameof(Destroy), 0.01f);
    }

    [Server]
    void Destroy()
    {
        NetworkServer.Destroy(this.gameObject);
    }

    [ClientRpc]
    public void RpcPlayExplosion(Vector3 pos)
    {
        Debug.Log("Explode!");
        GameObject fx = Instantiate(effects.explosionEffects[grenade.explosionEffect]);
        fx.transform.position = pos;
    }
}