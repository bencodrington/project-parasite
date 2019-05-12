using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class MatchManager : MonoBehaviourPunCallbacks, IOnEventCallback {

    #region [Public Variables]

    // Use the singleton pattern, as there should only ever
    //  be one MatchManager per client
    public static MatchManager Instance;

    // Keeps track of which players have selected which characters
    public CharacterSelectionManager characterSelectionManager {get; private set;}

    public GameObject playerObjectPrefab;
    // If this is true, skip straight to game and don't connect to multiplayer
    public bool DEBUG_MODE = false;
	// If this is true, spawn all players at (0, 0)
    public bool spawnPlayersAtZero = false;
	// If this is true, spawn all players as hunters
    public bool huntersOnlyMode = false;
	// If this is true, only spawn one NPC rather than usual spawn patterns
	public bool spawnOneNpcOnly = false;
    
    #endregion

    #region [Private Variables]

    GameObject roundManagerPrefab;
    RoundManager roundManager;
    const int MAX_PLAYERS_PER_ROOM = 4;
    Dictionary<int, bool> playersReady;
    bool isRandomParasite = false;

    #endregion

    #region [Public Methods]

    public void Connect() {
        StoreClientName("Anonymous Player");
        // Entry point of all networking
        if (PhotonNetwork.IsConnected) {
            PhotonNetwork.JoinRandomRoom();
        } else {
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    public void SendStartGameEvent() {
        byte eventCode = EventCodes.StartGame;
        EventCodes.RaiseEventAll(eventCode, null);
    }

    public void DebugToggleTimeStop() {
        roundManager.ToggleShouldRunPhysicsUpdate();
    }

    public void DebugSetShouldRunPhysicsUpdate(bool newValue) {
        roundManager.SetShouldRunPhysicsUpdate(newValue);
    }

    public void AdvanceOnePhysicsUpdate() {
        roundManager.AdvanceOnePhysicsUpdate();
    }

    public bool GetDebugMode() {
        return DEBUG_MODE;
    }
    
    #endregion

    #region [MonoBehaviour Callbacks]

    void Awake() {
        if (Instance != null) {
            Debug.LogError("MatchManager:Awake: Attempting to make a second MatchManager");
            return;
        }
        Instance = this;
    }

    void Start() {
        playersReady = new Dictionary<int, bool>();
        roundManagerPrefab = Resources.Load("RoundManager") as GameObject;
        characterSelectionManager = new CharacterSelectionManager();

        if (DEBUG_MODE) {
            // Start offline round
            PhotonNetwork.OfflineMode = true;
            SendStartGameEvent();
        } else {
            UiManager.Instance.ShowMainMenu();
        }
    }

    void Update() {
		if (DEBUG_MODE && Input.GetKeyDown(KeyCode.T)) {
			AdvanceOnePhysicsUpdate();
		}
    }
    
    #endregion

    #region MonoBehaviour PunCallbacks

    public override void OnConnectedToMaster() {
        PhotonNetwork.JoinRandomRoom();
    }

    public override void OnDisconnected(DisconnectCause cause) {
        // TODO:
    }

    public override void OnJoinRandomFailed(short returnCode, string message) {
        Debug.Log("MatchManager:OnJoinRandomFailed(). No random room available, creating one.");
        PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = MAX_PLAYERS_PER_ROOM });
    }

    public override void OnJoinedRoom() {
        if (!DEBUG_MODE) {
            // We're not in debug mode, so the menu object has been created
            //  and should transition now
            UiManager.Instance.OnJoinedRoom();
        }
        InstantiatePlayerObject();
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
        } else if (photonEvent.Code == EventCodes.StartGame) {
            if (PhotonNetwork.IsMasterClient) {
                StartGame();
            }
        } else if (photonEvent.Code == EventCodes.ToggleRandomParasite) {
            SetIsRandomParasite((bool)EventCodes.GetFirstEventContent(photonEvent));
        }
    }

    #region [Private Methods]

    void StoreClientName(string defaultName) {
        PhotonNetwork.LocalPlayer.NickName = GetClientName(defaultName);
    }

    string GetClientName(string defaultName) {
        // Get name from input box
		GameObject nameField = GameObject.FindGameObjectWithTag("Name Field");
		string clientName = "";
		if (nameField != null) {
			clientName = nameField.GetComponent<InputField>().text;
		}
        // Ensure client name is never the empty string
		clientName = (clientName == "") ? defaultName : clientName;
        // Error handling for supplied default name; should never occur
        return (clientName == "") ? "ERROR: DEFAULT NAME WAS EMPTY" : clientName;
    }

    void SetActorReady(int actorNumber, bool isReady) {
        playersReady[actorNumber] = isReady;
        HandleShouldShowStartGameButton();
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

    void InstantiatePlayerObject() {
        Instantiate(playerObjectPrefab, Vector3.zero, Quaternion.identity);
    }

    void StartGame() {
        GameObject roundManagerGameObject;
        // If roundmanager exists, end round
        if (roundManager != null) {
            roundManager.EndRound();
        }
        // Create new roundmanager
        if (roundManagerPrefab == null) {
            Debug.LogError("MatchManager: OnEvent: roundManagerPrefab not set.");
            return;
        }
        roundManagerGameObject = PhotonNetwork.Instantiate(roundManagerPrefab.name, Vector3.zero, Quaternion.identity, 0);
        roundManager = roundManagerGameObject.GetComponent<RoundManager>();
        roundManager.SetSpawnPlayersAtZero(spawnPlayersAtZero);
        roundManager.SetHuntersOnlyMode(huntersOnlyMode);
        roundManagerGameObject.GetComponent<NpcManager>().SetSpawnOneNpcOnly(spawnOneNpcOnly);
        roundManager.SetSelectParasiteRandomly(isRandomParasite);
        roundManager.SetCharacterSelections(characterSelectionManager.GetCharacterSelections());
    }

    void SetIsRandomParasite(bool isRandom) {
        isRandomParasite = isRandom;
        // Let characterSelectionManager know if it should be active
        characterSelectionManager.SetEnabled(!isRandom);
        // Let UiManager know which controls to show
        UiManager.Instance.OnIsRandomParasiteChanged(isRandom);
        HandleShouldShowStartGameButton();
    }

    void HandleShouldShowStartGameButton() {
        // Should only ever show the start game button if we are the master client
        if (!PhotonNetwork.IsMasterClient) { return; }
        bool shouldShow = isRandomParasite ?
            AreAllPlayersReady() :
            characterSelectionManager.IsValidComposition();
        UiManager.Instance.SetStartGameButtonActive(shouldShow);

    }

    #endregion

}
