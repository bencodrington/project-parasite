using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoundManager : MonoBehaviour {

	PlayerObject[] connectedPlayers;

	void Start () {
		// Cache Player Objects
		connectedPlayers = FindObjectsOfType<PlayerObject>();
		// TODO: uncache on leave
		SelectParasite();
	}

	void SelectParasite() {
		int n = connectedPlayers.Length;
		// Randomly select one of the players to be parasite, the rest are hunters
		int indexOfParasite = Random.Range(0, n);
		string characterType;
		for (int i = 0; i < n; i++) {
			if (i == indexOfParasite) {
				characterType = "PARASITE";
			} else { // Player is a hunter
				characterType = "HUNTER";
			}
			connectedPlayers[i].RpcSetCharacterType(characterType);
			connectedPlayers[i].CmdSpawnPlayerCharacter(characterType, Vector3.zero, Vector2.zero);
		}
	}

	public void EndRound() {
		foreach (PlayerObject player in connectedPlayers) {
			player.CmdEndRound();
		}
		transform.GetComponentInChildren<NpcManager>().DespawnNPCs();
	}

}
