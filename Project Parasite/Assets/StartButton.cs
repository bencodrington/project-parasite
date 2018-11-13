using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StartButton : MonoBehaviour {

	public GameObject RoundManagerPrefab;

	public void StartGame() {
		foreach (RoundManager rm in FindObjectsOfType<RoundManager>()) {
			rm.EndRound();
			Destroy(rm.gameObject);
		}
		// Create new RoundManager game object on the server
		Instantiate(RoundManagerPrefab);
		
        Menu menu = FindObjectOfType<Menu>();
        if (menu == null) {
            Debug.LogError("NetworkDiscoveryClient: onReceivedBroadcast: Menu not found");
            return;
        }
        menu.DeleteMenuItems();

		NetworkDiscoveryServer networkDiscoveryServer = FindObjectOfType<NetworkDiscoveryServer>();
		if (networkDiscoveryServer == null) {
			Debug.Log("StartButton: Network Discovery Server not found.");
			return;
		}
		networkDiscoveryServer.StopBroadcast();
		Destroy(networkDiscoveryServer.gameObject);
	}
}
