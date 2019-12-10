using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class BingoScreen : MonoBehaviour
{

    #region [Public Variables]
    
    public TextMeshPro leftText;
    public TextMeshPro centerText;
    public TextMeshPro rightText;
    
    #endregion

    #region [Private Variables]
    
    string[] PHRASES = new string[]{
        "it's bingo time",
        "grab your card",
        "crave that rush",
        "ohana means bingo",
        "yeah baby yeah",
        "let's go bingo",
        "real spicy bingo",
        "sweet bingo meat",
        "hit that bingo"
    };

    int currentPhraseIndex = -1;
    
    #endregion

    #region [Public Methods]
    
    public void NewPhrase() {
        // Select new phrase
        // int newPhraseIndex = currentPhraseIndex; // For testing word length
        int newPhraseIndex = Random.Range(0, PHRASES.Length);
        if (newPhraseIndex == currentPhraseIndex) {
            // Ensure we don't show the same phrase twice in a row
            newPhraseIndex = newPhraseIndex == PHRASES.Length - 1 ? 0 : newPhraseIndex + 1;
        }
        currentPhraseIndex = newPhraseIndex;
        string[] newWords = PHRASES[currentPhraseIndex].Split(' ');
        if (newWords.Length != 3) {
            Debug.LogError("BingoScreen:NewPhrase(): '" + PHRASES[currentPhraseIndex] + "' isn't exactly 3 words.");
            return;
        }
        // Update textMeshPro components
        leftText.text   = newWords[0];
        centerText.text = newWords[1];
        rightText.text  = newWords[2];
    }
    
    #endregion
}
