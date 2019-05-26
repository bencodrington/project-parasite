using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class TutorialManager
{

    #region [Private Variables]
    
    Vector2 PARASITE_SPAWN_COORDINATES = new Vector2(113, -56.4f);
    Vector2 HUNTER_SPAWN_COORDINATES = new Vector2(-191, -5.5f);

    CharacterSpawner characterSpawner;
    NpcManager npcManager;
    // Used for spawning and keeping track of static orbs,
    //  not associated with any given hunter
    OrbManager orbManager;

    CharacterType characterType;
    
    #endregion

    #region [Public Methods]

    public TutorialManager(CharacterType type, NpcSpawnData spawnData = null) {
        characterSpawner = new CharacterSpawner();
        characterType = type;
        SpawnPlayer();
        UiManager.Instance.SetCharacterType(type);
        if (spawnData != null) {
            npcManager = new NpcManager(spawnData);
        }
        if (type == CharacterType.Parasite) {
            // Spawn static orbs
            GameObject orbManagerPrefab = AssetDatabase.LoadAssetAtPath("Assets/Prefabs/Objects/OrbManager.prefab", typeof(GameObject)) as GameObject;
            orbManager = GameObject.Instantiate(orbManagerPrefab).GetComponent<OrbManager>();
            new ParasiteTutorialWinCondition();
        }
    }

	public void PhysicsUpdate() {
		// Update all characters in the scene
		// OPTIMIZE: (maybe by going through the cached players and calling PhysicsUpdate for them?)
		foreach(Character character in GameObject.FindObjectsOfType<Character>()) {
			character.PhysicsUpdate();
		}
	}

    public void End() {
        characterSpawner.DestroyCharacter();
        characterSpawner = null;
        npcManager.DespawnNPCs();
        npcManager = null;
        if (orbManager == null) { return; }
        orbManager.DestroyOrbs();
        orbManager = null;
    }

    public void Restart() {
        // Respawn player
        characterSpawner.DestroyCharacter();
        SpawnPlayer();
        // Respawn NPCs
        npcManager.Restart();
    }
    
    #endregion

    #region [Private Methods]
    
    void SpawnPlayer() {
        Vector2 spawnCoords = characterType == CharacterType.Parasite ? PARASITE_SPAWN_COORDINATES : HUNTER_SPAWN_COORDINATES;
        characterSpawner.SpawnPlayerCharacter(characterType, spawnCoords, Vector2.zero, true, true, null, Restart);
    }
    
    #endregion

}
