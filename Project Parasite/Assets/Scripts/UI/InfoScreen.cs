using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class InfoScreen : MonoBehaviour
{

    #region [Public Variables]
    
    // Will start displaying message when the player enters this zone
    public InfoScreenTriggerZone triggerZone;
    public Animator exclamationAnimator;
    
    #endregion

    #region [Private Variables]
    
    TextMeshPro textMesh;
    // Used to keep track of what the screen should say, even when
    //  the textMesh has different content mid-animation
    string fullString;

    Coroutine printing;

    const float TIME_BETWEEN_LETTERS = 0.025f;
    
    #endregion

    #region [Public Methods]
    
    public void Reset() {
        if (printing != null) {
            StopCoroutine(printing);
        }
        textMesh.text = "";
        exclamationAnimator.SetTrigger("Reset");
    }
    
    public void StartPrinting() {
        if (printing != null) {
            StopCoroutine(printing);
            textMesh.text = "";
        }
        printing = StartCoroutine(Printing());
        exclamationAnimator.SetTrigger("Triggered");
    }
    
    #endregion

    #region [MonoBehaviour Callbacks]

    void Start() {
        textMesh = GetComponent<TextMeshPro>();
        fullString = textMesh.text;
        textMesh.text = "";
        if (triggerZone != null) {
            triggerZone.RegisterOnTriggerCallback(StartPrinting);
        }
    }
    
    #endregion

    #region [Private Methods]

    IEnumerator Printing() {
        int charactersShown = 0;
        while (charactersShown < fullString.Length) {
            textMesh.text += fullString[charactersShown];
            yield return new WaitForSeconds(TIME_BETWEEN_LETTERS);
            charactersShown++;
        }
        textMesh.text = fullString;
    }
    
    #endregion

}
