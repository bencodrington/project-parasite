using System.Collections;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class RoundManager : MonoBehaviourPun {

	#region [Public Variables]

	public Vector2[] spawnPoints;

	public bool isGameOver = false;
	
	#endregion

	#region [Private Variables]

	GameObject objectManagerPrefab;
	ObjectManager objectManager;

	Vector2 parasiteSpawnPoint;
	Vector2 hunterSpawnPoint;

	NoMoreNPCsWinCondition noMoreNPCs;

	Dictionary<int, CharacterType> characterSelections;

	// If this is true, spawn all players as hunters
	bool huntersOnlyMode = false;
	// If this is true, spawn all players at (0, 0)
	bool spawnPlayersAtZero = false;
	// If this is true, randomly select one of the connected
	// 	players to be the parasite
	bool shouldSelectParasiteRandomly = false;
	
	#endregion

	#region [Public Methods]

	public void EndRound() {
		transform.GetComponentInChildren<NpcManager>().DespawnNPCs();
		objectManager.OnRoundEnd();
		PhotonNetwork.Destroy(photonView);
	}

	public void SetHuntersOnlyMode(bool value) {
		huntersOnlyMode = value;
	}

	public void SetSpawnPlayersAtZero(bool value) {
		spawnPlayersAtZero = value;
	}

	public void SetSelectParasiteRandomly(bool isRandom) {
		shouldSelectParasiteRandomly = isRandom;
	}

	public void SetCharacterSelections(Dictionary<int, CharacterType> selections) {
		characterSelections = selections;
	}

	#endregion
	
	#region [MonoBehaviour Callbacks]
	
	void Start () {
		noMoreNPCs = new NoMoreNPCsWinCondition();
		if (!PhotonNetwork.IsMasterClient) { return; }
		objectManagerPrefab = Resources.Load("ObjectManager") as GameObject;
		SpawnObjectManager();
		SelectSpawnPoints();
		SelectParasite();
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


	#region [Private Methods]

	void SelectParasite() {
		Vector2 spawnPoint;
		int actorNumber;
		int parasiteActorNumber = GetActorNumberOfParasitePlayer();
		CharacterType characterType;
		foreach (Player player in PhotonNetwork.PlayerList) {
			actorNumber = player.ActorNumber;
			if (actorNumber == parasiteActorNumber) {
				characterType = CharacterType.Parasite;
				spawnPoint = parasiteSpawnPoint;
			} else { // Player is a hunter
				characterType = CharacterType.Hunter;
				spawnPoint = hunterSpawnPoint;
			}
			RaiseSpawnEvent(actorNumber, characterType, spawnPoint);
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

		if (spawnPlayersAtZero) {
			parasiteSpawnPoint = Vector2.zero;
			hunterSpawnPoint = Vector2.zero;
		}
	}

	void SpawnObjectManager() {
		GameObject oMGameObject = PhotonNetwork.Instantiate(objectManagerPrefab.name, Vector3.zero, Quaternion.identity, 0);
		objectManager = oMGameObject.GetComponent<ObjectManager>();
		photonView.RPC("RpcSetObjectManager", RpcTarget.All, objectManager.photonView.ViewID);
	}

	void RaiseSpawnEvent(int actorNumber, CharacterType characterType, Vector2 spawnPoint) {
		byte eventCode = EventCodes.AssignPlayerTypeAndSpawnPoint;
		object[] content = { actorNumber, characterType, spawnPoint };
		EventCodes.RaiseEventAll(eventCode, content);
	}
	
	int GetActorNumberOfParasitePlayer() {
		if (shouldSelectParasiteRandomly) {
			// Ensure that if we're in huntersOnlyMode, no parasite player is chosen	
			if (huntersOnlyMode) { return -1; }
			// Randomly select one of the players to be parasite, the rest are hunters
			int indexOfParasite = Random.Range(0, PhotonNetwork.PlayerList.Length);
			return PhotonNetwork.PlayerList[indexOfParasite].ActorNumber;
		}
		foreach (int key in characterSelections.Keys) {
			if (characterSelections[key] == CharacterType.Parasite) {
				return key;
			}
		}
		Debug.LogError("RoundManager:GetActorNumberOfParasitePlayer: No entry with parasite selected found.");
		return PhotonNetwork.PlayerList[0].ActorNumber;
	}

	#endregion

	[PunRPC]
	void RpcSetObjectManager(int objectManagerViewId) {
		objectManager = PhotonView.Find(objectManagerViewId).GetComponentInChildren<ObjectManager>();
		objectManager.OnRoundStart();
	}
}
