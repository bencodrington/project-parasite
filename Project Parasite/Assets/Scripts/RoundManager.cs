using System.Collections;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class RoundManager : MonoBehaviourPun {

	#region [Public Variables]

	public bool isGameOver = false;
	public MapData mapData;
	
	#endregion

	#region [Private Variables]

	GameObject objectManagerPrefab;
	ObjectManager objectManager;
	NpcManager npcManager;

	Vector2 parasiteSpawnPoint;
	Vector2 hunterSpawnPoint;

	NpcSpawnData spawnPointData;

	NoMoreNPCsWinCondition noMoreNPCs;

	Dictionary<int, CharacterType> characterSelections;

	// If this is true, spawn all players as hunters
	bool huntersOnlyMode = false;
	// If this is true, spawn all players at (0, 0)
	bool spawnPlayersAtZero = false;
	// If this is true, randomly select one of the connected
	// 	players to be the parasite
	bool shouldSelectParasiteRandomly = false;
	// If this is false, physics updates won't run, though
	// 	animations, input, etc. will still happen
	bool shouldRunPhysicsUpdate = true;
	
	#endregion

	#region [Public Methods]

	public void Initialize(NpcSpawnData spawnData, bool spawnPlayersAtZero, bool huntersOnlyMode, bool isRandomParasite, Dictionary<int, CharacterType> characterSelections) {
        // This method is only called on the master client, and runs before the Start() callback is called
		SetNpcSpawnData(spawnData);
        this.spawnPlayersAtZero = spawnPlayersAtZero;
		this.huntersOnlyMode = huntersOnlyMode;
        shouldSelectParasiteRandomly = isRandomParasite;
        this.characterSelections = characterSelections;
	}

	public void EndRound() {
		npcManager.DespawnNPCs();
		objectManager.OnRoundEnd();
		PhotonNetwork.Destroy(photonView);
	}

	public void SetShouldRunPhysicsUpdate(bool newValue) {
		shouldRunPhysicsUpdate = newValue;
	}

	public void ToggleShouldRunPhysicsUpdate() {
		shouldRunPhysicsUpdate = !shouldRunPhysicsUpdate;
		Debug.Log(shouldRunPhysicsUpdate);
	}

	public void AdvanceOnePhysicsUpdate() {
		if (shouldRunPhysicsUpdate) {
			SetShouldRunPhysicsUpdate(false);
			return;
		}
		PhysicsUpdate();
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
		if (!shouldRunPhysicsUpdate) { return; }
		PhysicsUpdate();
	}
	
	#endregion


	#region [Private Methods]

	void SetNpcSpawnData(NpcSpawnData spawnData) {
		spawnPointData = spawnData;
		npcManager = new NpcManager(spawnData);
	}

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
		int n = spawnPointData.spawnPoints.Length;
		int parasiteSpawnPointIndex, hunterSpawnPointIndex;
		// Randomly select one of the points for the parasite
		parasiteSpawnPointIndex = Random.Range(0, n);
		// And one for the hunters
		hunterSpawnPointIndex = Random.Range(0, n - 1);
		if (n > 1 && (parasiteSpawnPointIndex == hunterSpawnPointIndex)) {
			// Ensure the hunterSpawnPoint is different from the parasite's
			hunterSpawnPointIndex = n - 1;
		}
		parasiteSpawnPoint = spawnPointData.spawnPoints[parasiteSpawnPointIndex].coordinates;
		hunterSpawnPoint = spawnPointData.spawnPoints[hunterSpawnPointIndex].coordinates;

		if (spawnPlayersAtZero) {
			parasiteSpawnPoint = mapData.mapOrigin + mapData.debugSpawnCoordinates;
			hunterSpawnPoint = mapData.mapOrigin + mapData.debugSpawnCoordinates;
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
		// Ensure that if we're in huntersOnlyMode, no parasite player is chosen	
		if (huntersOnlyMode) { return -1; }
		if (shouldSelectParasiteRandomly) {
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

	void PhysicsUpdate() {
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

	[PunRPC]
	void RpcSetObjectManager(int objectManagerViewId) {
		objectManager = PhotonView.Find(objectManagerViewId).GetComponentInChildren<ObjectManager>();
		// Only the master client needs to have access to the map data and instantiate the map
		if (!PhotonNetwork.IsMasterClient) { return; }
		objectManager.SetMap(mapData);
		objectManager.OnRoundStart();
	}
}
