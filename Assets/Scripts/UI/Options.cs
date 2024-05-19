using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Options : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown dropdownQuality;
    [SerializeField] private GameObject toActive;

    // Hacky fix
    public static bool isPaused = false;

    public void SetQuality(int i)
    {
        QualitySettings.SetQualityLevel(i, true);
    }

    private void Start()
    {
        int qualityLevel = QualitySettings.GetQualityLevel();
        dropdownQuality.value = qualityLevel;
    }

    public void Pause()
    {
        isPaused = true;
        toActive.SetActive(isPaused);
        Cursor.lockState = CursorLockMode.None;
    }

    public void Unpause()
    {
        isPaused = false;
        toActive.SetActive(isPaused);
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void Toggle()
    {
        if (isPaused) Unpause();
        else Pause();
    }

    private void Update()
    {
        if (Input.GetButtonDown("Cancel"))
        {
            Toggle();
        }
    }
}
