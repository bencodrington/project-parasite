using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BurstIndicator : MonoBehaviour
{

    #region [Private Variables]

    Color START_COLOUR  = new Color(.15f, 0f, 0f, .85f);
    Color END_COLOUR    = new Color(.75f, 0f, 0f, 1f);

    SpriteRenderer spriteRenderer;
    SpriteMask spriteMask;

    float timeToFill = 0.25f;
    float timeElapsed = 0f;
    bool isFilling = false;
    
    #endregion

    #region [Public Methods]

    public void SetTimeToFill(float newTimeToFill) {
        timeToFill = newTimeToFill;
    }

    
    public void StartFilling() {
        timeElapsed = 0f;
        isFilling = true;
    }

    public void StopFilling() {
        isFilling = false;
    }
    
    #endregion

    #region [MonoBehaviour Callbacks]
    
    void Start() {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        spriteMask = GetComponentInChildren<SpriteMask>();
        // Hide at start
        spriteRenderer.color = Color.clear;
    }

    void Update() {
        if (isFilling) {
            timeElapsed += Time.deltaTime;
            float progress = isFilling ? Mathf.Min(timeElapsed / timeToFill, 1) : 0f;
            spriteRenderer.color = Color.Lerp(START_COLOUR, END_COLOUR, progress);
            spriteMask.alphaCutoff = 1 - progress;
        } else {
            spriteRenderer.color = Color.clear;
            spriteMask.alphaCutoff = 0;
        }
    }
    
    #endregion
}
