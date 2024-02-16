using System;
using Mirror;
using UnityEngine;

[CreateAssetMenu(fileName = "weapon", menuName = "ScriptableObjects/Weapon", order = 1)]
public class Weapon : ScriptableObject
{
    public float damage = 10f;
    public float range = 100f;
    public float fireRate = 15f;
    public float impactForce = 30f;
    public GameObject weaponModel;
}