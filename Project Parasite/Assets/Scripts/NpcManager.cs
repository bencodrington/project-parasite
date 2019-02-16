using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class NpcManager : MonoBehaviour {

	#region [Public Variables]
	
	// The points around which groups of NPCs will be spawned
	public Vector2[] spawnCenters;

	#endregion

	#region [Private Variables]
	
	GameObject NpcPrefab;

	List<NonPlayerCharacter> NpcList;

	// How many NPCs are being spawned each round
	const int MIN_NPC_COUNT = 1;//3;
	const int MAX_NPC_COUNT = 1;//7;

	// Each NPC will be spawned at position
	// 	([-SPAWN_RANGE_X...SPAWN_RANGE_X], [-SPAWN_RANGE_Y...SPAWN_RANGE_Y])
	// 	around its spawn center
	const float SPAWN_RANGE_X = 6;
	const float SPAWN_RANGE_Y = 2;

	bool DEBUG_MODE = false;
	
	#endregion

	#region [MonoBehaviour Callbacks]
	
	void Start () {
		NpcList = new List<NonPlayerCharacter>();
		if (!PhotonNetwork.IsMasterClient) { return; }
		NpcPrefab = (GameObject)Resources.Load("NonPlayerCharacter");
		SpawnNPCs();
	}
	
	#endregion

	#region [Private Methods]
	
	void SpawnNPCs() {
		if (DEBUG_MODE) { return; }
		foreach(Vector2 spawnCenter in spawnCenters) {
			SpawnNpcGroup(spawnCenter);
		}
		// TODO: there has to be a more efficient way of updating this
		foreach (PlayerObject playerObject in FindObjectsOfType<PlayerObject>()) {
			// TODO:
			// playerObject.RpcUpdateRemainingNpcCount(NpcList.Count);
		}
	}
	
	void SpawnNpcGroup(Vector2 spawnCenter) {
		// This should only ever run on the Master Client
		int npcCount = SelectNpcGroupSize();
		Vector2 spawnPosition;
		NonPlayerCharacter npc;
		for (int i = 0; i < npcCount; i++) {
			spawnPosition = SelectSpawnPosition(spawnCenter);
			npc = PhotonNetwork.Instantiate(NpcPrefab.name, spawnPosition, Quaternion.identity).GetComponentInChildren<NonPlayerCharacter>();
			
			npc.GeneratePhysicsEntity(Vector2.zero);
			StartCoroutine(npc.Idle());
			// TODO: run on all clients??
			NpcList.Add(npc);
		}
	}

	int SelectNpcGroupSize() {
		return Random.Range(MIN_NPC_COUNT, MAX_NPC_COUNT + 1);
	}

	Vector2 SelectSpawnPosition(Vector2 spawnCenter) {
		return spawnCenter + new Vector2(
				Random.Range(-SPAWN_RANGE_X, SPAWN_RANGE_X),
				Random.Range(-SPAWN_RANGE_Y, SPAWN_RANGE_Y)
			);
	}
	
	#endregion

	public void DespawnNPCs() {
		// Remove NPCs
		foreach (NonPlayerCharacter npc in NpcList) {
			if (npc == null) {
				Debug.LogError("NpcManager: Attempting to destroy an NPC that is null");
			} else {
				PhotonNetwork.Destroy(npc.gameObject);
			}
		}
		NpcList.Clear();
	}

}
