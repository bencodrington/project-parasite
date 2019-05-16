using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialManager
{

    #region [Private Variables]
    
    Vector2 PARASITE_SPAWN_COORDINATES = new Vector2(113, -56.4f);
    Vector2 HUNTER_SPAWN_COORDINATES = new Vector2(-191, -5.5f);

    CharacterSpawner characterSpawner;
    NpcManager npcManager;
    
    #endregion

    #region [Public Methods]

    public TutorialManager(CharacterType type, NpcSpawnData spawnData = null) {
        characterSpawner = new CharacterSpawner();
        Vector2 spawnCoords = type == CharacterType.Parasite ? PARASITE_SPAWN_COORDINATES : HUNTER_SPAWN_COORDINATES;
        characterSpawner.SpawnPlayerCharacter(type, spawnCoords, Vector2.zero, true);
        if (spawnData != null) {
            npcManager = new NpcManager(spawnData);
        }
    }

	public void PhysicsUpdate() {
		// Update all characters in the scene
		// OPTIMIZE: (maybe by going through the cached players and calling PhysicsUpdate for them?)
		foreach(Character character in GameObject.FindObjectsOfType<Character>()) {
			character.PhysicsUpdate();
		}
	}
    
    #endregion
}
