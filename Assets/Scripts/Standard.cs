using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Standard : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        foreach (var material in Resources.FindObjectsOfTypeAll<Material>())
        {
            Debug.Log("updating ...");
            if (material.shader.name.StartsWith("Hidden/InternalErrorShader"))
            {
                material.shader = Shader.Find("Standard");
            }
        }
    }
}
