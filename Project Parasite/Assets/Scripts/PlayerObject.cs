﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class PlayerObject : NetworkBehaviour {

	public GameObject ParasitePrefab;
	public GameObject HunterPrefab;
	public GameObject RoundManagerPrefab;
	public GameObject HealthPrefab;

	private GameObject playerCharacterGameObject;
	private GameObject healthObject;

	private string characterType;
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

	void Start () {
		if (isLocalPlayer == false) {
			// Object belongs to another player
			return;
		}
	}

	void Update () {
		// Runs on everyone's computer, regardless of whether they own this player object
		if (isLocalPlayer == false) {
			return;
		}

		if (Input.GetKeyDown(KeyCode.E)) {
			CmdStartGame();
		}
	}

	// Commands

	[Command]
	public void CmdSpawnPlayerCharacter(string characterType, Vector3 atPosition) {
		GameObject playerCharacterPrefab = characterType == "PARASITE" ? ParasitePrefab : HunterPrefab;
		// Create PlayerCharacter game object on the server
		playerCharacterGameObject = Instantiate(playerCharacterPrefab, atPosition, Quaternion.identity);
		// Propogate to all clients
		NetworkServer.SpawnWithClientAuthority(playerCharacterGameObject, connectionToClient);
		// Get PlayerCharacter script
		PlayerCharacter playerCharacter = playerCharacterGameObject.GetComponentInChildren<PlayerCharacter>();
		// Initialize each player's character on their own client
		playerCharacter.RpcGeneratePhysicsEntity();
		playerCharacter.playerObject = this;
		//  Set playerCharacter as new target of camera
		playerCharacter.RpcSetCameraFollow();
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
		if (hasAuthority && playerCharacterGameObject != null) {
			NetworkServer.Destroy(playerCharacterGameObject);
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
	public void RpcSetCharacterType(string newCharacterType) {
		characterType = newCharacterType;
		if (!isLocalPlayer) { return; }
		FindObjectOfType<ClientInformation>().clientType = newCharacterType;
		if (characterType == "PARASITE") {
			// Generate HUD
			healthObject = Instantiate(HealthPrefab, Vector3.zero, Quaternion.identity, FindObjectOfType<Canvas>().transform);
			// TODO: replace Vector2.zero with a padding constant
			healthObject.GetComponentInChildren<RectTransform>().anchoredPosition = Vector2.zero;
			Health = 100;
		}
	}

	[ClientRpc]
	void RpcRemoveHud() {
		if (isLocalPlayer && healthObject != null) {
			Destroy(healthObject);
		}
	}
}
