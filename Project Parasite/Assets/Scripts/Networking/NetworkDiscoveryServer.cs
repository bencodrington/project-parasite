using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class NetworkDiscoveryServer : NetworkDiscovery {

    public MenuItemSet searchingForPlayersMenuItemSet;
    public GameObject ClientInformationPrefab;

	void Start () {
		// Get name from input box
		GameObject nameField = GameObject.FindGameObjectWithTag("Name Field");
		string clientName = "";
		if (nameField != null) {
			clientName = nameField.GetComponent<InputField>().text;
		}
		clientName = (clientName == "") ? "Anonymous Server" : clientName;
        Instantiate(ClientInformationPrefab).GetComponent<ClientInformation>().clientName = clientName;
		// Switch to looking for player menu
        Menu menu = FindObjectOfType<Menu>();
        if (menu == null) {
            Debug.LogError("NetworkDiscoveryServer: Start: Menu not found");
            return;
        }
        menu.TransitionToNewMenuItemSet(searchingForPlayersMenuItemSet);
		// NetworkManager.singleton.StartServer();
		NetworkManager.singleton.StartHost();
		Initialize();
		// Broadcast string in input box
		// 	for future possibility of selecting from several servers
		broadcastData = clientName;
		// IMPORTANT NOTE! Make sure broadcastData is not an empty string, otherwise you'll get an ArgumentOutOfRange exception
		StartAsServer();

		Debug.Log("NetworkDiscoveryServer: Server Started");
	}
}
