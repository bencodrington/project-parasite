using System;
using Photon.Pun;
using UnityEngine;

public class CharacterSpawner
{
    #region [Public Variables]
    
    public ParasiteData parasiteData {get; private set;}
    
    #endregion

    #region [Private Variables]

	Character character;

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
					InputSource inputSource = null,
					bool preserveParasiteHealth = false,
					string name = null) {
		if (inputSource == null) {
			inputSource = new PlayerInput();
		}
		String characterPrefabName = assignedCharacterType == CharacterType.Parasite ? "Parasite" : "Hunter";
		GameObject characterPrefab = Resources.Load(characterPrefabName) as GameObject;
        // Create PlayerCharacter game object on the server
        GameObject characterGameObject = PhotonNetwork.Instantiate(characterPrefab.name, atPosition, Quaternion.identity);
    	// Get PlayerCharacter script
    	character = characterGameObject.GetComponentInChildren<Character>();
    	// Initialize each player's character on their own client
    	character.SetStartingVelocity(velocity);
    	character.CharacterSpawner = this;
		if (shouldCameraFollow) {
			// Make the camera follow this character
			character.SetCameraFollow(forceCameraSnap);
		}
		// Make the character draw in front of other characters
    	character.SetRenderLayer();
		// This spawner has already spawned a parasite, but now wants to
		//	replace it, so initialize its data object
		bool overwriteParasiteData = assignedCharacterType == CharacterType.Parasite && !preserveParasiteHealth;
		// This spawner is spawning a parasite for the first time, so
		//	initialize its data object
		bool isNewParasite = assignedCharacterType == CharacterType.Parasite && parasiteData == null;
		if (overwriteParasiteData || isNewParasite) {
            parasiteData = new ParasiteData(this, deathHandler);
		}
		character.SetInputSource(inputSource);
		character.SetName(name);
		return character;
	}

	public void DestroyCharacter() {
    	if (character != null) {
			character.Destroy();
    	}
		if (deathHandler != null) {
			TutorialManager.parasitesStillAlive--;
		}
		UiManager.Instance.minimap.RemoveTarget();
    }

	public void SetCharacter(Character newCharacter) {
		this.character = newCharacter;
		newCharacter.CharacterSpawner = this;
	}

	public Character GetCharacter() {
		if (character != null) {
			return character;
		} else {
			Debug.LogError("CharacterSpawner:GetCharacter:Trying to get a null character");
			return null;
		}
	}

	public void OnMutation() {
		if (parasiteData != null) {
			parasiteData.SetVamparasite();
		}
	}
    
    #endregion

}
