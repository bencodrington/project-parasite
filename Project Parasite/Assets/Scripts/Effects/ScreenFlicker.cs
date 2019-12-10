using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScreenFlicker : MonoBehaviour
{
    
    #region [Public Variables]
    
    public SpriteRenderer spriteRenderer;
    
    #endregion

    #region [Private Variables]

    const float minOpacity = 0.6f;
    const float maxOpacity = 1f;
    const float timeBetweenFlickers = 0.05f;
    float timeToNextFlicker;
    
    Color startColour;
    
    #endregion

    #region [MonoBehaviour Callbacks]

    void Start() {
        startColour = spriteRenderer.color;
        timeToNextFlicker = timeBetweenFlickers;
    }
    
    void Update() {
        if (timeToNextFlicker < 0) {
            spriteRenderer.color = new Color(startColour.r, startColour.g, startColour.b, Random.Range(minOpacity, maxOpacity));
            timeToNextFlicker += timeBetweenFlickers;
        }
        timeToNextFlicker -= Time.deltaTime;
    }
    
    #endregion
    
}
