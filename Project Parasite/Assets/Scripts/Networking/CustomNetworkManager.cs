using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class CustomNetworkManager : NetworkManager {

	public override void OnClientConnect(NetworkConnection conn) {

		base.OnClientConnect(conn);
		NetworkDiscoveryClient networkDiscoveryClient = FindObjectOfType<NetworkDiscoveryClient>();
		if (networkDiscoveryClient == null) {
			Debug.Log("CustomNetworkManager: Network Discovery Client not found.");
			return;
		}
		Debug.Log("CustomNetworkManager: Client: Connected to " + conn.connectionId + ", host ID: " + conn.hostId);
		networkDiscoveryClient.StopBroadcast();
	}

	public override void OnServerConnect(NetworkConnection conn) {
		base.OnServerConnect(conn);
 
        int cid = conn.connectionId;
        int hid = conn.hostId;
 
        Debug.Log("CustomNetworkManager: Server: Client with CID " + cid + " connected, host " + hid);
	}
}
