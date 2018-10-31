using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class PlayerObject : NetworkBehaviour {

	public GameObject ParasitePrefab;
	public GameObject HunterPrefab;
	public GameObject RoundManagerPrefab;

	private GameObject playerCharacter;

	void Start () {
		if (isLocalPlayer == false) {
			// Object belongs to another player
			return;
		}
	}

	void Update () {
		// Runs on everyone's computer, regardless of whether they own this player object
		if (isLocalPlayer == false) {
			return;
		}

		if (Input.GetKeyDown(KeyCode.E)) {
			CmdStartGame();
		}
	}

	public void DestroyCharacter() {
		if (hasAuthority) {
			NetworkServer.Destroy(playerCharacter);
		}
	}

	// Commands
	[Command]
	public void CmdSpawnPlayerCharacter(string characterType) {
		GameObject playerCharacterPrefab = characterType == "PARASITE" ? ParasitePrefab : HunterPrefab;
		// Create PlayerCharacter game object on the server
		playerCharacter = Instantiate(playerCharacterPrefab);
		// Propogate to all clients
		NetworkServer.SpawnWithClientAuthority(playerCharacter, connectionToClient);
		// Initialize each player's character on their own client
		playerCharacter.GetComponentInChildren<PlayerCharacter>().RpcGeneratePhysicsEntity();
	}

	[Command]
	void CmdStartGame() {
		foreach (RoundManager rm in FindObjectsOfType<RoundManager>()) {
			rm.EndRound();
			Destroy(rm.gameObject);
		}
		// Create new RoundManager game object on the server
		Instantiate(RoundManagerPrefab);
	}
}
