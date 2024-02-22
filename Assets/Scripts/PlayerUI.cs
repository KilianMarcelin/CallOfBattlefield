using System;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    public Image healthOverlay;
    public float animSpeed = 2;
    
    private float health = 100;
    
    public void UpdateHealth(float health)
    {
        this.health = health;
    }
    
    public void Update()
    {
        health = Math.Clamp(health, 0, 100);
        healthOverlay.color = new Color(healthOverlay.color.r,healthOverlay.color.g,healthOverlay.color.b, (float) Math.Abs(Math.Sin(Time.time * animSpeed) * (1.0f-health/100.0f)));
    }
}