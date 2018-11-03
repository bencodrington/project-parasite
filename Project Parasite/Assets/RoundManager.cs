using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoundManager : MonoBehaviour {

	PlayerObject[] connectedPlayers;

	void Start () {
		// Cache Player Objects
		// TODO: uncache on leave
		connectedPlayers = FindObjectsOfType<PlayerObject>();
		SelectParasite();
	}

	void SelectParasite() {
		int n = connectedPlayers.Length;
		int indexOfParasite = Random.Range(0, n);
		for (int i = 0; i < n; i++) {
			// TODO: Replace strings with constants
			if (i == indexOfParasite) {
				connectedPlayers[i].CmdSpawnPlayerCharacter("PARASITE", new Vector3());
			} else { // Player is a hunter
				connectedPlayers[i].CmdSpawnPlayerCharacter("HUNTER", new Vector3());
			}
		}
	}

	public void EndRound() {
		foreach (PlayerObject player in connectedPlayers) {
			player.DestroyCharacter();
		}
		transform.GetComponentInChildren<NpcManager>().DespawnNPCs();
	}

}
