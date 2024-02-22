using System;
using Mirror;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "weapon", menuName = "ScriptableObjects/Weapon", order = 1)]
public class Weapon : ScriptableObject
{
    public float damage = 10f;
    public float range = 100f;
    public float fireRate = 15f;
    public float impactForce = 30f;
    public int maxAmmo = 10;
    public float reloadTime = 1f;
    public int weaponModelIndex;
    public bool fullAuto = true;
}

// public static class WeaponExtensions
// {
//     public static void WriteWeapon(this NetworkWriter writer, Weapon value)
//     {
//         writer.WriteFloat(value.damage);
//         writer.WriteFloat(value.range);
//         writer.WriteFloat(value.fireRate);
//         writer.WriteFloat(value.impactForce);
//         writer.WriteInt(value.maxAmmo);
//         writer.WriteFloat(value.reloadTime);
//         writer.WriteBool(value.fullAuto);
//     }
//
//     public static Weapon ReadWeapon(this NetworkReader reader)
//     {
//         // read MyType data here
//     }
// }

