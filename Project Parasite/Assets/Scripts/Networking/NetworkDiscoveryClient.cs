using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class NetworkDiscoveryClient : NetworkDiscovery {

    public MenuItemSet searchingForGameMenuItemSet;

    // Necessary to fix Unity bug where onReceivedBroadcast will be called multiple times
    bool hasRecievedBroadcastAtLeastOnce = true;

    void Start() {
        onEnable();
    }

    // Use onEnable instead of Start() or Awake() in case we disable/reenable upon disconnection
    protected void onEnable() {
		// Get name from input box
		GameObject nameField = GameObject.FindGameObjectWithTag("Name Field");
		string name = "";
		if (nameField != null) {
			name = nameField.GetComponent<InputField>().text;
		}
		name = (name == "") ? "Anonymous Client" : name;

        Initialize();
        hasRecievedBroadcastAtLeastOnce = false;
        StartAsClient();
		Debug.Log("NetworkDiscoveryClient: Client Started");
		// IMPORTANT NOTE! PlayerGrid is not activated until StartAsClient() is called
		// 	attempting to reference it before that will cause errors
		PlayerGrid.Instance.localPlayerName = name;

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
        menu.DeleteMenuItems();
		
        // Deactivate Network Discovery Client
        // gameObject.SetActive(false);
        // If we get disconnected, simply re-enable this game object to start discovery again
    }

}
