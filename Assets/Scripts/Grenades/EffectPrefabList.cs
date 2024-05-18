using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "effectPrefabList", menuName = "ScriptableObjects/EffectPrefabList", order = 2)]
public class EffectPrefabList : ScriptableObject
{
    public List<GameObject> explosionEffects;
}