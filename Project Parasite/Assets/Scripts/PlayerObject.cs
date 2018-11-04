using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class PlayerObject : NetworkBehaviour {

	public GameObject ParasitePrefab;
	public GameObject HunterPrefab;
	public GameObject RoundManagerPrefab;
	public GameObject HealthPrefab;
	public GameObject NpcCountPrefab;

	private GameObject characterGameObject;
	private GameObject healthObject;
	private GameObject npcCountObject;

	private int _health;
	private int Health {
		get { return _health; }
		set {
			_health = value;
			healthObject.GetComponentInChildren<Text>().text = value.ToString();
			if (value <= 0) {
				CmdStartGame();
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

	void Update () {
		// Runs on everyone's computer, regardless of whether they own this player object
		if (isLocalPlayer == false) { return; }
		if (Input.GetKeyDown(KeyCode.E)) {
			CmdStartGame();
		}
	}

	// Commands

	[Command]
	public void CmdSpawnPlayerCharacter(CharacterType characterType, Vector3 atPosition, Vector2 velocity) {
		GameObject characterPrefab = characterType == CharacterType.Parasite ? ParasitePrefab : HunterPrefab;
		// Create PlayerCharacter game object on the server
		characterGameObject = Instantiate(characterPrefab, atPosition, Quaternion.identity);
		// Propogate to all clients
		NetworkServer.SpawnWithClientAuthority(characterGameObject, connectionToClient);
		// Get PlayerCharacter script
		Character character = characterGameObject.GetComponentInChildren<Character>();
		// Initialize each player's character on their own client
		character.RpcGeneratePhysicsEntity(velocity);
		character.playerObject = this;
		//  Set character as new target of camera
		character.RpcSetCameraFollow();
		character.RpcSetRenderLayer();
	}

	[Command]
	public void CmdStartGame() {
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

	[ClientRpc]
	public void RpcTakeDamage(int damage) {
		if (isLocalPlayer) {
			Health -= damage;
		}
	}

	// Client RPCs

	[ClientRpc]
	public void RpcSetCharacterType(CharacterType newCharacterType) {
		if (!isLocalPlayer) { return; }
		FindObjectOfType<ClientInformation>().clientType = newCharacterType;
		// Generate HUD
		if (newCharacterType == CharacterType.Parasite) {
			// Display health
			healthObject = Instantiate(HealthPrefab, Vector3.zero, Quaternion.identity, FindObjectOfType<Canvas>().transform);
			healthObject.GetComponentInChildren<RectTransform>().anchoredPosition = new Vector2(-UI_PADDING_DISTANCE, -UI_PADDING_DISTANCE);
			Health = STARTING_PARASITE_HEALTH;
		}
		// Display NPC count
		npcCountObject = Instantiate(NpcCountPrefab, Vector3.zero, Quaternion.identity, FindObjectOfType<Canvas>().transform);
		npcCountObject.GetComponentInChildren<RectTransform>().anchoredPosition = new Vector2(UI_PADDING_DISTANCE, -UI_PADDING_DISTANCE);
	}

	[ClientRpc]
	void RpcRemoveHud() {
		if (!isLocalPlayer) { return; }
		if (healthObject != null) {
			Destroy(healthObject);
		}
		if (npcCountObject != null) {
			Destroy(npcCountObject);
		}
	}

	[ClientRpc]
	public void RpcUpdateRemainingNpcCount(int updatedCount) {
		if (!isLocalPlayer) { return; }
		RemainingNpcCount = updatedCount;
	}
}
