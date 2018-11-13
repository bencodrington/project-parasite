using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class NetworkDiscoveryServer : NetworkDiscovery {

    public MenuItemSet searchingForPlayersMenuItemSet;

	void Start () {
        Menu menu = FindObjectOfType<Menu>();
        if (menu == null) {
            Debug.LogError("NetworkDiscoveryClient: onEnable: Menu not found");
            return;
        }
        menu.TransitionToNewMenuItemSet(searchingForPlayersMenuItemSet);

		// NetworkManager.singleton.StartServer();
		NetworkManager.singleton.StartHost();
		Initialize();
		// TODO: broadcast data = string in input box
		// IMPORTANT NOTE! Make sure broadcastData is not an empty string, otherwise you'll get an ArgumentOutOfRange exception
		StartAsServer();

		Debug.Log("NetworkDiscoveryServer: Server Started");
	}
}
