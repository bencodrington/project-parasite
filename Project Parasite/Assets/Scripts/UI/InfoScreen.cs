using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class InfoScreen : MonoBehaviour
{

    #region [Public Variables]
    
    // Will start displaying message when the player enters this zone
    public InfoScreenTriggerZone triggerZone;
    
    #endregion

    #region [Private Variables]
    
    TextMeshPro textMesh;
    // Used to keep track of what the screen should say, even when
    //  the textMesh has different content mid-animation
    string fullString;

    Coroutine printing;

    const float TIME_BETWEEN_LETTERS = 0.1f;
    
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
    
    void StartPrinting() {
        Debug.Log(fullString);
        if (printing != null) {
            StopCoroutine(printing);
            textMesh.text = "";
        }
        printing = StartCoroutine(Printing());
    }

    IEnumerator Printing() {
        float timeElapsed = 0;
        while (timeElapsed < fullString.Length * TIME_BETWEEN_LETTERS) {
            textMesh.text += fullString[(int)Mathf.Floor(timeElapsed / TIME_BETWEEN_LETTERS)];
            yield return new WaitForSeconds(TIME_BETWEEN_LETTERS);
            timeElapsed += TIME_BETWEEN_LETTERS;
        }
        textMesh.text = fullString;
    }
    
    #endregion

}
