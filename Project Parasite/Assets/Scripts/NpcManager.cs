using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class NpcManager : NetworkBehaviour {

	public GameObject NpcPrefab;
	List<NonPlayerCharacter> NpcList;

	// How many NPCs are being spawned each round
	const int MIN_NPC_COUNT = 3;
	const int MAX_NPC_COUNT = 7;

	// Each NPC will be spawned at position
	// 	([-SPAWN_RANGE_X...SPAWN_RANGE_X], [-SPAWN_RANGE_Y...SPAWN_RANGE_Y])
	// 	around its spawn center
	const float SPAWN_RANGE_X = 6;
	const float SPAWN_RANGE_Y = 2;

	// The points around which groups of NPCs will be spawned
	public Vector2[] spawnCenters;

	bool DEBUG_MODE = false;

	void Start () {
		if (!isServer) { return; }
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
			// TODO:
			// playerObject.RpcUpdateRemainingNpcCount(NpcList.Count);
		}
		if (NpcList.Count == 0) {
			// Game Over
			// TODO:
			// PlayerGrid.Instance.GetLocalPlayerObject().CmdShowGameOverScreen(CharacterType.Parasite);
		}
	}

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
		int npcCount = SelectNpcGroupSize();
		Vector2 spawnPosition;
		NonPlayerCharacter npc;
		for (int i = 0; i < npcCount; i++) {
			spawnPosition = SelectSpawnPosition(spawnCenter);
			npc = Instantiate(NpcPrefab, spawnPosition, Quaternion.identity).GetComponentInChildren<NonPlayerCharacter>();
			NpcList.Add(npc);

			// Propogate to all clients
			NetworkServer.Spawn(npc.gameObject);
			npc.RpcGeneratePhysicsEntity(Vector2.zero);
			// Ensure npc snaps to its starting position on all clients
			npc.CmdUpdatePosition(spawnPosition, true);
			StartCoroutine(npc.Idle());
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

}
