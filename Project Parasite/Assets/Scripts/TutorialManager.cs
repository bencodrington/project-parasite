using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class TutorialManager
{

    #region [Public Variables]

    public static int parasitesStillAlive;
    
    #endregion

    #region [Private Variables]
    
    Vector2 PARASITE_SPAWN_COORDINATES = new Vector2(111.5f, -83.5f);
    Vector2 HUNTER_SPAWN_COORDINATES = new Vector2(-198, -58f);

    CharacterSpawner characterSpawner;
    NpcManager npcManager;
    // Used for spawning and keeping track of static orbs,
    //  not associated with any given hunter
    OrbManager orbManager;

    CharacterType characterType;

    GameObject pauseOverlay;
    ControlInfoZone[] controlInfoZones;
    
    #endregion

    #region [Public Methods]

    public TutorialManager(CharacterType type, NpcSpawnData spawnData = null) {
        parasitesStillAlive = 0;
        characterSpawner = type == CharacterType.Parasite ? new CharacterSpawner(Restart) : new CharacterSpawner();
        characterType = type;
        SpawnPlayer();
        UiManager.Instance.SetCharacterType(type);
        UiManager.Instance.DeactivateControls();
        controlInfoZones = GameObject.FindObjectsOfType<ControlInfoZone>();

        if (spawnData != null) {
            npcManager = new NpcManager(spawnData);
        }
        if (type == CharacterType.Parasite) {
            // Spawn static orbs
            GameObject orbManagerPrefab = Resources.Load("OrbManager") as GameObject;
            orbManager = GameObject.Instantiate(orbManagerPrefab).GetComponent<OrbManager>();
            new ParasiteTutorialWinCondition();
        } else {
            new HunterTutorialNpcWinCondition();
        }
        InstantiatePauseOverlay();
        SetPauseOverlayActive(false);
    }

	public void PhysicsUpdate() {
		// Update all characters in the scene
		// OPTIMIZE: (maybe by going through the cached players and calling PhysicsUpdate for them?)
		foreach(Character character in GameObject.FindObjectsOfType<Character>()) {
			character.PhysicsUpdate();
		}
        Vector2 characterPosition = characterSpawner.GetCharacter().transform.position;
        foreach(ControlInfoZone zone in controlInfoZones) {
            zone.OnUpdate(characterPosition);
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
        GameObject.Destroy(pauseOverlay);
    }

    public void Restart(CharacterSpawner spawner) {
        // Respawn player
        spawner.DestroyCharacter();
        SpawnPlayer();
        UiManager.Instance.RemoveHud();
        UiManager.Instance.SetCharacterType(characterType);
        UiManager.Instance.DeactivateControls();
        foreach(ControlInfoZone zone in controlInfoZones) {
            zone.Reset();
        }
        // Respawn NPCs
        npcManager.Restart();
    }

    // CLEANUP: this and its associated variables should probably be extracted
    //  to a separate class
    public static void OnParasiteKilled(CharacterSpawner spawner) {
        spawner.DestroyCharacter();
        if (parasitesStillAlive == 0) {
            UiManager.Instance.SetReturnToMenuPanelActive(true);
        }
    }

    public void TogglePauseOverlay() {
        SetPauseOverlayActive(!pauseOverlay.activeInHierarchy);
    }
    
    #endregion

    #region [Private Methods]
    
    void SpawnPlayer() {
        Vector2 spawnCoords = characterType == CharacterType.Parasite ? PARASITE_SPAWN_COORDINATES : HUNTER_SPAWN_COORDINATES;
        characterSpawner.SpawnPlayerCharacter(characterType, spawnCoords, Vector2.zero);
    }

    void InstantiatePauseOverlay() {
        GameObject overlayPrefab = Resources.Load("Tutorial Pause Overlay") as GameObject;
        pauseOverlay = GameObject.Instantiate(overlayPrefab, Vector3.zero, Quaternion.identity);
        // NOTE: the 'false' in the SetParent call is necessary when using the Canvas Scaler
        pauseOverlay.transform.SetParent(UiManager.Instance.GetCanvas(), false);
    }

    void SetPauseOverlayActive(bool isActive) {
        pauseOverlay.SetActive(isActive);
    }
    
    #endregion

}
