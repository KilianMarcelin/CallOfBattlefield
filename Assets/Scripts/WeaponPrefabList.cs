using UnityEngine;
using Mirror;

[CreateAssetMenu(fileName = "weaponPrefabList", menuName = "ScriptableObjects/WeaponPrefabList", order = 1)]
public class WeaponPrefabList : ScriptableObject
{
    public GameObject[] weaponPrefabs;
}