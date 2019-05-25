using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class NpcManager {

	#region [Private Variables]
	
	GameObject npcPrefab;
	GameObject parasitePrefab;
	GameObject hunterPrefab;

	List<NonPlayerCharacter> NpcList;

	// The points around which groups of NPCs will be spawned
	NpcSpawnData spawnData;

	// How many NPCs are being spawned each round
	const int MIN_NPC_COUNT = 3;
	const int MAX_NPC_COUNT = 7;

	// Each NPC will be spawned at position
	// 	([-SPAWN_RANGE_X...SPAWN_RANGE_X], [-SPAWN_RANGE_Y...SPAWN_RANGE_Y])
	// 	around its spawn center
	const float SPAWN_RANGE_X = 6;
	const float SPAWN_RANGE_Y = 2;
	
	#endregion

	#region [Public Methods]

	public NpcManager(NpcSpawnData spawnData) {
		this.spawnData = spawnData;
		NpcList = new List<NonPlayerCharacter>();
		if (!PhotonNetwork.IsMasterClient) { return; }
		npcPrefab = (GameObject)Resources.Load("NonPlayerCharacter");
		parasitePrefab = (GameObject)Resources.Load("Parasite");
		hunterPrefab = (GameObject)Resources.Load("Hunter");
		SpawnNPCs();
		SpawnPlayableCharacters();
	}
	
	#endregion

	#region [Private Methods]
	
	void SpawnNPCs() {
		if (spawnData.shouldSpawnClusters) {
			foreach (NpcSpawnData.spawnPoint spawnPoint in spawnData.spawnPoints) {
				SpawnNpcGroup(spawnPoint);
			}
		} else {
			foreach (NpcSpawnData.spawnPoint spawnPoint in spawnData.spawnPoints) {
				SpawnNpcAtPosition(spawnPoint);
			}
		}
		object[] content = { NpcList.Count };
		EventCodes.RaiseEventAll(EventCodes.SetNpcCount, content);
	}
	
	void SpawnNpcGroup(NpcSpawnData.spawnPoint spawnCenter) {
		// This should only ever run on the Master Client
		int npcCount = SelectNpcGroupSize();
		Vector2 spawnPosition;
		for (int i = 0; i < npcCount; i++) {
			spawnPosition = SelectSpawnPosition(spawnCenter.coordinates);
			SpawnNpcAtPosition(spawnPosition);
		}
	}

	void SpawnNpcAtPosition(Vector2 coordinates, bool isStationary = false) {
		SpawnNpcAtPosition(new NpcSpawnData.spawnPoint(isStationary, coordinates));
	}

	void SpawnNpcAtPosition(NpcSpawnData.spawnPoint spawnPoint) {
		NonPlayerCharacter npc;
		npc = PhotonNetwork.Instantiate(npcPrefab.name, spawnPoint.coordinates, Quaternion.identity)
				.GetComponentInChildren<NonPlayerCharacter>();
		if (spawnPoint.isStationary) {
			npc.SetInputSource(new EmptyInputSource());
		} else {
			npc.SetInputSource(new DefaultNpcInput());
		}
		// TODO: FIX INFECT, shouldn't have to overwrite input source
		if(spawnPoint.isInfected) {
			npc.Infect();
			// Instantiate new dummy CharacterSpawner so that the npc can spawn a parasite when it
			//	gets fried
			npc.CharacterSpawner = new CharacterSpawner(); 
		}
		NpcList.Add(npc);
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

	void SpawnPlayableCharacters() {
		GameObject playableCharacterPrefab;
		Character character;
		foreach (NpcSpawnData.playableCharacterSpawnPoint spawnPoint in spawnData.playableCharacterSpawnPoints) {
			playableCharacterPrefab = spawnPoint.isParasite ? parasitePrefab : hunterPrefab;
			character = PhotonNetwork.Instantiate(playableCharacterPrefab.name, spawnPoint.coordinates, Quaternion.identity)
					.GetComponentInChildren<Character>();
			character.SetInputSource(new EmptyInputSource());
		}
	}
	
	#endregion

	public void DespawnNPCs() {
		// Remove NPCs
		foreach (NonPlayerCharacter npc in NpcList) {
			if (npc != null) {
				PhotonNetwork.Destroy(npc.gameObject);
			}
		}
		NpcList.Clear();
	}

}
