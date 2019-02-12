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

        public static PlayerData Empty() {
            return new PlayerData("Undefined Name",
                    NetworkInstanceId.Invalid,
                    null,
                    CharacterType.NPC,
                    null,
                    false);
        }
    }

    List<PlayerData> playerList;

    public string localPlayerName;

    void Awake() {
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
        return playerList.Find((entry) => entry.playerNetId == playerNetId);
    }

    PlayerData FindLocalEntry() {
        return playerList.Find((entry => entry.isLocalPlayer));
    }

    public PlayerObject GetLocalPlayerObject() {
        return FindLocalEntry().playerObject;
    }

    public Character GetLocalCharacter() {
        PlayerData player = FindLocalEntry();
        if (player == null) {
            Debug.LogError("PlayerGrid: GetLocalCharacter: No entries are marked as the local player");
            return null;
        }
        return player.character;
    }

    public CharacterType GetLocalCharacterType() {
        return FindLocalEntry().characterType;
    }

    public List<string> GetPlayerNames() {
        List<string> names = new List<string>();
        foreach(PlayerData player in playerList) {
            names.Add(player.name);
        }
        return names;
    }

    void SetCharacter(NetworkInstanceId playerNetId, Character character) {
        PlayerData player = FindEntryWithId(playerNetId);
        if (player == null) {
            Debug.LogError("PlayerGrid: SetCharacter: Failed to find player with net id " + playerNetId);
            return;
        }
        player.character = character;
    }

    void SetCharacterType(NetworkInstanceId playerNetId, CharacterType characterType) {
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

    void SetPlayerName(NetworkInstanceId playerNetId, string name) {
        PlayerData player = FindEntryWithId(playerNetId);
        if (player == null) {
            AddPlayer(playerNetId);
            player = FindEntryWithId(playerNetId);
        }
        player.name = name;
    }

    public void SetPlayerObject(NetworkInstanceId playerNetId, PlayerObject playerObject) {
        PlayerData player = FindEntryWithId(playerNetId);
        if (player == null) {
            AddPlayer(playerNetId);
            player = FindEntryWithId(playerNetId);
        }
        player.playerObject = playerObject;
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
        // Make sure grid doesn't already contain this player
        if (FindEntryWithId(playerNetId) != null) {
            Debug.Log("PlayerGrid: AddPlayer: Grid already contains an entry with net id " + playerNetId);
            return;
        }
        PlayerObject playerObject;
        if (isServer) {
            playerObject = NetworkServer.FindLocalObject(playerNetId).GetComponent<PlayerObject>();
        } else {
            playerObject = ClientScene.FindLocalObject(playerNetId).GetComponent<PlayerObject>();
        }
        PlayerData newPlayer = PlayerData.Empty();
        newPlayer.playerObject = playerObject;
        newPlayer.playerNetId = playerNetId;
        playerList.Add(newPlayer);
        Debug.Log("ADD PLAYER: " + newPlayer.name + ", Net Id: " + newPlayer.playerNetId);
    }

    // Commands

    [Command]
    public void CmdAddPlayer(NetworkInstanceId playerNetId) {
        // CmdAddPlayer is only called by the PlayerObject.OnStartLocalPlayer() method
        RpcAddPlayer(playerNetId);
    }

    [Command]
    public void CmdSetCharacterType(NetworkInstanceId playerNetId, CharacterType characterType) {
        PlayerData player = FindEntryWithId(playerNetId);
        if (player == null) {
            Debug.LogError("PlayerGrid: CmdSetCharacterType: Failed to find player with net id " + playerNetId);
            return;
        }
        if (player.characterType == characterType) {
            return;
        }
        RpcSetCharacterType(playerNetId, characterType);
    }

    [Command]
    public void CmdSetCharacter(NetworkInstanceId playerNetId, NetworkInstanceId characterNetId) {
        GameObject localObject = NetworkServer.FindLocalObject(characterNetId);
        if (localObject == null) {
            Debug.LogError("PlayerGrid: CmdSetCharacter: Failed to find character object with net id " + characterNetId);
            return;
        }
        RpcSetCharacter(playerNetId, characterNetId);
    }

    [Command]
    public void CmdSetPlayerName(NetworkInstanceId playerNetId, string name) {
        RpcSetPlayerName(playerNetId, name);
    }

    [Command]
    public void CmdPull() {
        PlayerData e = PlayerData.Empty();
        foreach (PlayerData pD in playerList) {
            // Find any properties that have been set and distribute them to client copies of the PlayerGrid
            if (pD.name != e.name) {
                RpcSetPlayerName(pD.playerNetId, pD.name);
            }
            if (pD.characterType != e.characterType){
                RpcSetCharacterType(pD.playerNetId, pD.characterType);
            }
            if (pD.character != e.character) {
                // TODO:
                // RpcSetCharacter(pD.playerNetId, pD.character.netId);
            }
        }
    }

    // ClientRpc

    [ClientRpc]
    void RpcAddPlayer(NetworkInstanceId playerNetId) {
        AddPlayer(playerNetId);
    }

    [ClientRpc]
    void RpcSetCharacterType(NetworkInstanceId playerNetId, CharacterType characterType) {
        SetCharacterType(playerNetId, characterType);
    }

    [ClientRpc]
    void RpcSetCharacter(NetworkInstanceId playerNetId, NetworkInstanceId characterNetId) {
        GameObject localObject =    isServer ?
                                    NetworkServer.FindLocalObject(characterNetId) :
                                    ClientScene.FindLocalObject(characterNetId);
        if (localObject == null) {
            Debug.LogError("PlayerGrid: CmdSetCharacter: Failed to find character object with net id " + characterNetId);
            return;
        }
        SetCharacter(playerNetId, localObject.GetComponent<Character>());

    }

    [ClientRpc]
    void RpcSetPlayerName(NetworkInstanceId playerNetId, string name) {
        SetPlayerName(playerNetId, name);
    }
}