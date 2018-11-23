using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoundManager : MonoBehaviour {

	PlayerObject[] connectedPlayers;

	public Vector2[] spawnPoints;

	Vector2 parasiteSpawnPoint;
	Vector2 hunterSpawnPoint;

	bool huntersOnlyMode = false;

	void Start () {
		// Cache Player Objects
		connectedPlayers = FindObjectsOfType<PlayerObject>();
		// TODO: uncache on leave
		SelectSpawnPoints();
		SelectParasite();
	}

	void SelectParasite() {
		int n = connectedPlayers.Length;
		Vector2 spawnPoint;
		// Randomly select one of the players to be parasite, the rest are hunters
		int indexOfParasite = Random.Range(0, n);
		if (huntersOnlyMode) { indexOfParasite = -1; }
		CharacterType characterType;
		for (int i = 0; i < n; i++) {
			if (i == indexOfParasite) {
				characterType = CharacterType.Parasite;
				spawnPoint = parasiteSpawnPoint;
			} else { // Player is a hunter
				characterType = CharacterType.Hunter;
				spawnPoint = hunterSpawnPoint;
			}
			connectedPlayers[i].CmdAssignCharacterTypeAndSpawnPoint(characterType, spawnPoint);
		}
	}
	
	void SelectSpawnPoints() {
		int n = spawnPoints.Length;
		int parasiteSpawnPointIndex, hunterSpawnPointIndex;
		// Randomly select one of the points for the parasite
		parasiteSpawnPointIndex = Random.Range(0, n);
		// And one for the hunters
		hunterSpawnPointIndex = Random.Range(0, n - 1);
		if (n > 1 && (parasiteSpawnPointIndex == hunterSpawnPointIndex)) {
			// Ensure the hunterSpawnPoint is different from the parasite's
			hunterSpawnPointIndex = n - 1;
		}
		parasiteSpawnPoint = spawnPoints[parasiteSpawnPointIndex];
		hunterSpawnPoint = spawnPoints[hunterSpawnPointIndex];
	}

	public void EndRound() {
		foreach (PlayerObject player in connectedPlayers) {
			player.CmdEndRound();
		}
		transform.GetComponentInChildren<NpcManager>().DespawnNPCs();
	}

}
