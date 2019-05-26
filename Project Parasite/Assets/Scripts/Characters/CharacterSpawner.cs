﻿using System;
using Photon.Pun;
using UnityEngine;

public class CharacterSpawner
{
    #region [Public Variables]
    
    public ParasiteData parasiteData {get; private set;}
    
    #endregion

    #region [Private Variables]

	GameObject characterGameObject;

	// If not null, this is a function that should be called when any Parasites
	// 	spawned by this CharacterSpawner run out of health
	ParasiteData.DeathHandler deathHandler;
    
    #endregion

    #region [Public Methods]

	public CharacterSpawner(ParasiteData.DeathHandler deathHandler = null) {
		if (deathHandler != null) {
			TutorialManager.parasitesStillAlive++;
		}
		this.deathHandler = deathHandler;
	}
    
	public Character SpawnPlayerCharacter(
					CharacterType assignedCharacterType,
					Vector3 atPosition,
					Vector2 velocity,
					bool forceCameraSnap = true,
					bool shouldCameraFollow = true,
					InputSource inputSource = null) {
		if (inputSource == null) {
			inputSource = new PlayerInput();
		}
		String characterPrefabName = assignedCharacterType == CharacterType.Parasite ? "Parasite" : "Hunter";
		GameObject characterPrefab = Resources.Load(characterPrefabName) as GameObject;
        // Create PlayerCharacter game object on the server
        characterGameObject = PhotonNetwork.Instantiate(characterPrefab.name, atPosition, Quaternion.identity);
    	// Get PlayerCharacter script
    	Character character = characterGameObject.GetComponentInChildren<Character>();
    	// Initialize each player's character on their own client
    	character.SetStartingVelocity(velocity);
    	character.CharacterSpawner = this;
		if (shouldCameraFollow) {
			// Make the camera follow this character
			character.SetCameraFollow(forceCameraSnap);
		}
		// Make the character draw in front of other characters
    	character.SetRenderLayer();
		if (assignedCharacterType == CharacterType.Parasite) {
            parasiteData = new ParasiteData(this, deathHandler);
		}
		character.SetInputSource(inputSource);
		return character;
	}

	public void DestroyCharacter() {
    	if (characterGameObject != null) {
            PhotonNetwork.Destroy(characterGameObject);
    	}
		if (deathHandler != null) {
			TutorialManager.parasitesStillAlive--;
		}
    }
    
    #endregion

}
