using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Networking;

public class RoundManager : MonoBehaviour {

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
		if (!PhotonNetwork.IsMasterClient) { return; }
		// TODO:
		// SpawnObjectManager();
		// TODO:
		// SelectSpawnPoints();
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
		int n = PhotonNetwork.PlayerList.Length;
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
			// TODO: extract to method
			byte eventCode = EventCodes.AssignPlayerType;
			object[] content = { PhotonNetwork.PlayerList[i].ActorNumber, characterType };
			EventCodes.RaiseEventAll(eventCode, content);
			// TODO: remove
			// connectedPlayers[i].CmdAssignCharacterTypeAndSpawnPoint(characterType, spawnPoint);
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
			// TODO:
			// player.CmdEndRound();
		}
		transform.GetComponentInChildren<NpcManager>().DespawnNPCs();
		objectManager.OnRoundEnd();
	}

	void SpawnObjectManager() {
		PhotonNetwork.Instantiate(objectManagerPrefab.name, Vector3.zero, Quaternion.identity, 0);
		// RpcSetObjectManager(objectManagerGameObject.GetComponent<NetworkIdentity>().netId);
	}

	// // ClientRpc
	// [ClientRpc]
	// void RpcSetObjectManager(NetworkInstanceId objectManagerNetId) {
	// 	objectManager = Utility.GetLocalObject(objectManagerNetId, isServer).GetComponentInChildren<ObjectManager>();
	// 	objectManager.OnRoundStart();
	// }

}
