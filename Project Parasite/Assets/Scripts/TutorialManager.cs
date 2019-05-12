using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialManager
{

    #region [Private Variables]
    
    Vector2 PARASITE_SPAWN_COORDINATES = new Vector2(-5, 0);
    Vector2 HUNTER_SPAWN_COORDINATES = new Vector2(5, 0);

    CharacterSpawner characterSpawner;
    
    #endregion

    #region [Public Methods]

    public TutorialManager(CharacterType type) {
        characterSpawner = new CharacterSpawner(false);
        Vector2 spawnCoords = type == CharacterType.Parasite ? PARASITE_SPAWN_COORDINATES : HUNTER_SPAWN_COORDINATES;
        characterSpawner.SpawnPlayerCharacter(type, spawnCoords, Vector2.zero, true);
    }
    
    #endregion
}
