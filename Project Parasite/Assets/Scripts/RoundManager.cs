using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class RoundManager : NetworkBehaviour {

	PlayerObject[] connectedPlayers;

	public GameObject objectManagerPrefab;
	ObjectManager objectManager;

	public Vector2[] spawnPoints;

	Vector2 parasiteSpawnPoint;
	Vector2 hunterSpawnPoint;

	public bool isGameOver = false;

	bool huntersOnlyMode = true;
	bool DEBUG_MODE = true;

	void Start () {
		if (!isServer) { return; }
		// Cache Player Objects
		// TODO: uncache on leave
		connectedPlayers = FindObjectsOfType<PlayerObject>();
		CmdSpawnObjectManager();
		SelectSpawnPoints();
		SelectParasite();
	}

	void FixedUpdate() {
		if (objectManager != null) {
			// Update all objects in the scene
			objectManager.PhysicsUpdate();
		}
		// Update all characters in the scene
		// TODO: optimize (maybe by going through the cached players and calling PhysicsUpdate for them?)
		foreach(Character character in FindObjectsOfType<Character>()) {
			character.PhysicsUpdate();
		}
	}

	void SelectParasite() {
		int n = connectedPlayers.Length;
		Vector2 spawnPoint;
		// Randomly select one of the players to be parasite, the rest are hunters
		int indexOfParasite = Random.Range(0, n);
		if (huntersOnlyMode) { indexOfParasite = -1; }
		CharacterType characterType;
		for (int i = 0; i < n; i++) {
			if (i == indexOfParasite) {
				characterType = CharacterType.Parasite;
				spawnPoint = parasiteSpawnPoint;
			} else { // Player is a hunter
				characterType = CharacterType.Hunter;
				spawnPoint = hunterSpawnPoint;
			}
			connectedPlayers[i].CmdAssignCharacterTypeAndSpawnPoint(characterType, spawnPoint);
		}
	}
	
	void SelectSpawnPoints() {
		int n = spawnPoints.Length;
		int parasiteSpawnPointIndex, hunterSpawnPointIndex;
		// Randomly select one of the points for the parasite
		parasiteSpawnPointIndex = Random.Range(0, n);
		// And one for the hunters
		hunterSpawnPointIndex = Random.Range(0, n - 1);
		if (n > 1 && (parasiteSpawnPointIndex == hunterSpawnPointIndex)) {
			// Ensure the hunterSpawnPoint is different from the parasite's
			hunterSpawnPointIndex = n - 1;
		}
		parasiteSpawnPoint = spawnPoints[parasiteSpawnPointIndex];
		hunterSpawnPoint = spawnPoints[hunterSpawnPointIndex];

		if (DEBUG_MODE) {
			parasiteSpawnPoint = Vector2.zero;
			hunterSpawnPoint = Vector2.zero;
		}
	}

	public void EndRound() {
		foreach (PlayerObject player in connectedPlayers) {
			player.CmdEndRound();
		}
		transform.GetComponentInChildren<NpcManager>().DespawnNPCs();
		objectManager.OnRoundEnd();
	}

	// Commands
	[Command]
	void CmdSpawnObjectManager() {
		// Create new ObjectManager game object on the server
		GameObject objectManagerGameObject = Instantiate(objectManagerPrefab);
		NetworkServer.Spawn(objectManagerGameObject);
		RpcSetObjectManager(objectManagerGameObject.GetComponent<NetworkIdentity>().netId);
	}

	// ClientRpc
	[ClientRpc]
	void RpcSetObjectManager(NetworkInstanceId objectManagerNetId) {
		objectManager = Utility.GetLocalObject(objectManagerNetId, isServer).GetComponentInChildren<ObjectManager>();
		objectManager.OnRoundStart();
	}

}
