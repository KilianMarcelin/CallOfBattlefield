using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Simple class to play a foot sound when walking cause I'm too lazy to do it the proper way
public class FootSound : MonoBehaviour
{
    public AudioSource source;
    public AudioClip clip;
    
    public void PlaySound()
    {
        source.PlayOneShot(clip);
    }
}
