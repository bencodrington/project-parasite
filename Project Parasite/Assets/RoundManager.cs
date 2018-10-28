using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoundManager : MonoBehaviour {

	// Use this for initialization
	void Start () {
		// Alert PlayerCharacters(PlayerObjects?) that the game has started
		// TODO: cache playercharacters on creation
		// TODO: uncache on leave
		PlayerCharacter[] playerCharacters = FindObjectsOfType<PlayerCharacter>();
		int n = playerCharacters.Length;
		int indexOfParasite = Random.Range(0, n);
		Debug.Log("There are " +
				   n +
				   " players connected. The parasite will be player #" +
				   indexOfParasite);
		for (int i = 0; i < n; i++) {
			if (i == indexOfParasite) {
				playerCharacters[i].RpcUpdatePlayerType("PARASITE");
			} else { // Player is a hunter
				playerCharacters[i].RpcUpdatePlayerType("HUNTER");
			}
		}
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
