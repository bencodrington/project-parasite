using UnityEngine;
using TMPro;

public class Nametag : MonoBehaviour {
    
    #region [Public Variables]
    
    public TextMeshPro text;
    
    public string fullName {get; private set;}
    
    #endregion

    #region [Public Methods]
    
    public void SetName(string newName) {
        fullName = newName;
        updateText();
    }
    
    #endregion

    #region [Private Methods]
    
    void updateText() {
        bool setBlank = fullName == MatchManager.DEFAULT_PLAYER_NAME || fullName == null;
        text.text = setBlank ? "" : fullName;
    }
    
    #endregion

}
