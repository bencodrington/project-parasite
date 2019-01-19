using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AlertIcon : MonoBehaviour {

    SpriteRenderer[] SRs;
    // How long before the icon is completely faded
    const float LIFETIME = 1;
    float remainingLifetime;
    void Start() {
        SRs = GetComponentsInChildren<SpriteRenderer>();
        // Initialize to full lifetime remaining
        remainingLifetime = LIFETIME;
    }

    void Update() {
        // Fade over time
        SetAlpha(remainingLifetime / LIFETIME);
        remainingLifetime -= Time.deltaTime;
        // Then destroy self
        if (remainingLifetime < 0) {
            Destroy(gameObject);
        }
    }

    void SetAlpha(float alpha) {
        Color c;
        foreach (SpriteRenderer sR in SRs) {
            c = sR.color;
            sR.color = new Color(c.r, c.g, c.b, alpha);
        }
    }
}
