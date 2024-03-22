using System;
using Mirror;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "grenade", menuName = "ScriptableObjects/Grenade", order = 2)]
public class Grenade : ScriptableObject
{
    public float damage = 10f;
    public float damageRange = 10f;
    public float throwForce = 10f;
    public float reloadTime = 10f;
    public float fuseTime = 3f;
    public bool impactDetonate = true;
    public GameObject explosionEffect;
}