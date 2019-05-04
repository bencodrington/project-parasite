using System;
using System.Collections;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class PlayerObject : MonoBehaviour, IOnEventCallback {

	#region [Public Variables]
	
	public const int STARTING_PARASITE_HEALTH = 100;
	
	#endregion

	#region [Private Variables]

	GameObject characterGameObject;
	RoundManager roundManager;

	bool hasSentGameOver = false;

	#endregion

	int _parasiteHealth;
	int ParasiteHealth {
		get { return _parasiteHealth; }
		set {
			_parasiteHealth = Mathf.Clamp(value, 0, STARTING_PARASITE_HEALTH);
			UiManager.Instance.UpdateHealthObject(_parasiteHealth);
			if (value <= 0 && !hasSentGameOver) {
				Debug.Log("Hunters Win!");
                EventCodes.RaiseGameOverEvent(CharacterType.Hunter);
				hasSentGameOver = false;
			}
		}
	}

	public void ParasiteTakeDamage(int damage) {
		ParasiteHealth -= damage;
	}

    public void OnEvent(EventData photonEvent)
    {
		switch (photonEvent.Code) {
			case EventCodes.StartGame:
				DestroyCharacter();
				break;
			case EventCodes.AssignPlayerTypeAndSpawnPoint:
				// Deconstruct event
				int actorNumber = (int)EventCodes.GetEventContentAtPosition(photonEvent, 0);
				CharacterType assignedCharacterType = (CharacterType)EventCodes.GetEventContentAtPosition(photonEvent, 1);
				Vector3 spawnPoint = (Vector2)EventCodes.GetEventContentAtPosition(photonEvent, 2);
				if (actorNumber == PhotonNetwork.LocalPlayer.ActorNumber) {
					// Spawn Character of type `assignedCharacterType` across clients
					SpawnPlayerCharacter(assignedCharacterType, spawnPoint, Vector2.zero);
				}
				break;
		}
    }

	#region [Public Methods]
	
	public void SpawnPlayerCharacter(CharacterType assignedCharacterType, Vector3 atPosition, Vector2 velocity, bool forceCameraSnap = true) {
		String characterPrefabName = assignedCharacterType == CharacterType.Parasite ? "Parasite" : "Hunter";
		GameObject characterPrefab = Resources.Load(characterPrefabName) as GameObject;
    	// Create PlayerCharacter game object on the server
    	characterGameObject = PhotonNetwork.Instantiate(characterPrefab.name, atPosition, Quaternion.identity);
    	// Get PlayerCharacter script
    	Character character = characterGameObject.GetComponentInChildren<Character>();
    	// Initialize each player's character on their own client
    	character.SetStartingVelocity(velocity);
    	character.PlayerObject = this;
		// Make the camera follow this character
    	character.SetCameraFollow(forceCameraSnap);
		// Make the character draw in front of other characters
    	character.SetRenderLayer();
		hasSentGameOver = false;
		if (assignedCharacterType == CharacterType.Parasite) {
			ParasiteHealth = STARTING_PARASITE_HEALTH;
		}
	}
	
	#endregion

	#region [MonoBehaviour Callbacks]
	
	public void OnEnable() {
		PhotonNetwork.AddCallbackTarget(this);
	}

	public void OnDisable() {
		PhotonNetwork.RemoveCallbackTarget(this);
	}
	
	#endregion

	#region [Private Methods]

	void DestroyCharacter() {
    	if (characterGameObject != null) {
    		PhotonNetwork.Destroy(characterGameObject);
    	}
    }
	
	#endregion

}
