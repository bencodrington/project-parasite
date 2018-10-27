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

		if (Input.GetKeyDown(KeyCode.S)) {
			this.transform.Translate(0, 1, 0);
		}

		if (Input.GetKeyDown(KeyCode.D)) {
			CmdSetRandomColour();
		}

	}

	// COMMANDS

	[Command]
	void CmdSetRandomColour() {
		colour = Random.ColorHSV();
		RpcOnColourChange(colour);
	}

	// CLIENTRPC

	[ClientRpc]
	void RpcOnColourChange(Color colour) {
		spriteRenderer.color = colour;
	}
}
