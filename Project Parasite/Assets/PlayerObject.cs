using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class PlayerObject : NetworkBehaviour {

	public GameObject ParasitePrefab;
	public GameObject HunterPrefab;
	public GameObject RoundManagerPrefab;

	private GameObject playerCharacterGameObject;

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
		if (hasAuthority && playerCharacterGameObject != null) {
			NetworkServer.Destroy(playerCharacterGameObject);
		}
	}

	// Commands
	[Command]
	public void CmdSpawnPlayerCharacter(string characterType) {
		SpawnPlayerCharacter(characterType);
	}

	public void SpawnPlayerCharacter(string characterType, Vector3 atPosition = new Vector3()) {
		GameObject playerCharacterPrefab = characterType == "PARASITE" ? ParasitePrefab : HunterPrefab;
		// Create PlayerCharacter game object on the server
		playerCharacterGameObject = Instantiate(playerCharacterPrefab, atPosition, Quaternion.identity);
		// Propogate to all clients
		NetworkServer.SpawnWithClientAuthority(playerCharacterGameObject, connectionToClient);
		// Get PlayerCharacter script
		PlayerCharacter playerCharacter = playerCharacterGameObject.GetComponentInChildren<PlayerCharacter>();
		// Initialize each player's character on their own client
		playerCharacter.RpcGeneratePhysicsEntity();
		playerCharacter.playerObject = this;
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
