using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class CharacterSelectButton : MonoBehaviour
{
    #region [Public Variables]
    
    public CharacterType characterType;
    
    #endregion

    #region [Private Variables]

    bool isSelected = false;

    static List<CharacterSelectButton> allSelectButtons;
    
    #endregion

    #region [Public Methods]
    
    public void OnClick() {
        if (isSelected) {
            // This one was previously selected
            Disable();
        } else {
            // Another one was previously selected
            //  So disable all buttons
            foreach (CharacterSelectButton selectButton in allSelectButtons) {
                selectButton.Disable();
            }
            // Then reenable this one
            isSelected = true;
        }
        // Alert local and client MatchManagers that a character selection has been made
        byte eventCode = EventCodes.SelectCharacter;
        object[] content = new object[] {
            PhotonNetwork.LocalPlayer.ActorNumber,
            characterType
        };
        EventCodes.RaiseEventAll(eventCode, content);
    }

    public void Disable() {
        isSelected = false;
    }
    
    #endregion

    #region [MonoBehaviour Callbacks]
    
    void Start() {
        // Store all CharacterSelectButtons in a statically accessible list
        if (allSelectButtons == null) {
            // First character select button to have called it's Start() method
            //  So initialize the list
            allSelectButtons = new List<CharacterSelectButton>();
        }
        allSelectButtons.Add(this);
    }
    
    #endregion
}
