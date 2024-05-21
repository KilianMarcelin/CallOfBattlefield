using Aura2API;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class Options : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown dropdownQuality;
    [SerializeField] private GameObject toActive;
    [SerializeField] private AuraVolume auraVolume;
    [SerializeField] private PostProcessVolume ppVolume;
    
    // Hacky fix
    public static bool isPaused = false;

    public void SetQuality(int i)
    {
        QualitySettings.SetQualityLevel(i, true);

        if (i == 0) // Low
        {
            if (auraVolume) auraVolume.enabled = false;
            if (ppVolume) ppVolume.enabled = false;
        }
        else
        {
            if (auraVolume) auraVolume.enabled = true;
            if (ppVolume) ppVolume.enabled = true;
        }
    }

    private void Start()
    {
        int qualityLevel = QualitySettings.GetQualityLevel();
        dropdownQuality.value = qualityLevel;
        
        // Update aura and pp volume
        SetQuality(qualityLevel);
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