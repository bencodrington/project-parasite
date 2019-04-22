using System.Collections;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class CharacterSelectionManager : IOnEventCallback
{
    #region [Private Variables]
    
    // true iff Random Character Selection is turned off
    bool isEnabled = true;
    // Key is Photon Actor Number and CharacterType is the selected
    //  character. If a given player does not currently have a character
    //  selected, they have no entry in the list
    Dictionary<int, CharacterType> characterSelections;
    
    #endregion

    #region [Public Methods]

    // Constructor
    public CharacterSelectionManager() {
        // Register to receive events
        PhotonNetwork.AddCallbackTarget(this);
        // Initialize dictionary of character selections
        characterSelections = new Dictionary<int, CharacterType>();
    }
    
    public void SetEnabled(bool newValue) {
        isEnabled = newValue;
    }

    public Dictionary<int, CharacterType> GetCharacterSelections() {
        if (!isEnabled) {
            return null;
        }
        return characterSelections;
    }
    
    #endregion

    public void OnEvent(EventData photonEvent) {
        if (photonEvent.Code == EventCodes.SelectCharacter) {
            // Deconstruct event
            object[] content = (object[])photonEvent.CustomData;
            int actorNumber = (int)content[0];
            CharacterType selectedCharacter  = (CharacterType)content[1];
            // Update characterSelections dictionary
            SetCharacterSelected(actorNumber, selectedCharacter);
        }
    }

    #region [Private Methods]

    void SetCharacterSelected(int actorNumber, CharacterType selectedCharacter) {
        if (characterSelections.ContainsKey(actorNumber) &&
            (characterSelections[actorNumber] == selectedCharacter)) {
            // Player has deselected their previous character choice
            characterSelections.Remove(actorNumber);
        } else {
            characterSelections[actorNumber] = selectedCharacter;
        }
        // TODO: check valid team composition
    }

    #endregion
}
