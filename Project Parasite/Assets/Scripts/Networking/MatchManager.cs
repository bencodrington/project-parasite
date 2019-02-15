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
    
    // Prefabs for the various menu item sets this flow requires
    public MenuItemSet searchingForPlayersMenuItemSet;
    GameObject roundManagerPrefab;
    public GameObject playerObjectPrefab;
    
    #endregion

    #region [Private Variables]
    
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

    public void StartGame() {
        Debug.Log("master client: starting game");
        byte eventCode = EventCodes.StartGame;
        EventCodes.RaiseEventAll(eventCode, null);
    }
    
    #endregion

    #region [MonoBehaviour Callbacks]

    public void Start() {
        playersReady = new Dictionary<int, bool>();
        roundManagerPrefab = Resources.Load("RoundManager") as GameObject;
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
        TransitionToMenuItemSet(searchingForPlayersMenuItemSet);
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
            // Hide menu
            // TODO: extract to UI manager
            Destroy(GameObject.FindWithTag("TitleScreen"));
            // Hide Menu
            Menu menu = FindObjectOfType<Menu>();
            if (menu == null) {
                Debug.LogError("MatchManager: OnEvent: Menu not found");
                return;
            }
            menu.DeleteMenuItems();

            if (PhotonNetwork.IsMasterClient) {
                // If roundmanager exists, end round and destroy it
                // TODO:
                // Create new roundmanager
                if (roundManagerPrefab == null) {
                    Debug.LogError("MatchManager: OnEvent: roundManagerPrefab not set.");
                    return;
                }
                PhotonNetwork.Instantiate(roundManagerPrefab.name, Vector3.zero, Quaternion.identity, 0);

            }
        }
    }

    #region [Private Methods]

    void TransitionToMenuItemSet(MenuItemSet menuItemSet) {
        Menu menu = FindObjectOfType<Menu>();
        if (menu == null) {
            Debug.LogError("MatchManager: TransitionToMenuItemSet: Menu not found");
            return;
        }
        menu.TransitionToNewMenuItemSet(menuItemSet);
    }

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
                return;
            }
        }
        // If we got here, all connected players are ready
        Debug.Log("Everyone's ready!");
        if (PhotonNetwork.IsMasterClient) {
            StartGame();
        }
    }

    void InstantiatePlayerObject() {
        Instantiate(playerObjectPrefab, Vector3.zero, Quaternion.identity);
    }

    #endregion

}
