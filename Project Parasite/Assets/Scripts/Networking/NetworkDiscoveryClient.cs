using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class NetworkDiscoveryClient : NetworkDiscovery {

    // Necessary to fix Unity bug where onReceivedBroadcast will be called multiple times
    bool hasRecievedBroadcastAtLeastOnce = true;

    void Start() {
        onEnable();
    }

    // Use onEnable instead of Start() or Awake() in case we disable/reenable upon disconnection
    protected void onEnable() {
        /*
        if ( DURING DEVELOPMENT) {
            OnReceivedBroadcast("localhost", "");
            return;
        }
         */

        Initialize();
        hasRecievedBroadcastAtLeastOnce = false;
        StartAsClient();
		Debug.Log("NetworkDiscoveryClient: Client Started");
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
