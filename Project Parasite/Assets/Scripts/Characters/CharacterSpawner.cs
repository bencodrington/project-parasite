using System;
using Photon.Pun;
using UnityEngine;

public class CharacterSpawner
{
    #region [Public Variables]
    
    public ParasiteData parasiteData {get; private set;}
    
    #endregion

    #region [Private Variables]

	GameObject characterGameObject;
    bool isNetworked;
    
    #endregion

    #region [Public Methods]

    public CharacterSpawner(bool isNetworked) {
        this.isNetworked = isNetworked;
    }
    
	public void SpawnPlayerCharacter(CharacterType assignedCharacterType, Vector3 atPosition, Vector2 velocity, bool forceCameraSnap = true) {
		String characterPrefabName = assignedCharacterType == CharacterType.Parasite ? "Parasite" : "Hunter";
		GameObject characterPrefab = Resources.Load(characterPrefabName) as GameObject;
        if (isNetworked) {
            // Create PlayerCharacter game object on the server
            characterGameObject = PhotonNetwork.Instantiate(characterPrefab.name, atPosition, Quaternion.identity);
        } else {
            // Create it offline
            characterGameObject = GameObject.Instantiate(characterPrefab, atPosition, Quaternion.identity);
        }
    	// Get PlayerCharacter script
    	Character character = characterGameObject.GetComponentInChildren<Character>();
    	// Initialize each player's character on their own client
    	character.SetStartingVelocity(velocity);
    	character.CharacterSpawner = this;
		// Make the camera follow this character
    	character.SetCameraFollow(forceCameraSnap);
		// Make the character draw in front of other characters
    	character.SetRenderLayer();
		if (assignedCharacterType == CharacterType.Parasite) {
            parasiteData = new ParasiteData();
		}
	}

	public void DestroyCharacter() {
    	if (characterGameObject != null) {
            if (isNetworked) {
                PhotonNetwork.Destroy(characterGameObject);
            } else {
                GameObject.Destroy(characterGameObject);
            }
    	}
    }
    
    #endregion

}
