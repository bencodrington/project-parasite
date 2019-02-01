using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlatformCalledAlert : MonoBehaviour
{
    Text text;
    Color startingColour;
    Color fadeColour;
    float LIFETIME = .5f;
    float remainingLifetime;
    Vector2 startingPosition;
    Vector2 displacement = new Vector2(0, .125f);
    void Start() {
        // Cache reference to the text on this object
        text = GetComponentInChildren<Text>();
        // Cache the starting colour
        startingColour = text.color;
        // Fade out and turn bluish over time
        fadeColour = new Color(startingColour.r, startingColour.g, 1, 0f);
        remainingLifetime = LIFETIME;
        startingPosition = transform.position;
    }

    void Update() {
        // How far from the startingPosition should the alert be this frame
        Vector2 offset;
        if (remainingLifetime < 0) {
            Destroy(gameObject);
        } else {
            // Update colour and position
            text.color = Color.Lerp(fadeColour, startingColour, remainingLifetime / LIFETIME);;
            offset = (1 - remainingLifetime / LIFETIME) * displacement;
            transform.position = startingPosition + offset;
            // Decrement the remaining lifetime
            remainingLifetime -= Time.deltaTime;
        }
    }
}
