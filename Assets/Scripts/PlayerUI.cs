using System;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    public Image healthOverlay;
    public Image hitmarker;
    public float animSpeed = 2;
    public float hitmarkerDecay = 2.0f;

    private float health = 100;

    private float hitmarkerOpacity = 0;

    public void UpdateHealth(float health)
    {
        this.health = health;
    }

    public void Hit()
    {
        hitmarkerOpacity = 1;
    }

    public void Update()
    {
        health = Math.Clamp(health, 0, 100);
        healthOverlay.color = new Color(healthOverlay.color.r, healthOverlay.color.g, healthOverlay.color.b,
            (float)Math.Abs(Math.Sin(Time.time * animSpeed) * (1.0f - health / 100.0f)));
        hitmarkerOpacity = Math.Max(0, hitmarkerOpacity - Time.deltaTime / hitmarkerDecay);
        hitmarker.color = new Color(hitmarker.color.r, hitmarker.color.g, hitmarker.color.b, hitmarkerOpacity);
    }
}