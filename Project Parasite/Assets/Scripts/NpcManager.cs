using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class NpcManager : IOnEventCallback {

	#region [Private Variables]
	
	GameObject npcPrefab;
	GameObject parasitePrefab;
	GameObject hunterPrefab;

	List<NonPlayerCharacter> NpcList;
	CharacterSpawner[] characterSpawners;

	// The points around which groups of NPCs will be spawned
	NpcSpawnData spawnData;

	// How many NPCs are being spawned each round
	const int MIN_NPC_COUNT = 3;
	const int MAX_NPC_COUNT = 7;

	// Each NPC will be spawned at position
	// 	([-SPAWN_RANGE_X...SPAWN_RANGE_X], [-SPAWN_RANGE_Y...SPAWN_RANGE_Y])
	// 	around its spawn center
	const float SPAWN_RANGE_X = 6;
	const float SPAWN_RANGE_Y = 0;
	
	#endregion

	#region [Public Methods]

	public NpcManager(NpcSpawnData spawnData) {
		PhotonNetwork.AddCallbackTarget(this);
		this.spawnData = spawnData;
		NpcList = new List<NonPlayerCharacter>();
		if (!PhotonNetwork.IsMasterClient) { return; }
		npcPrefab = (GameObject)Resources.Load("NonPlayerCharacter");
		parasitePrefab = (GameObject)Resources.Load("Parasite");
		hunterPrefab = (GameObject)Resources.Load("Hunter");
		SpawnNPCs();
	}

    public void OnEvent(EventData photonEvent) {
        if (photonEvent.Code == EventCodes.RequestNpcCount) {
            if (PhotonNetwork.IsMasterClient) {
                // Resend the current NPC count
                object[] content = { NpcList.Count };
                EventCodes.RaiseEventAll(EventCodes.SetNpcCount, content);
            }
		}
    }

	public void Restart() {
		DespawnNPCs(false);
		SpawnNPCs();
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
		SpawnPlayableCharacters();
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
			npc.SetInputSource(new StationaryNpcInput());
		} else {
			npc.SetInputSource(new DefaultNpcInput());
		}
		if(spawnPoint.isInfected) {
			npc.Infect();
			// Instantiate new dummy CharacterSpawner so that the npc can spawn a parasite when it
			//	gets fried
			npc.CharacterSpawner = new CharacterSpawner(TutorialManager.OnParasiteKilled);
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
		CharacterType type;
		Character character;
		InputSource inputSource;
		characterSpawners = new CharacterSpawner[spawnData.playableCharacterSpawnPoints.Length];
		for (int i = 0; i < spawnData.playableCharacterSpawnPoints.Length; i++) {
			NpcSpawnData.playableCharacterSpawnPoint spawnPoint = spawnData.playableCharacterSpawnPoints[i];
			if (spawnPoint.isParasite) {
				type = CharacterType.Parasite;
				characterSpawners[i] = new CharacterSpawner(TutorialManager.OnParasiteKilled);
				inputSource = new EmptyInputSource();
			} else {
				type = CharacterType.Hunter;
				characterSpawners[i] = new CharacterSpawner();
				inputSource = new HunterAiInputSource();
			}
			character = characterSpawners[i].SpawnPlayerCharacter(type, spawnPoint.coordinates, Vector2.zero, false, false, inputSource);
			// Don't draw hunter UI
			if (!spawnPoint.isParasite) {
				((Hunter)character).isNpcControlled = true;
			}
		}
	}
	
	#endregion

	public void DespawnNPCs(bool shouldRemoveCallbackTarget = true) {
		// Remove NPCs
		foreach (NonPlayerCharacter npc in NpcList) {
			if (npc != null) {
				PhotonNetwork.Destroy(npc.gameObject);
			}
		}
		NpcList.Clear();
		// Remove NPC-controlled playable characters
		for (int i = 0; i < characterSpawners.Length; i++) {
			characterSpawners[i].DestroyCharacter();
		}
		characterSpawners = null;
		if (!shouldRemoveCallbackTarget) { return; }
        PhotonNetwork.RemoveCallbackTarget(this);
	}

}
