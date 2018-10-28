using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NpcManager : MonoBehaviour {

	public GameObject NpcPrefab;
	// TODO: convert to list of Npc C# scripts
	List<GameObject> NpcList;

	// How many NPCs are being spawned each round
	const int MIN_NPC_COUNT = 1;
	const int MAX_NPC_COUNT = 3;

	// Each NPC will be spawned at position
	// 	([-SPAWN_RANGE_X...SPAWN_RANGE_X], [-SPAWN_RANGE_Y...SPAWN_RANGE_Y])
	const int SPAWN_RANGE_X = 6;
	const int SPAWN_RANGE_Y = 5;

	void Start () {
		NpcList = new List<GameObject>();
		SpawnNPCs();
	}

	void OnDestroy() {
		// Remove NPCs
		foreach (GameObject npc in NpcList) {
			Destroy(npc);
		}
		NpcList.Clear();
	}

	void SpawnNPCs() {
		int npcCount = Random.Range(MIN_NPC_COUNT, MAX_NPC_COUNT + 1);
		Vector3 spawnPos;
		GameObject npc;
		Debug.Log("Spawning " + npcCount + " NPCs");
		for (int i = 0; i < npcCount; i++) {
			spawnPos = new Vector3(
				Random.Range(-SPAWN_RANGE_X, SPAWN_RANGE_X),
				Random.Range(-SPAWN_RANGE_Y, SPAWN_RANGE_Y),
				0
			);
			npc = Instantiate(NpcPrefab, spawnPos, Quaternion.identity);
			npc.GetComponentInChildren<SpriteRenderer>().color = Color.yellow;

			NpcList.Add(npc);
		}
		Debug.Log("SPAWN: NPC LIST COUNT: " + NpcList.Count);

		// TODO: spawn via server
	}
}
