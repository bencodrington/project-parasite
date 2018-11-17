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

	private GameObject characterGameObject;
	private Text topRightUiText;
	private GameObject npcCountObject;
	private GameObject controlsObject;

	private int _health;
	// private int Health {
	// 	get { return _health; }
	// 	set {
	// 		_health = value;
	// 		healthObject.GetComponentInChildren<Text>().text = value.ToString();
	// 		if (value <= 0) {
	// 			CmdStartGame();
	// 		}
	// 	}
	// }
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

	void Start() {
		PlayerGrid.Instance.AddPlayer(netId);
		if (isLocalPlayer) {
			FindObjectOfType<ClientInformation>().localPlayer = this;
			PlayerGrid.Instance.SetLocalPlayer(netId);
			topRightUiText = GameObject.FindGameObjectWithTag("TopRightUI").GetComponent<Text>();
		}
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

	// Commands

	[Command]
	public void CmdAssignCharacterType(CharacterType characterType) {
		// Spawn Character across clients
		CmdSpawnPlayerCharacter(characterType, Vector3.zero, Vector2.zero);
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
		character.playerObject = this;
		// Ensure character snaps to its starting position on all clients
		character.CmdUpdatePosition(atPosition, true);
		//  Set character as new target of camera
		character.RpcSetCameraFollow();
		character.RpcSetRenderLayer();
	}

	[Command]
	public void CmdStartGame() {
		RpcDestroyTitleScreen();
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

	// Client RPCs

	[ClientRpc]
	public void RpcTakeDamage(int damage) {
		if (isLocalPlayer) {
			// TODO:
			// Health -= damage;
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

	// 	// TODO: refactor (from RpcSetCharacterType)
	// 	if (newCharacterType == CharacterType.Parasite) {
	// 		// Display health
	// 		Health = STARTING_PARASITE_HEALTH;
	// 		controlsObject = Instantiate(ParasiteControlsPrefab, Vector3.zero, Quaternion.identity, FindObjectOfType<Canvas>().transform);
	// 	} else {
	// 		// Display controls
	// 		controlsObject = Instantiate(HunterControlsPrefab, Vector3.zero, Quaternion.identity, FindObjectOfType<Canvas>().transform);
	// 	}
	// 	controlsObject.GetComponentInChildren<RectTransform>().anchoredPosition = new Vector2(UI_PADDING_DISTANCE, UI_PADDING_DISTANCE);
	// 	// Display NPC count
	// 	npcCountObject = Instantiate(NpcCountPrefab, Vector3.zero, Quaternion.identity, FindObjectOfType<Canvas>().transform);
	// 	npcCountObject.GetComponentInChildren<RectTransform>().anchoredPosition = new Vector2(UI_PADDING_DISTANCE, -UI_PADDING_DISTANCE);
	// }

	[ClientRpc]
	void RpcUpdateHud() {
		if (!isLocalPlayer) { return; }
		Character character = PlayerGrid.Instance.GetLocalCharacter();
		CharacterType characterType = PlayerGrid.Instance.GetLocalCharacterType();
		switch (characterType) {
			case CharacterType.Hunter: 
				Hunter hunter = ((Hunter) character);
				hunter.RegisterOnArmourChangeCallback(UpdateHealthObject);
				hunter.ArmourHealth = 150;
				topRightUiText.enabled = true;
				break;
			default:
				// TODO: deactivate all UI
				topRightUiText.enabled = false;
				break;
		}
	}

	[ClientRpc]
	void RpcRemoveHud() {
		if (!isLocalPlayer) { return; }
		if (topRightUiText != null) {
			topRightUiText.gameObject.SetActive(false);
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
}
