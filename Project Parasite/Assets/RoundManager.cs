using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoundManager : MonoBehaviour {

	void Start () {
		SelectParasite();
	}

	void SelectParasite() {
		// TODO: cache playercharacters on creation
		// TODO: uncache on leave
		PlayerCharacter[] playerCharacters = FindObjectsOfType<PlayerCharacter>();
		int n = playerCharacters.Length;
		int indexOfParasite = Random.Range(0, n);
		for (int i = 0; i < n; i++) {
			if (i == indexOfParasite) {
				playerCharacters[i].RpcUpdatePlayerType("PARASITE");
			} else { // Player is a hunter
				playerCharacters[i].RpcUpdatePlayerType("HUNTER");
			}
		}
	}

}
