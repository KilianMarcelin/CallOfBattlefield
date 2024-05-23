using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class Volume : MonoBehaviour
{
    public AudioMixer audioMixer;
    public string volumeParameter;

    private Slider slider;

    private void Awake()
    {
        slider = GetComponent<Slider>();

        // Ajouter un écouteur d'événement pour détecter les changements de valeur du slider
        slider.onValueChanged.AddListener(delegate { OnSliderValueChanged(); });

        // Récupérer la valeur actuelle du volume depuis l'AudioMixer et la convertir en pourcentage
        float volume = GetInitialVolume();
        // Convertir la valeur de volume en pourcentage pour le slider (0-1)
        slider.value = Mathf.Pow(10, volume / 20);
    }

    // Appelé lorsque la valeur du slider change
    private void OnSliderValueChanged()
    {
        // Si le slider est à zéro, définir le volume à -80 dB
        if (slider.value == 0f)
        {
            audioMixer.SetFloat(volumeParameter, -80f);
        }
        else
        {
            // Convertir la valeur du slider (0-1) en valeur de volume réel
            float volume = Mathf.Log10(slider.value) * 20;

            // Définir le volume dans l'AudioMixer
            audioMixer.SetFloat(volumeParameter, volume);
        }
    }

    // Récupérer la valeur actuelle du volume depuis l'AudioMixer
    private float GetInitialVolume()
    {
        float volume;
        bool result = audioMixer.GetFloat(volumeParameter, out volume);
        if (result)
        {
            return volume;
        }
        else
        {
            Debug.LogWarning("Le paramètre de volume spécifié n'existe pas dans l'AudioMixer.");
            return 0f;
        }
    }
}
