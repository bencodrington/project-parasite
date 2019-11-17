using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlatformCalledAlert : MonoBehaviour
{
    #region [Public Variables]
    
    public float totalLifetime = .5f;
    // How far the alert moves over its lifetime
    public Vector2 displacement = new Vector2(0, .125f);
    
    #endregion

    #region [Private Variables]
    
    Color startingColour;
    Color fadeColour;
    float remainingLifetime;

    // If the alert has a parent, this value is this displacement from the parent's transform to
    //  the alert's transform when it is spawned
    // If not, this value is the alert's position when it is spawned
    Vector2 startingOffset;
    Text _text;
    TextMeshPro _textMeshPro;
    
    #endregion

    #region [Public Methods]
    
    public void Restart() {
        remainingLifetime = totalLifetime;
    }

    public void SetText(string text) {
        CacheTextComponent();
        if (_text != null) {
            _text.text = text;
        } else {
            _textMeshPro.text = text;
        }
    }
    
    #endregion

    void Start() {
        CacheTextComponent();
        // Cache the starting colour
        startingColour = GetTextColour();
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
            SetTextColour(Color.Lerp(fadeColour, startingColour, remainingLifetime / totalLifetime));
            offset = (1 - remainingLifetime / totalLifetime) * displacement;
            transform.position = transform.parent != null
                ? (Vector2)transform.parent.position + startingOffset + offset
                : startingOffset + offset;
            // Decrement the remaining lifetime
            remainingLifetime -= Time.deltaTime;
        }
    }
    
    #region [Private Methods]
    
    void CacheTextComponent() {
        if (_text == null && _textMeshPro == null) {
            // Cache reference to the text on this object
            _text = GetComponentInChildren<Text>();
            if (_text == null) {
                // This prefab variant is using TextMeshPro
                _textMeshPro = GetComponentInChildren<TextMeshPro>();
            }
        }
    }

    Color GetTextColour() {
        if (_text != null) {
            return _text.color;
        }
        return _textMeshPro.color;
    }

    void SetTextColour(Color colour) {
        if (_text != null) {
            _text.color = colour;
            return;
        }
        _textMeshPro.color = colour;
    }
    
    #endregion
}
