﻿using System;
using System.Collections;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class PlayerObject : MonoBehaviour, IOnEventCallback {

	public GameObject ParasitePrefab;
	public GameObject ParasiteControlsPrefab;
	public GameObject HunterPrefab;
	public GameObject HunterControlsPrefab;
	public GameObject HealthPrefab;
	public GameObject NpcCountPrefab;
	public GameObject GameOverScreenPrefab;
	public GameObject GameOverScreenServerPrefab;

	private GameObject characterGameObject;
	private Text topRightUiText;
	private GameObject npcCountObject;
	private GameObject controlsObject;
	private GameObject gameOverScreen;
	private RoundManager roundManager;

	// The text shown on the game over screen
	private const string HUNTERS_WIN = "HUNTERS WIN!";
	private const string PARASITE_WINS = "PARASITE WINS!";
	// The colour of the text shown on the game over screen
	private Color WIN_COLOUR = Color.green;
	private Color LOSS_COLOUR = Color.red;

	private int _parasiteHealth;
	private int ParasiteHealth {
		get { return _parasiteHealth; }
		set {
			if (value < _parasiteHealth) {
				// Notify parasite that it is taking damage
				((Parasite)PlayerGrid.Instance.GetLocalCharacter()).OnTakingDamage();
			}
			_parasiteHealth = value;
			UpdateHealthObject(value);
			if (value <= 0) {
				// TODO:
				// CmdShowGameOverScreen(CharacterType.Hunter);
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

	void Start() {
		// TODO: extract to UI manager
		topRightUiText = GameObject.FindGameObjectWithTag("TopRightUI").GetComponent<Text>();
	}

	void UpdateHealthObject(int newValue) {
		topRightUiText.text = newValue.ToString();
	}

	// TODO: extrat to UI manager
	// void ShowGameOverScreen(CharacterType victorType) {
	// 	gameOverScreen = isServer ? Instantiate(GameOverScreenServerPrefab) : Instantiate(GameOverScreenPrefab);
	// 	gameOverScreen.transform.SetParent(FindObjectOfType<Canvas>().transform);
	// 	RectTransform rect = gameOverScreen.GetComponent<RectTransform>();
	// 	// Position gameOverScreen;
	// 	rect.anchoredPosition = new Vector2(0.5f, 0.5f);
	// 	rect.offsetMax = Vector2.zero;
	// 	rect.offsetMin = Vector2.zero;

	// 	Transform VictorText = gameOverScreen.transform.Find("Victor");
	// 	if (VictorText == null) {
	// 		Debug.LogError("PlayerObject:ShowGameOverScreen: Victor Text not found");
	// 		return;
	// 	}
	// 	Text txt = VictorText.GetComponent<Text>();
	// 	txt.text = victorType == CharacterType.Hunter ? HUNTERS_WIN : PARASITE_WINS;
	// 	txt.color = victorType == PlayerGrid.Instance.GetLocalCharacterType() ? WIN_COLOUR : LOSS_COLOUR;
	// }

	void DestroyGameOverScreen() {
		// gameOverScreen should be null when starting the game from the main menu
		// 	as opposed to when restarting after a round has been completed
		if (gameOverScreen == null) { return; }
		Destroy(gameOverScreen.gameObject);
	}

	public void ParasiteTakeDamage(int damage) {
		ParasiteHealth -= damage;
	}

    public void OnEvent(EventData photonEvent)
    {
        if (photonEvent.Code == EventCodes.AssignPlayerType) {
			// Deconstruct event
			object[] content = (object[])photonEvent.CustomData;
			int actorNumber = (int)content[0];
			CharacterType characterType = (CharacterType)content[1];
			Vector3 spawnPoint = Vector3.zero;
			Debug.Log("aN: " + actorNumber + ", local aN: " + PhotonNetwork.LocalPlayer.ActorNumber);
			if (actorNumber == PhotonNetwork.LocalPlayer.ActorNumber) {
				// Spawn Character of type `characterType` across clients
				SpawnPlayerCharacter(characterType, spawnPoint, Vector2.zero);
				// TODO: update hud
			}
		}
    }

	#region [MonoBehaviour Callbacks]
	
	public void OnEnable() {
		PhotonNetwork.AddCallbackTarget(this);
	}

	public void OnDisable() {
		PhotonNetwork.RemoveCallbackTarget(this);
	}
	
	#endregion

	#region [Private Methods]
	
	void SpawnPlayerCharacter(CharacterType characterType, Vector3 atPosition, Vector2 velocity) {
		Debug.Log("Spawning character of type " + characterType);
		GameObject characterPrefab = characterType == CharacterType.Parasite ? ParasitePrefab : HunterPrefab;
    	// Create PlayerCharacter game object on the server
    	characterGameObject = PhotonNetwork.Instantiate(characterPrefab.name, atPosition, Quaternion.identity);
    	// Get PlayerCharacter script
    	Character character = characterGameObject.GetComponentInChildren<Character>();
    	// Initialize each player's character on their own client
    	character.GeneratePhysicsEntity(velocity);
    	character.PlayerObject = this;
		// TODO:
    	// // Ensure character snaps to its starting position on all clients
    	// character.CmdUpdatePosition(atPosition, true);
	}
	
	#endregion

    // // Commands

    // [Command]
    // public void CmdAssignCharacterTypeAndSpawnPoint(CharacterType characterType, Vector2 spawnPoint) {
    // 	// Spawn Character across clients
    // 	CmdSpawnPlayerCharacter(characterType, spawnPoint, Vector2.zero);
    // 	// Update grid entry to include new character type
    // 	PlayerGrid.Instance.CmdSetCharacterType(netId, characterType);
    // 	// Update HUD to show necessary information for this character type
    // 	RpcUpdateHud();
    // }

    // [Command]
    // public void CmdShowGameOverScreen(CharacterType victorType) {
    // 	if (roundManager == null) {
    // 		return;
    // 	}
    // 	if (roundManager.isGameOver) { return; }
    // 	roundManager.isGameOver = true;
    // 	RpcShowGameOverScreen(victorType);
    // }

    // [Command]
    // public void CmdDestroyGameOverScreen() {
    // 	RpcDestroyGameOverScreen();
    // }

    // [Command]
    // public void CmdDestroyCharacter() {
    // 	if (characterGameObject != null) {
    // 		OnCharacterDestroy();
    // 		NetworkServer.Destroy(characterGameObject);
    // 	}
    // }

    // [Command]
    // public void CmdEndRound() {
    // 	CmdDestroyCharacter();
    // 	RpcRemoveHud();
    // }

    // [Command]
    // public void CmdCallElevatorToStop(NetworkInstanceId elevatorId, int stopIndex) {
    // 	NetworkServer.FindLocalObject(elevatorId).GetComponentInChildren<Elevator>().CmdCallToStop(stopIndex);
    // }

    // // ClientRpc

    // [ClientRpc]
    // public void RpcDestroyTitleScreen() {
    // 	Destroy(GameObject.FindWithTag("TitleScreen"));
    // 	// Hide Menu
    //     Menu menu = FindObjectOfType<Menu>();
    //     if (menu == null) {
    //         Debug.LogError("NetworkDiscoveryClient: onReceivedBroadcast: Menu not found");
    //         return;
    //     }
    //     menu.DeleteMenuItems();
    // }

    // [ClientRpc]
    // void RpcUpdateHud() {
    // 	if (!isLocalPlayer) { return; }
    // 	CharacterType characterType = PlayerGrid.Instance.GetLocalCharacterType();
    // 	switch (characterType) {
    // 		case CharacterType.Hunter:
    // 			topRightUiText.enabled = false;
    // 			controlsObject = Instantiate(HunterControlsPrefab, Vector3.zero, Quaternion.identity, FindObjectOfType<Canvas>().transform);
    // 			break;
    // 		case CharacterType.Parasite:
    // 			topRightUiText.enabled = true;
    // 			ParasiteHealth = STARTING_PARASITE_HEALTH;
    // 			controlsObject = Instantiate(ParasiteControlsPrefab, Vector3.zero, Quaternion.identity, FindObjectOfType<Canvas>().transform);
    // 			break;
    // 		default:
    // 			// TODO: deactivate all UI
    // 			topRightUiText.enabled = false;
    // 			break;
    // 	}
    // 	controlsObject.GetComponentInChildren<RectTransform>().anchoredPosition = new Vector2(UI_PADDING_DISTANCE, UI_PADDING_DISTANCE);
    // 	// Display NPC count 
    // 	npcCountObject = Instantiate(NpcCountPrefab, Vector3.zero, Quaternion.identity, FindObjectOfType<Canvas>().transform);
    // 	npcCountObject.GetComponentInChildren<RectTransform>().anchoredPosition = new Vector2(UI_PADDING_DISTANCE, -UI_PADDING_DISTANCE);
    // }

    // [ClientRpc]
    // void RpcRemoveHud() {
    // 	if (!isLocalPlayer) { return; }
    // 	if (topRightUiText != null) {
    // 		topRightUiText.enabled = false;
    // 	}
    // 	if (npcCountObject != null) {
    // 		Destroy(npcCountObject);
    // 	}
    // 	if (controlsObject != null) {
    // 		Destroy(controlsObject);
    // 	}
    // }

    // [ClientRpc]
    // public void RpcUpdateRemainingNpcCount(int updatedCount) {
    // 	if (!isLocalPlayer) { return; }
    // 	RemainingNpcCount = updatedCount;
    // }

    // [ClientRpc]
    // void RpcShowGameOverScreen(CharacterType victorType) {
    // 	PlayerGrid.Instance.GetLocalPlayerObject().ShowGameOverScreen(victorType);
    // }

    // [ClientRpc]
    // void RpcDestroyGameOverScreen() {
    // 	PlayerGrid.Instance.GetLocalPlayerObject().DestroyGameOverScreen();
    // }
}
