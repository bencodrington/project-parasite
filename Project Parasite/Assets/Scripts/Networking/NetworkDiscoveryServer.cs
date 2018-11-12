using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class NetworkDiscoveryServer : NetworkDiscovery {

	void Start () {
		// NetworkManager.singleton.StartServer();
		NetworkManager.singleton.StartHost();
		Initialize();
		// TODO: broadcast data = string in input box
		// IMPORTANT NOTE! Make sure broadcastData is not an empty string, otherwise you'll get an ArgumentOutOfRange exception
		StartAsServer();

		Debug.Log("NetworkDiscoveryServer: Server Started");
	}
}
