using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Options : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown dropdownQuality;

    public void SetQuality(int i)
    {
        QualitySettings.SetQualityLevel(i, true);
    }

    private void Start()
    {
        int qualityLevel = QualitySettings.GetQualityLevel();
        dropdownQuality.value = qualityLevel;
    }
}
