using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Photon.Pun;
using Photon.Realtime;

public class MatchManager : MonoBehaviourPunCallbacks {

    // The prefab for the object used to persist client names across menus
    public GameObject ClientInformationPrefab;

    // The current client's name, stored here for easy access when creating matches
    string clientName;

    // Prefabs for the various menu item sets this flow requires
    public MenuItemSet searchingForPlayersMenuItemSet;

    #region Private Variables
    
    const int MAX_PLAYERS_PER_ROOM = 4;

    #endregion

    #region Public Methods

    public void Connect() {
        StoreClientName("Anonymous Player");
        // Entry point of all networking
        if (PhotonNetwork.IsConnected) {
            PhotonNetwork.JoinRandomRoom();
        } else {
            PhotonNetwork.ConnectUsingSettings();
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
        TransitionToMenuItemSet(searchingForPlayersMenuItemSet);
    }

    #endregion

    #region Private Methods

    void TransitionToMenuItemSet(MenuItemSet menuItemSet) {
        Menu menu = FindObjectOfType<Menu>();
        if (menu == null) {
            Debug.LogError("MatchManager: TransitionToMenuItemSet: Menu not found");
            return;
        }
        menu.TransitionToNewMenuItemSet(menuItemSet);
    }

    void StoreClientName(string defaultName) {
        clientName = GetClientName(defaultName);
        PhotonNetwork.LocalPlayer.NickName = clientName;
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

    #endregion

}
