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

	#region [Private Variables]

	CharacterSpawner characterSpawner;

	#endregion

    public void OnEvent(EventData photonEvent)
    {
		switch (photonEvent.Code) {
			case EventCodes.StartGame:
				if (characterSpawner != null) {
					characterSpawner.DestroyCharacter();
				}
				break;
			case EventCodes.AssignPlayerTypeAndSpawnPoint:
				// Deconstruct event
				int actorNumber = (int)EventCodes.GetEventContentAtPosition(photonEvent, 0);
				if (actorNumber == PhotonNetwork.LocalPlayer.ActorNumber) {
					CharacterType assignedCharacterType = (CharacterType)EventCodes.GetEventContentAtPosition(photonEvent, 1);
					Vector3 spawnPoint = (Vector2)EventCodes.GetEventContentAtPosition(photonEvent, 2);
					if (characterSpawner != null) {
						// TODO: destroy characterspawner
						Debug.LogError("TODO: destroy existing characterSpawner");
					}
					characterSpawner = new CharacterSpawner();
					// Spawn Character of type `assignedCharacterType` across clients
					characterSpawner.SpawnPlayerCharacter(assignedCharacterType, spawnPoint, Vector2.zero);
				}
				break;
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

}
