using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlatformCalledAlert : MonoBehaviour
{
    #region [Public Variables]
    
    public float totalLifetime = .5f;
    
    #endregion

    #region [Private Variables]
    
    Text text;
    Color startingColour;
    Color fadeColour;
    float remainingLifetime;

    // If the alert has a parent, this value is this displacement from the parent's transform to
    //  the alert's transform when it is spawned
    // If not, this value is the alert's position when it is spawned
    Vector2 startingOffset;
    // How far the alert moves over its lifetime
    Vector2 displacement = new Vector2(0, .125f);
    
    #endregion
    void Start() {
        // Cache reference to the text on this object
        text = GetComponentInChildren<Text>();
        // Cache the starting colour
        startingColour = text.color;
        // Fade out and turn bluish over time
        fadeColour = new Color(startingColour.r, startingColour.g, 1, 0f);
        remainingLifetime = totalLifetime;
        startingOffset = transform.parent != null
            ? transform.position - transform.parent.position
            : transform.position;
    }

    void Update() {
        // How far from the startingOffset should the alert be this frame
        Vector2 offset;
        if (remainingLifetime < 0) {
            Destroy(gameObject);
        } else {
            // Update colour and position
            text.color = Color.Lerp(fadeColour, startingColour, remainingLifetime / totalLifetime);
            offset = (1 - remainingLifetime / totalLifetime) * displacement;
            transform.position = transform.parent != null
                ? (Vector2)transform.parent.position + startingOffset + offset
                : startingOffset + offset;
            // Decrement the remaining lifetime
            remainingLifetime -= Time.deltaTime;
        }
    }
}
