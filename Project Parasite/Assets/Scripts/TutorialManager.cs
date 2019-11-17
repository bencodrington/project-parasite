using UnityEngine;

public class TutorialManager
{

    #region [Public Variables]

    public static int parasitesStillAlive;
    
    #endregion

    #region [Private Variables]
    
    // Parasite tutorial, checkpoint right before first NPC: 
    // Vector2 PARASITE_SPAWN_COORDINATES = new Vector2(109, -59);
    // Actual spawn coordinates:
    Vector2 PARASITE_SPAWN_COORDINATES = new Vector2(81.5f, -66.75f);
    Vector2 HUNTER_SPAWN_COORDINATES = new Vector2(-198, -58f);

    CharacterSpawner characterSpawner;
    NpcManager npcManager;
    // Used for spawning and keeping track of static orbs,
    //  not associated with any given hunter
    OrbManager orbManager;

    CharacterType characterType;

    GameObject pauseOverlay;
    TriggerZone[] triggerZones;
    InfoScreen[] infoScreens;
    NPCInfectionDetector detector;

    string localName;
    
    #endregion

    #region [Public Methods]

    public TutorialManager(CharacterType type, NpcSpawnData spawnData = null, string nickname = "") {
        parasitesStillAlive = 0;
        characterSpawner = type == CharacterType.Parasite ? new CharacterSpawner(Restart) : new CharacterSpawner();
        characterType = type;
        localName = nickname;
        SpawnPlayer();
        UiManager.Instance.SetCharacterType(type);
        UiManager.Instance.DeactivateControls();
        triggerZones = GameObject.FindObjectsOfType<TriggerZone>();
        infoScreens = GameObject.FindObjectsOfType<InfoScreen>();
        detector = GameObject.FindObjectOfType<NPCInfectionDetector>();

        if (spawnData != null) {
            npcManager = new NpcManager(spawnData);
            detector.ScanForNPCs();
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
        // Note: when caching characters, account for new characters spawning (e.g. parasite disinfect)
		foreach(Character character in GameObject.FindObjectsOfType<Character>()) {
			character.PhysicsUpdate();
		}
        Vector2 characterPosition = characterSpawner.GetCharacter().transform.position;
        foreach(TriggerZone zone in triggerZones) {
            zone.OnUpdate(characterPosition);
        }
	}

    public void End() {
        characterSpawner.DestroyCharacter();
        characterSpawner = null;
        npcManager.DespawnNPCs();
        npcManager = null;
        GameObject.Destroy(pauseOverlay);
        // OrbManager is used in the parasite tutorial to manage the static
        //  orbs that are part of the level
        if (orbManager != null) {
            orbManager.DestroyOrbs();
            orbManager = null;
        }
        foreach(TriggerZone zone in triggerZones) {
            zone.Reset();
        }
        foreach(InfoScreen screen in infoScreens) {
            screen.Reset();
        }
    }

    public void Restart(CharacterSpawner spawner) {
        // Respawn player
        spawner.DestroyCharacter();
        SpawnPlayer();
        UiManager.Instance.RemoveHud();
        UiManager.Instance.SetCharacterType(characterType);
        UiManager.Instance.DeactivateControls();
        foreach(TriggerZone zone in triggerZones) {
            zone.Reset();
        }
        foreach(InfoScreen screen in infoScreens) {
            screen.Reset();
        }
        // Respawn NPCs
        npcManager.Restart();
        detector.Reset();
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
        characterSpawner.SpawnPlayerCharacter(
            characterType,
            spawnCoords,
            Vector2.zero,
            true,
            true,
            null,
            false,
            localName
            ).gameObject.AddComponent<AudioListener>();
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
