using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.Match;
using UnityEngine.Networking.Types;
using UnityEngine.UI;

public class MatchManager : MonoBehaviour {

    // Used to stop matches being created twice
	bool matchCreated;
    // The prefab for the object used to persist client names across menus
    public GameObject ClientInformationPrefab;
    // The prefab for the button that's spawned for each available server when listing open matches
    public GameObject OpenServerButtonPrefab;
    // List of available server buttons
    List<GameObject> openServerButtons;

    // How often the list of servers should be refreshed (seconds between refreshes)
    const float SERVER_LIST_REFRESH_RATE = 1f;
    // The current client's name, stored here for easy access when creating matches
    string clientName;
    // The coroutine for refreshing the server list
    Coroutine listMatchCoroutine;

    // Prefabs for the various menu item sets this flow requires
    public MenuItemSet searchingForMatchMenuItemSet;
    public MenuItemSet searchingForPlayersMenuItemSet;
    public MenuItemSet searchingForPlayersClientMenuItemSet;


    void Start() {
        openServerButtons = new List<GameObject>();
        NetworkManager.singleton.StartMatchMaker();
    }

    void StoreClientName(string defaultName) {
        clientName = GetClientName(defaultName);
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
		uint matchSize = 4;
		bool matchAdvertise = true;
		string matchPassword = "";

        StoreClientName("Anonymous Server");
        NetworkManager.singleton.matchMaker.CreateMatch(clientName, matchSize, matchAdvertise, matchPassword, "", "", 0, 0, OnMatchCreate);
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
    public void TransitionToListMatches() {
        StoreClientName("Anonymous Client");
        ListMatches();
        TransitionToMenuItemSet(searchingForMatchMenuItemSet);
    }

    void ListMatches() {
        string matchName = "";
        NetworkManager.singleton.matchMaker.ListMatches(0, 10, matchName, true, 0, 0, OnMatchList);
        if (listMatchCoroutine != null) {
            StopCoroutine(listMatchCoroutine);
        }
        listMatchCoroutine = StartCoroutine(Utility.WaitXSeconds(1, ListMatches));
    }

    public void OnMatchList(bool success, string extendedInfo, List<MatchInfoSnapshot> matches) {
        if (success && matches != null) {
            UpdateOpenServerList(matches);
        } else if (!success) {
            Debug.LogError("MatchManager[On Client]: OnMatchList: Couldn't connect to match maker: " + extendedInfo);
        }
    }

    void UpdateOpenServerList(List<MatchInfoSnapshot> matches) {
        // TODO: this can be optimized by diffing previous matches with current matches
        GameObject newButton;
        Menu menu = FindObjectOfType<Menu>();
        if (menu == null) {
            Debug.LogError("MatchManager: UpdateOpenServerList: Menu not found");
            return;
        }
        // Remove current match buttons
        foreach(GameObject button in openServerButtons) {
            menu.RemoveMenuItem(button);
            Destroy(button);
        }
        // Remove references to freshly destroyed button objects
        openServerButtons.Clear();
        // Find menu
        // Create new match buttons
        foreach(MatchInfoSnapshot match in matches) {
            // Create new button as the second-last child of the menu ("searching..." text is the last one)
            newButton = menu.AddNewItemAtIndex(OpenServerButtonPrefab, menu.transform.childCount - 1);
            // Set its text to reflect the match's name
            newButton.GetComponentInChildren<Text>().text = match.name + "'s game";
            // Join match on button press
            newButton.GetComponentInChildren<Button>().onClick.AddListener(delegate { JoinMatch(match.networkId); });
            // Store it for removal next update
            openServerButtons.Add(newButton);
        }
    }

    void JoinMatch(NetworkID matchNetworkId) {
        NetworkManager.singleton.matchMaker.JoinMatch(matchNetworkId, "", "", "", 0, 0, OnMatchJoined);
        if (listMatchCoroutine != null) {
            StopCoroutine(listMatchCoroutine);
        }
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
