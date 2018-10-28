using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class PlayerCharacter : NetworkBehaviour {

	private SpriteRenderer spriteRenderer;
	private Color colour;

	// Use this for initialization
	void Start () {
		spriteRenderer = gameObject.GetComponentInChildren<SpriteRenderer>();
	}
	
	void Update () {
		// Called once per frame for each PlayerCharacter
		if (!hasAuthority) {
			return;
		}

		if (Input.GetKeyDown(KeyCode.W)) {
			this.transform.Translate(0, 1, 0);
		}
		if (Input.GetKeyDown(KeyCode.A)) {
			this.transform.Translate(-1, 0, 0);
		}
		if (Input.GetKeyDown(KeyCode.S)) {
			this.transform.Translate(0, -1, 0);
		}
		if (Input.GetKeyDown(KeyCode.D)) {
			this.transform.Translate(1, 0, 0);
		}

	}

	// COMMANDS

	// CLIENTRPC

	[ClientRpc]
	void RpcOnColourChange(Color colour) {
		spriteRenderer.color = colour;
	}

	[ClientRpc]
	public void RpcUpdatePlayerType(string playerType) {
		// TODO: replace strings and colours with constants
		Color newColour;
		switch (playerType) {
			// Only set colour to red if this character is the parasite & on the parasite player's client
			case "PARASITE": 	newColour = hasAuthority ? Color.red : Color.yellow; break;
			case "HUNTER": 		newColour = Color.green; break;
			case "NEUTRAL":		newColour = Color.yellow; break;
			default: 			newColour = Color.white; break;
		}
		spriteRenderer.color = newColour;
	}
}
