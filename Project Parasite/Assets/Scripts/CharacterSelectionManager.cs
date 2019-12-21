using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class CharacterSelectionManager : IOnEventCallback
{
    #region [Private Variables]

    // If true, randomly select parasite.
    //  If false, players select their own character type.
    bool isRandomParasite = false;
    // Key is Photon Actor Number and CharacterType is the selected
    //  character. If a given player does not currently have a character
    //  selected, they have no entry in the list
    Dictionary<int, CharacterType> characterSelections;
    // Key is Photon Actor Number and bool is whether or not the player has
    //  selected the ready checkbox. If a given player hasn't selected
    //  the box, they have no entry in the list
    Dictionary<int, bool> playersReady;
    
    #endregion

    #region [Public Methods]

    // Constructor
    public CharacterSelectionManager() {
        // Register to receive events
        PhotonNetwork.AddCallbackTarget(this);
        Reset();
    }

    public Dictionary<int, CharacterType> GetCharacterSelections() {
        if (isRandomParasite) {
            return null;
        }
        return characterSelections;
    }

    public bool GetIsRandomParasite() {
        return isRandomParasite;
    }

    public void Reset() {
        // Initialize dictionary of character selections
        characterSelections = new Dictionary<int, CharacterType>();
        playersReady = new Dictionary<int, bool>();
    }

    public void RequestUpdate() {
        EventCodes.RaiseEventAll(EventCodes.RequestCharacterSelections, null);
    }
    
    #endregion

    public void OnEvent(EventData photonEvent) {
        if (photonEvent.Code == EventCodes.SetReady) {
            // Deconstruct event
            object[] content = (object[])photonEvent.CustomData;
            int actorNumber = (int)content[0];
            bool isReady = (bool)content[1];
            // Update playersReady dictionary
            SetActorReady(actorNumber, isReady);
        } else if (photonEvent.Code == EventCodes.ToggleRandomParasite) {
            SetIsRandomParasite((bool)EventCodes.GetFirstEventContent(photonEvent));
        } else if (photonEvent.Code == EventCodes.SelectCharacter) {
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
            // Also re-broadcast whether we're in 'Random Parasite' mode or not
            content = new object[] { isRandomParasite};
            EventCodes.RaiseEventAll(EventCodes.ToggleRandomParasite, content);
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

    void SetIsRandomParasite(bool isRandom) {
        isRandomParasite = isRandom;
        // Let UiManager know which controls to show
        UiManager.Instance.OnIsRandomParasiteChanged(isRandom);
        if (PhotonNetwork.IsMasterClient) {
            UiManager.Instance.SetStartGameButtonActive(IsValidComposition());
        }
    }

    void SetActorReady(int actorNumber, bool isReady) {
        playersReady[actorNumber] = isReady;
        if (PhotonNetwork.IsMasterClient) {
            UiManager.Instance.SetStartGameButtonActive(IsValidComposition());
        }
    }

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

    bool AreAllPlayersReady() {
        int playerNumber;
        foreach (Player player in PhotonNetwork.PlayerList) {
            playerNumber = player.ActorNumber;
            if (!playersReady.ContainsKey(playerNumber) || !playersReady[playerNumber]) {
                // Return false if we haven't received a ready message from one of the players
                //  or if the most recent message we've received from them is that they're
                //  not ready
                return false;
            }
        }
        // If we got here, all connected players are ready
        return true;
    }
    
    bool IsValidComposition() {
        //  There are 2 or more players in the game
        bool twoOrMorePlayers = PhotonNetwork.PlayerList.Length > 1;
        if (isRandomParasite) {
            return twoOrMorePlayers && AreAllPlayersReady();
        }
        // Return true iff everyone has made a selection
        bool everyoneHasSelected = characterSelections.Count == PhotonNetwork.PlayerList.Length;
        //  and exactly one person has selected parasite
        bool exactlyOneParasite = IsExactlyOneParasite();
        return everyoneHasSelected && exactlyOneParasite && twoOrMorePlayers;
    }

    #endregion
}
