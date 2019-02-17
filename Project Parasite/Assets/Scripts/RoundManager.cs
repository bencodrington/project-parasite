using System.Collections;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class RoundManager : MonoBehaviourPun {

	GameObject objectManagerPrefab;
	ObjectManager objectManager;

	public Vector2[] spawnPoints;

	Vector2 parasiteSpawnPoint;
	Vector2 hunterSpawnPoint;

	public bool isGameOver = false;

	bool huntersOnlyMode = false;
	bool DEBUG_MODE = true;

	NoMoreNPCsWinCondition noMoreNPCs;

	#region [MonoBehaviour Callbacks]
	
	void Start () {
		if (!PhotonNetwork.IsMasterClient) { return; }
		objectManagerPrefab = Resources.Load("ObjectManager") as GameObject;
		SpawnObjectManager();
		// TODO:
		// SelectSpawnPoints();
		SelectParasite();
		noMoreNPCs = new NoMoreNPCsWinCondition();
	}

	void FixedUpdate() {
		if (objectManager != null) {
			// Update all objects in the scene
			objectManager.PhysicsUpdate();
		}
		// Update all characters in the scene
		// OPTIMIZE: (maybe by going through the cached players and calling PhysicsUpdate for them?)
		foreach(Character character in FindObjectsOfType<Character>()) {
			character.PhysicsUpdate();
		}
	}
	
	#endregion

	public void EndRound() {
		transform.GetComponentInChildren<NpcManager>().DespawnNPCs();
		objectManager.OnRoundEnd();
		PhotonNetwork.Destroy(photonView);
	}

	#region [Private Methods]
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
			// CLEANUP: extract to method
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

	void SpawnObjectManager() {
		GameObject oMGameObject = PhotonNetwork.Instantiate(objectManagerPrefab.name, Vector3.zero, Quaternion.identity, 0);
		objectManager = oMGameObject.GetComponent<ObjectManager>();
		photonView.RPC("RpcSetObjectManager", RpcTarget.All, objectManager.photonView.ViewID);
	}
	
	#endregion

	[PunRPC]
	void RpcSetObjectManager(int objectManagerViewId) {
		objectManager = PhotonView.Find(objectManagerViewId).GetComponentInChildren<ObjectManager>();
		objectManager.OnRoundStart();
	}
}
