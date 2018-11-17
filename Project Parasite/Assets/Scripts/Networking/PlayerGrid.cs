using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class PlayerGrid : NetworkBehaviour {

    public static PlayerGrid Instance {get; protected set;}

    class PlayerData {
        // Player Name
        public string name;
        // PlayerObject Net Id
        public NetworkInstanceId playerNetId;
        // PlayerObject itself
        public PlayerObject playerObject;
        // Character Type
        public CharacterType characterType;
        // Character
        public Character character;
        // Whether or not is the local player
        public bool isLocalPlayer;

        // Constructor
        public PlayerData(string name,
                    NetworkInstanceId playerNetId,
                    PlayerObject playerObject,
                    CharacterType characterType,
                    Character character,
                    bool isLocalPlayer) {
            this.name = name;
            this.playerNetId = playerNetId;
            this.playerObject = playerObject;
            this.characterType = characterType;
            this.character = character;
            this.isLocalPlayer = isLocalPlayer;
        }
    }

    List<PlayerData> playerList;

    void OnEnable() {
		if(Instance != null) {
            // There should never be two player grid instances
            Debug.LogError("Attempting to enable a second PlayerGrid.");
            Destroy(this);
            return;
        }
        Instance = this;
        playerList = new List<PlayerData>();
    }

    PlayerData FindEntryWithId(NetworkInstanceId playerNetId) {
        PlayerData playerData = playerList.Find((entry) => entry.playerNetId == playerNetId);
        return playerData;
    }

    // public GameObject GetLocalPlayerObject() {
    //     GameObject LocalPlayerObject = null;
    //     // TODO:
    //     // Find entry in player list where isLocalPlayer is true
    //     // return entry.playerObject;
    //     return LocalPlayerObject;
    // }

    // public Character GetLocalCharacter() {
    //     return null;
    // }

    public CharacterType GetLocalCharacterType() {
        return CharacterType.Hunter;
    }

    public void SetCharacterType(NetworkInstanceId playerNetId, CharacterType characterType) {
        PlayerData player = FindEntryWithId(playerNetId);
        if (player == null) {
            Debug.LogError("PlayerGrid: SetCharacterType: Failed to find player with net id " + playerNetId);
            return;
        }
        player.characterType = characterType;
    }

    public void SetLocalPlayer(NetworkInstanceId playerNetId) {
        PlayerData player = FindEntryWithId(playerNetId);
        if (player == null) {
            Debug.LogError("PlayerGrid: SetLocalPlayer: Failed to find player with net id " + playerNetId);
            return;
        }
        player.isLocalPlayer = true;
    }

    public void PrintGrid() {
        Debug.Log("=========Player Grid=========");
        foreach (PlayerData entry in playerList) {
            Debug.Log(
                "Name: " + entry.name + ", " +
                "NetId: " + entry.playerNetId + ", " +
                "PlayerObject: " + entry.playerObject + ", " +
                "Character Type: " + entry.characterType + ", " +
                "Character: " + entry.character + ", " +
                "IsLocalPlayer: " + entry.isLocalPlayer
            );
        }
        Debug.Log("=========================");
    }

    public void AddPlayer(NetworkInstanceId playerNetId) {
        PlayerData newPlayer = new PlayerData("Undefined Name",
                    playerNetId,
                    NetworkServer.FindLocalObject(playerNetId).GetComponent<PlayerObject>(),
                    CharacterType.NPC,
                    null,
                    false);
        playerList.Add(newPlayer);
        Debug.Log("ADD PLAYER: " + newPlayer.name + ", Net Id: " + newPlayer.playerNetId);
        // TODO: RPC update clients
        // TODO: make sure they don't already have this player in their list, as local player objects may have updated their local player grid before notifying the server
    }
}