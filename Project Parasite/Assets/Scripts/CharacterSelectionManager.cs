﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        Reset();
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
    
    public bool IsValidComposition() {
        // Return true iff everyone has made a selection
        //  and only one person has selected parasite
        bool everyoneHasSelected = characterSelections.Count == PhotonNetwork.PlayerList.Length;
        bool exactlyOneParasite = IsExactlyOneParasite();
        return everyoneHasSelected && exactlyOneParasite;
    }

    public void Reset() {
        // Initialize dictionary of character selections
        characterSelections = new Dictionary<int, CharacterType>();
    }

    public void RequestUpdate() {
        EventCodes.RaiseEventAll(EventCodes.RequestCharacterSelections, null);
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
        } else if (photonEvent.Code == EventCodes.RequestCharacterSelections) {
            if (!PhotonNetwork.IsMasterClient) { return; }
            // We're the master client, so send the character selections dictionary
            int[] keys = characterSelections.Keys.ToArray();
            int[] values = new int[keys.Length];
            for (int i=0; i < keys.Length; i++) {
                values[i] = (int)characterSelections[keys[i]];
            }
            object[] content = { keys, values };
            EventCodes.RaiseEventAll(EventCodes.SendCharacterSelections, content);
        } else if (photonEvent.Code == EventCodes.SendCharacterSelections) {
            if (PhotonNetwork.IsMasterClient) { return; }
            // We're not the master client, so in case we were the one who sent the request,
            //  overwrite our set of character selections with the master's copy
            Reset();
            object[] content = (object[])photonEvent.CustomData;
            int[] keys = (int[])content[0];
            CharacterType[] values = (CharacterType[])content[1];
            for (int i = 0; i < keys.Length; i++) {
                SetCharacterSelected(keys[i], values[i]);
            }
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
        // Check to see if the valid composition status has changed
        //  and update UI accordingly
        if (PhotonNetwork.IsMasterClient) {
            UiManager.Instance.SetStartGameButtonActive(IsValidComposition());
        }
    }

    bool IsExactlyOneParasite() {
        bool hasSeenParasite = false;
        foreach (int key in characterSelections.Keys) {
            if (hasSeenParasite && characterSelections[key] == CharacterType.Parasite) {
                // This is the second parasite we've seen
                return false;
            } else if (characterSelections[key] == CharacterType.Parasite) {
                // This is the first parasite we've seen
                hasSeenParasite = true;
            }
        }
        return hasSeenParasite;
    }

    #endregion
}
