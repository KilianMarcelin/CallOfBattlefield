using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Options : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown dropdownQuality;
    [SerializeField] private GameObject toActive;

    public void SetQuality(int i)
    {
        QualitySettings.SetQualityLevel(i, true);
    }

    private void Start()
    {
        int qualityLevel = QualitySettings.GetQualityLevel();
        dropdownQuality.value = qualityLevel;
    }

    private void Update()
    {
        if (Input.GetButtonDown("Cancel"))
        {
            if(toActive) toActive.SetActive(!toActive.activeSelf);
        }
    }
}
