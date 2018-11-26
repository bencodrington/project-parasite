using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class PlayerObject : NetworkBehaviour {

	public GameObject ParasitePrefab;
	public GameObject ParasiteControlsPrefab;
	public GameObject HunterPrefab;
	public GameObject HunterControlsPrefab;
	public GameObject RoundManagerPrefab;
	public GameObject HealthPrefab;
	public GameObject NpcCountPrefab;
	public GameObject GameOverScreenPrefab;
	public GameObject GameOverScreenServerPrefab;

	private GameObject characterGameObject;
	private Text topRightUiText;
	private GameObject npcCountObject;
	private GameObject controlsObject;
	private GameObject gameOverScreen;

	private int _parasiteHealth;
	private int ParasiteHealth {
		get { return _parasiteHealth; }
		set {
			_parasiteHealth = value;
			UpdateHealthObject(value);
			if (value <= 0) {
				CmdShowGameOverScreen();
			}
		}
	}
	private int _remainingNpcCount;
	private int RemainingNpcCount {
		get { return _remainingNpcCount; }
		set {
			_remainingNpcCount = value;
			if (npcCountObject != null) {npcCountObject.GetComponentInChildren<Text>().text = value.ToString();};
		}
	}

	private const int STARTING_PARASITE_HEALTH = 100;
	private const int UI_PADDING_DISTANCE = 9;

	private Action OnCharacterDestroy;
	public void RegisterOnCharacterDestroyCallback(Action cb) {
		OnCharacterDestroy += cb;
	}
	public void UnRegisterOnCharacterDestroyCallback(Action cb) {
		OnCharacterDestroy -= cb;
	}

	public override void OnStartLocalPlayer() {
		base.OnStartLocalPlayer();
		ClientInformation cI = FindObjectOfType<ClientInformation>();
		if (cI == null) {
			Debug.LogError("PlayerObject:OnStartLocalPlayer: ClientInformation not found");
			return;
		}
		// Request the server's PlayerGrid to distribute any info it had before we connected
		CmdPullPlayerGrid();
		// Update local copy of PlayerGrid so that we can set local player without waiting
		// 	for the Rpc call to return to us
		// TODO: mimic setplayername for setlocalplayer, such that it creates an entry if a matching one isn't found
		PlayerGrid.Instance.AddPlayer(netId);
		// Update server copy and propogate to other clients
		CmdAddToPlayerGrid();
		// Update local copy with local player boolean
		PlayerGrid.Instance.SetLocalPlayer(netId);
		// Update all copies with the client name
		CmdSetPlayerName(cI.clientName);
	}

	void Start() {
		if (isLocalPlayer) {
			topRightUiText = GameObject.FindGameObjectWithTag("TopRightUI").GetComponent<Text>();
		}
		// Runs for all PlayerObjects on all clients
		PlayerGrid.Instance.SetPlayerObject(netId, this);
	}

	void Update () {
		// Runs on everyone's computer, regardless of whether they own this player object
		if (isLocalPlayer == false) { return; }
		if (Input.GetKeyDown(KeyCode.N)) {
			CmdStartGame();
		}
		if (Input.GetKeyDown(KeyCode.P)) {
			PlayerGrid.Instance.PrintGrid();
		}
	}

	void UpdateHealthObject(int newValue) {
		topRightUiText.text = newValue.ToString();
	}

	void ShowGameOverScreen() {
		gameOverScreen = isServer ? Instantiate(GameOverScreenServerPrefab) : Instantiate(GameOverScreenPrefab);
		gameOverScreen.transform.SetParent(FindObjectOfType<Canvas>().transform);
		RectTransform rect = gameOverScreen.GetComponent<RectTransform>();
		// Position gameOverScreen;
		rect.anchoredPosition = new Vector2(0.5f, 0.5f);
		rect.offsetMax = Vector2.zero;
		rect.offsetMin = Vector2.zero;
	}

	void DestroyGameOverScreen() {
		// gameOverScreen should be null when starting the game from the main menu
		// 	as opposed to when restarting after a round has been completed
		if (gameOverScreen == null) { return; }
		Destroy(gameOverScreen.gameObject);
	}

	// Commands

	[Command]
	public void CmdAssignCharacterTypeAndSpawnPoint(CharacterType characterType, Vector2 spawnPoint) {
		// Spawn Character across clients
		CmdSpawnPlayerCharacter(characterType, spawnPoint, Vector2.zero);
		// Update grid entry to include new character type
		PlayerGrid.Instance.CmdSetCharacterType(netId, characterType);
		// Update HUD to show necessary information for this character type
		RpcUpdateHud();
	}

	[Command]
	public void CmdSpawnPlayerCharacter(CharacterType characterType, Vector3 atPosition, Vector2 velocity) {
		GameObject characterPrefab = characterType == CharacterType.Parasite ? ParasitePrefab : HunterPrefab;
		// Create PlayerCharacter game object on the server
		characterGameObject = Instantiate(characterPrefab, atPosition, Quaternion.identity);
		// Propogate to all clients
		NetworkServer.SpawnWithClientAuthority(characterGameObject, connectionToClient);
		// Get PlayerCharacter script
		Character character = characterGameObject.GetComponentInChildren<Character>();
		// Update this player's entry in the player grid to reference the newly created character
		PlayerGrid.Instance.CmdSetCharacter(netId, character.netId);
		// Initialize each player's character on their own client
		character.RpcGeneratePhysicsEntity(velocity);
		character.PlayerObject = this;
		// Ensure character snaps to its starting position on all clients
		character.CmdUpdatePosition(atPosition, true);
		//  Set character as new target of camera
		character.RpcSetCameraFollow();
		character.RpcSetRenderLayer();
	}

	[Command]
	public void CmdShowGameOverScreen() {
		RpcShowGameOverScreen();
	}

	[Command]
	public void CmdDestroyGameOverScreen() {
		RpcDestroyGameOverScreen();
	}

	[Command]
	public void CmdStartGame() {
		RpcDestroyTitleScreen();
		RpcDestroyGameOverScreen();
		foreach (RoundManager rm in FindObjectsOfType<RoundManager>()) {
			rm.EndRound();
			Destroy(rm.gameObject);
		}
		// Create new RoundManager game object on the server
		Instantiate(RoundManagerPrefab);
	}

	[Command]
	public void CmdDestroyCharacter() {
		if (characterGameObject != null) {
			OnCharacterDestroy();
			NetworkServer.Destroy(characterGameObject);
		}
	}

	[Command]
	public void CmdEndRound() {
		CmdDestroyCharacter();
		RpcRemoveHud();
	}

	[Command]
	public void CmdCallElevatorToStop(NetworkInstanceId elevatorId, int stopIndex) {
		NetworkServer.FindLocalObject(elevatorId).GetComponentInChildren<Elevator>().CmdCallToStop(stopIndex);
	}

	[Command]
	void CmdPullPlayerGrid() {
		PlayerGrid.Instance.CmdPull();
	}

	[Command]
	void CmdAddToPlayerGrid() {
		PlayerGrid.Instance.CmdAddPlayer(netId);
	}

	[Command]
	void CmdSetPlayerName(string name) {
		PlayerGrid.Instance.CmdSetPlayerName(netId, name);
	}

	// Client RPCs

	[ClientRpc]
	public void RpcParasiteTakeDamage(int damage) {
		if (isLocalPlayer) {
			ParasiteHealth -= damage;
		}
	}

	[ClientRpc]
	public void RpcDestroyTitleScreen() {
		Destroy(GameObject.FindWithTag("TitleScreen"));
		// Hide Menu
        Menu menu = FindObjectOfType<Menu>();
        if (menu == null) {
            Debug.LogError("NetworkDiscoveryClient: onReceivedBroadcast: Menu not found");
            return;
        }
        menu.DeleteMenuItems();
	}

	[ClientRpc]
	void RpcUpdateHud() {
		if (!isLocalPlayer) { return; }
		Character character = PlayerGrid.Instance.GetLocalCharacter();
		CharacterType characterType = PlayerGrid.Instance.GetLocalCharacterType();
		switch (characterType) {
			case CharacterType.Hunter: 
				topRightUiText.enabled = true;
				Hunter hunter = ((Hunter) character);
				hunter.RegisterOnArmourChangeCallback(UpdateHealthObject);
				hunter.ArmourHealth = 150;
				controlsObject = Instantiate(HunterControlsPrefab, Vector3.zero, Quaternion.identity, FindObjectOfType<Canvas>().transform);
				break;
			case CharacterType.Parasite:
				topRightUiText.enabled = true;
				ParasiteHealth = STARTING_PARASITE_HEALTH;
				controlsObject = Instantiate(ParasiteControlsPrefab, Vector3.zero, Quaternion.identity, FindObjectOfType<Canvas>().transform);
				break;
			default:
				// TODO: deactivate all UI
				topRightUiText.enabled = false;
				break;
		}
		controlsObject.GetComponentInChildren<RectTransform>().anchoredPosition = new Vector2(UI_PADDING_DISTANCE, UI_PADDING_DISTANCE);
		// Display NPC count 
		npcCountObject = Instantiate(NpcCountPrefab, Vector3.zero, Quaternion.identity, FindObjectOfType<Canvas>().transform);
		npcCountObject.GetComponentInChildren<RectTransform>().anchoredPosition = new Vector2(UI_PADDING_DISTANCE, -UI_PADDING_DISTANCE);
	}

	[ClientRpc]
	void RpcRemoveHud() {
		if (!isLocalPlayer) { return; }
		if (topRightUiText != null) {
			topRightUiText.enabled = false;
		}
		if (npcCountObject != null) {
			Destroy(npcCountObject);
		}
		if (controlsObject != null) {
			Destroy(controlsObject);
		}
	}

	[ClientRpc]
	public void RpcUpdateRemainingNpcCount(int updatedCount) {
		if (!isLocalPlayer) { return; }
		RemainingNpcCount = updatedCount;
	}

	[ClientRpc]
	void RpcShowGameOverScreen() {
		PlayerGrid.Instance.GetLocalPlayerObject().ShowGameOverScreen();
	}

	[ClientRpc]
	void RpcDestroyGameOverScreen() {
		PlayerGrid.Instance.GetLocalPlayerObject().DestroyGameOverScreen();
	}
}
