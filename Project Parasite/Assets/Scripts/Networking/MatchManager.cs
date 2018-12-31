using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.Match;
using UnityEngine.UI;

public class MatchManager : MonoBehaviour {

    // Used to stop matches being created twice
	bool matchCreated;
    // The prefab for the object used to persist client names across menus
    public GameObject ClientInformationPrefab;

    // Prefabs for the various menu item sets this flow requires
    public MenuItemSet searchingForMatchMenuItemSet;
    public MenuItemSet searchingForPlayersMenuItemSet;
    public MenuItemSet searchingForPlayersClientMenuItemSet;


    void Start() {
        NetworkManager.singleton.StartMatchMaker();
    }

    void StoreClientName(string defaultName) {
        string clientName = GetClientName(defaultName);
        Instantiate(ClientInformationPrefab).GetComponent<ClientInformation>().clientName = clientName;
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

    void TransitionToMenuItemSet(MenuItemSet menuItemSet) {
        Menu menu = FindObjectOfType<Menu>();
        if (menu == null) {
            Debug.LogError("MatchManager: TransitionToMenuItemSet: Menu not found");
            return;
        }
        menu.TransitionToNewMenuItemSet(menuItemSet);
    }

    // Server
	public void CreateRoom() {
		string matchName = "room";
		uint matchSize = 4;
		bool matchAdvertise = true;
		string matchPassword = "";

        StoreClientName("Anonymous Server");
        NetworkManager.singleton.matchMaker.CreateMatch(matchName, matchSize, matchAdvertise, matchPassword, "", "", 0, 0, OnMatchCreate);
        TransitionToMenuItemSet(searchingForPlayersMenuItemSet);
	}

	public void OnMatchCreate(bool success, string extendedInfo, MatchInfo matchInfo) {
        if (success) {
            Debug.Log("MatchManager[On Server]: OnMatchCreate: Create match succeeded");
            matchCreated = true;
            NetworkServer.Listen(matchInfo, 9000);
            NetworkManager.singleton.StartHost(matchInfo);
        } else {
            Debug.LogError("Create match failed: " + extendedInfo);
        }
    }

    // Client
    public void ListMatches() {
        StoreClientName("Anonymous Client");
        string matchName = "";
        NetworkManager.singleton.matchMaker.ListMatches(0, 10, matchName, true, 0, 0, OnMatchList);
        TransitionToMenuItemSet(searchingForMatchMenuItemSet);
    }

    public void OnMatchList(bool success, string extendedInfo, List<MatchInfoSnapshot> matches) {
        if (success && matches != null && matches.Count > 0) {
            NetworkManager.singleton.matchMaker.JoinMatch(matches[0].networkId, "", "", "", 0, 0, OnMatchJoined);
        } else if (!success) {
            Debug.LogError("MatchManager[On Client]: OnMatchList: Couldn't connect to match maker: " + extendedInfo);
        }

        // TODO: if no matches, try again in x amount of time
    }

    public void OnMatchJoined(bool success, string extendedInfo, MatchInfo matchInfo) {
        if (success) {
            Debug.Log("MatchManager[On Client]: OnMatchJoined: Join match succeeded");
            if (matchCreated) {
                Debug.LogWarning("Match already set up, aborting...");
                return;
            }
            NetworkManager.singleton.StartClient(matchInfo);
            TransitionToMenuItemSet(searchingForPlayersClientMenuItemSet);
        } else {
            Debug.LogError("Join match failed " + extendedInfo);
        }
    }
}
