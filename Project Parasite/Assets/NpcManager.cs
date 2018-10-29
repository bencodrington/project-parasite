using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class NpcManager : NetworkBehaviour {

	public GameObject NpcPrefab;
	List<NonPlayerCharacter> NpcList;

	// How many NPCs are being spawned each round
	const int MIN_NPC_COUNT = 1;
	const int MAX_NPC_COUNT = 3;

	// Each NPC will be spawned at position
	// 	([-SPAWN_RANGE_X...SPAWN_RANGE_X], [-SPAWN_RANGE_Y...SPAWN_RANGE_Y])
	const int SPAWN_RANGE_X = 6;
	const int SPAWN_RANGE_Y = 5;

	void Start () {
		NpcList = new List<NonPlayerCharacter>();
		SpawnNPCs();
	}

	public void DespawnNPCs() {
		// Remove NPCs
		foreach (NonPlayerCharacter npc in NpcList) {
			NetworkServer.Destroy(npc.gameObject);
		}
		NpcList.Clear();
	}

	void SpawnNPCs() {
		int npcCount = Random.Range(MIN_NPC_COUNT, MAX_NPC_COUNT + 1);
		Vector3 spawnPos;
		NonPlayerCharacter npc;
		for (int i = 0; i < npcCount; i++) {
			spawnPos = new Vector3(
				Random.Range(-SPAWN_RANGE_X, SPAWN_RANGE_X),
				Random.Range(-SPAWN_RANGE_Y, SPAWN_RANGE_Y),
				0
			);
			npc = Instantiate(NpcPrefab, spawnPos, Quaternion.identity).GetComponentInChildren<NonPlayerCharacter>();
			NpcList.Add(npc);

			// Propogate to all clients
			NetworkServer.Spawn(npc.gameObject);
			npc.RpcSetColour();
		}
	}

}
