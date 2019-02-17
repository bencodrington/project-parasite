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
	
	GameObject HunterPrefab;
	GameObject ParasitePrefab;

	GameObject characterGameObject;
	RoundManager roundManager;

	#endregion

	int _parasiteHealth;
	int ParasiteHealth {
		get { return _parasiteHealth; }
		set {
			if (value < _parasiteHealth) {
				// Notify parasite that it is taking damage
				characterGameObject.GetComponent<Parasite>().OnTakingDamage();
			}
			_parasiteHealth = value;
			UiManager.Instance.UpdateHealthObject(value);
			if (value <= 0) {
				// OPTIMIZE: No need to keep sending this event
				Debug.Log("Hunters Win!");
                EventCodes.RaiseGameOverEvent(CharacterType.Hunter);
			}
		}
	}


	private Action OnCharacterDestroy;
	public void RegisterOnCharacterDestroyCallback(Action cb) {
		OnCharacterDestroy += cb;
	}
	public void UnRegisterOnCharacterDestroyCallback(Action cb) {
		OnCharacterDestroy -= cb;
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
			case EventCodes.AssignPlayerType:
				// Deconstruct event
				int actorNumber = (int)EventCodes.GetEventContentAtPosition(photonEvent, 0);
				CharacterType assignedCharacterType = (CharacterType)EventCodes.GetEventContentAtPosition(photonEvent, 1);
				Vector3 spawnPoint = Vector3.zero;
				if (actorNumber == PhotonNetwork.LocalPlayer.ActorNumber) {
					// Spawn Character of type `assignedCharacterType` across clients
					SpawnPlayerCharacter(assignedCharacterType, spawnPoint, Vector2.zero);
				}
				break;
		}
    }

	#region [Public Methods]
	
	public void SpawnPlayerCharacter(CharacterType assignedCharacterType, Vector3 atPosition, Vector2 velocity) {
		GameObject characterPrefab = assignedCharacterType == CharacterType.Parasite ? ParasitePrefab : HunterPrefab;
    	// Create PlayerCharacter game object on the server
    	characterGameObject = PhotonNetwork.Instantiate(characterPrefab.name, atPosition, Quaternion.identity);
    	// Get PlayerCharacter script
    	Character character = characterGameObject.GetComponentInChildren<Character>();
    	// Initialize each player's character on their own client
    	character.GeneratePhysicsEntity(velocity);
    	character.PlayerObject = this;
		UiManager.Instance.characterType = assignedCharacterType;
	}
	
	#endregion

	#region [MonoBehaviour Callbacks]

	void Start() {
		ParasitePrefab = Resources.Load("Parasite") as GameObject;
		HunterPrefab = Resources.Load("Hunter") as GameObject;
	}
	
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
    		OnCharacterDestroy();
    		PhotonNetwork.Destroy(characterGameObject);
    	}
    }
	
	#endregion

    // public void CmdAssignCharacterTypeAndSpawnPoint(CharacterType characterType, Vector2 spawnPoint) {
    // 	// Spawn Character across clients
    // 	CmdSpawnPlayerCharacter(characterType, spawnPoint, Vector2.zero);
    // 	// Update grid entry to include new character type
    // 	PlayerGrid.Instance.CmdSetCharacterType(netId, characterType);
    // 	// Update HUD to show necessary information for this character type
    // 	RpcUpdateHud();
    // }
}
