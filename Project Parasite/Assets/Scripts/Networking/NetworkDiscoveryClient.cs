using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class NetworkDiscoveryClient : NetworkDiscovery {

    public MenuItemSet searchingForGameMenuItemSet;
    public MenuItemSet searchingForPlayersClientMenuItemSet;
    public GameObject ClientInformationPrefab;

    // Necessary to fix Unity bug where onReceivedBroadcast will be called multiple times
    bool hasRecievedBroadcastAtLeastOnce = true;

    void Start() {
        onEnable();
    }

    // Use onEnable instead of Start() or Awake() in case we disable/reenable upon disconnection
    protected void onEnable() {
		// Get name from input box
		GameObject nameField = GameObject.FindGameObjectWithTag("Name Field");
		string clientName = "";
		if (nameField != null) {
			clientName = nameField.GetComponent<InputField>().text;
		}
		clientName = (clientName == "") ? "Anonymous Client" : clientName;
        Instantiate(ClientInformationPrefab).GetComponent<ClientInformation>().clientName = clientName;

        Initialize();
        hasRecievedBroadcastAtLeastOnce = false;
        StartAsClient();
		Debug.Log("NetworkDiscoveryClient: Client Started");

        Menu menu = FindObjectOfType<Menu>();
        if (menu == null) {
            Debug.LogError("NetworkDiscoveryClient: onEnable: Menu not found");
            return;
        }
        menu.TransitionToNewMenuItemSet(searchingForGameMenuItemSet);
    }

    public override void OnReceivedBroadcast(string fromAddress, string data)
    {

        Debug.Log("NetworkDiscoveryClient: Received broadcast from: " + fromAddress+ " with the data: " + data);
        if (hasRecievedBroadcastAtLeastOnce) {
            // Avoid Unity racetrack bug
            return;
        }
        hasRecievedBroadcastAtLeastOnce = true;

		NetworkManager.singleton.networkAddress = fromAddress;
		NetworkManager.singleton.StartClient();

        Menu menu = FindObjectOfType<Menu>();
        if (menu == null) {
            Debug.LogError("NetworkDiscoveryClient: onReceivedBroadcast: Menu not found");
            return;
        }
        menu.TransitionToNewMenuItemSet(searchingForPlayersClientMenuItemSet);
		
        // Deactivate Network Discovery Client
        // gameObject.SetActive(false);
        // If we get disconnected, simply re-enable this game object to start discovery again
    }

}
