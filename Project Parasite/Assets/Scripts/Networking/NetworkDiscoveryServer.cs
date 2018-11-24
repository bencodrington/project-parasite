using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class NetworkDiscoveryServer : NetworkDiscovery {

    public MenuItemSet searchingForPlayersMenuItemSet;

	void Start () {
		// Get name from input box
		GameObject nameField = GameObject.FindGameObjectWithTag("Name Field");
		string name = "";
		if (nameField != null) {
			name = nameField.GetComponent<InputField>().text;
		}
		name = (name == "") ? "Anonymous" : name;
		PlayerGrid.Instance.localPlayerName = name;
		// Switch to looking for player menu
        Menu menu = FindObjectOfType<Menu>();
        if (menu == null) {
            Debug.LogError("NetworkDiscoveryClient: onEnable: Menu not found");
            return;
        }
        menu.TransitionToNewMenuItemSet(searchingForPlayersMenuItemSet);
		// NetworkManager.singleton.StartServer();
		NetworkManager.singleton.StartHost();
		Initialize();
		// Broadcast string in input box
		// 	for future possibility of selecting from several servers
		broadcastData = name;
		// IMPORTANT NOTE! Make sure broadcastData is not an empty string, otherwise you'll get an ArgumentOutOfRange exception
		StartAsServer();

		Debug.Log("NetworkDiscoveryServer: Server Started");
	}
}
