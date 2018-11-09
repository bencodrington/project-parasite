using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class NpcManager : NetworkBehaviour {

	public GameObject NpcPrefab;
	List<NonPlayerCharacter> NpcList;

	// How many NPCs are being spawned each round
	const int MIN_NPC_COUNT = 1;
	const int MAX_NPC_COUNT = 5;

	// Each NPC will be spawned at position
	// 	([-SPAWN_RANGE_X...SPAWN_RANGE_X], [-SPAWN_RANGE_Y...SPAWN_RANGE_Y])
	const float SPAWN_RANGE_X = 6;
	const float SPAWN_RANGE_Y = 2;

	void Start () {
		NpcList = new List<NonPlayerCharacter>();
		SpawnNPCs();
	}

	public void DespawnNPCs() {
		// Remove NPCs
		foreach (NonPlayerCharacter npc in NpcList) {
			if (npc == null) {
				Debug.LogError("NpcManager: Attempting to destroy an NPC that is null");
			} else {
				NetworkServer.Destroy(npc.gameObject);
			}
		}
		NpcList.Clear();
	}

	public void DespawnNpc(NetworkInstanceId npcNetId) {
		NonPlayerCharacter npc = NpcList.Find((value) => {return value.netId == npcNetId;});
		int index = NpcList.IndexOf(npc);
		NpcList.RemoveAt(index);
		NetworkServer.Destroy(npc.gameObject);
		// TODO: there has to be a more efficient way of updating this
		foreach (PlayerObject playerObject in FindObjectsOfType<PlayerObject>()) {
			playerObject.RpcUpdateRemainingNpcCount(NpcList.Count);
		}
		if (NpcList.Count == 0) {
			// Game Over
			FindObjectOfType<PlayerObject>().CmdStartGame();
		}
	}

	void SpawnNPCs() {
		int npcCount = Random.Range(MIN_NPC_COUNT, MAX_NPC_COUNT + 1);
		// TODO: there has to be a more efficient way of updating this
		foreach (PlayerObject playerObject in FindObjectsOfType<PlayerObject>()) {
			playerObject.RpcUpdateRemainingNpcCount(npcCount);
		}
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

			npc.RpcGeneratePhysicsEntity(Vector2.zero);
			// Ensure npc snaps to it's starting position on all clients
			npc.CmdUpdatePosition(spawnPos, true);
			StartCoroutine(npc.Idle());
		}
	}

}
