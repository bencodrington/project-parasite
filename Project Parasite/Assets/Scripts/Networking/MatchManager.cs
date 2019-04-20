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
    
    #endregion

    #region [MonoBehaviour Callbacks]

    public void Start() {
        playersReady = new Dictionary<int, bool>();
        roundManagerPrefab = Resources.Load("RoundManager") as GameObject;

        if (DEBUG_MODE) {
            // Start offline round
            PhotonNetwork.OfflineMode = true;
            SendStartGameEvent();
        } else {
            UiManager.Instance.ShowMainMenu();
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
        CheckIfAllPlayersReady();
    }

    void CheckIfAllPlayersReady() {
        int playerNumber;
        foreach (Player player in PhotonNetwork.PlayerList) {
            playerNumber = player.ActorNumber;
            if (!playersReady.ContainsKey(playerNumber) || !playersReady[playerNumber]) {
                // Return if we haven't received a ready message from one of the players
                //  or if the most recent message we've received from them is that they're
                //  not ready
                if (PhotonNetwork.IsMasterClient) {
                    // Ensure the start game button isn't being shown
                    UiManager.Instance.SetStartGameButtonActive(false);
                }
                return;
            }
        }
        // If we got here, all connected players are ready
        if (PhotonNetwork.IsMasterClient) {
            UiManager.Instance.SetStartGameButtonActive(true);
        }
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
    }

    #endregion

}
