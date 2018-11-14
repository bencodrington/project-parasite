using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StartButton : MonoBehaviour {

	public GameObject RoundManagerPrefab;

	public void StartGame() {
		NetworkDiscoveryServer networkDiscoveryServer = FindObjectOfType<NetworkDiscoveryServer>();
		if (networkDiscoveryServer == null) {
			Debug.Log("StartButton: Network Discovery Server not found.");
			return;
		}
		networkDiscoveryServer.StopBroadcast();
		Destroy(networkDiscoveryServer.gameObject);
		FindObjectOfType<ClientInformation>().localPlayer.CmdStartGame();
	}
}
