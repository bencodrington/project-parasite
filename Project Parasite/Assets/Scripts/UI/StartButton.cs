using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StartButton : MonoBehaviour {

	public void StartGame() {
		NetworkDiscoveryServer networkDiscoveryServer = FindObjectOfType<NetworkDiscoveryServer>();
		if (networkDiscoveryServer == null) {
			Debug.Log("StartButton: Network Discovery Server not found.");
			return;
		}
		networkDiscoveryServer.StopBroadcast();
		Destroy(networkDiscoveryServer.gameObject);
		PlayerGrid.Instance.GetLocalPlayerObject().CmdStartGame();
	}

	public void RestartGame() {
		PlayerGrid.Instance.GetLocalPlayerObject().CmdStartGame();
	}
}
