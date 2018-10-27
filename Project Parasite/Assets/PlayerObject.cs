﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class PlayerObject : NetworkBehaviour {

	public GameObject PlayerCharacterPrefab;

	private GameObject playerCharacter;

	void Start () {
		if (isLocalPlayer == false) {
			// Object belongs to another player
			return;
		}
		Debug.Log("PlayerObject.Start(): isLocalPlayer, so commanding server to spawn PC");
		// Instantiate only spawns on local machine
		// Network.Spawn must be called to take advantage of NetworkIdentity
		CmdSpawnPlayerCharacter();
	}
	
	// Update is called once per frame
	void Update () {
		// Runs on everyone's computer, regardless of whether they own this player object
		
	}

	// Commands => Special functions that are only executed on the server
	[Command]
	void CmdSpawnPlayerCharacter() {
		// Create PlayerCharacter game object on the server
		playerCharacter = Instantiate(PlayerCharacterPrefab);
		// Propogate to all clients
		NetworkServer.SpawnWithClientAuthority(playerCharacter, connectionToClient);
	}
}
