using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class PlayerCharacter : NetworkBehaviour {

	private SpriteRenderer spriteRenderer;
	private Color colour;

	float movementSpeed = 10f;

	[SyncVar]
	Vector3 serverPosition;
	Vector3 serverPositionSmoothVelocity;

	// Use this for initialization
	void Start () {
		spriteRenderer = gameObject.GetComponentInChildren<SpriteRenderer>();
	}
	
	void Update () {
		// Called once per frame for each PlayerCharacter
		if (hasAuthority) {
			HandleInput();
		} else {
			// Verify current position is up to date with server position
			transform.position = Vector3.SmoothDamp(transform.position, serverPosition, ref serverPositionSmoothVelocity, 0.25f);
		}

		// if (Input.GetKeyDown(KeyCode.W)) {
		// 	this.transform.Translate(0, 1, 0);
		// }
		// if (Input.GetKeyDown(KeyCode.A)) {
		// 	this.transform.Translate(-1, 0, 0);
		// }
		// if (Input.GetKeyDown(KeyCode.S)) {
		// 	this.transform.Translate(0, -1, 0);
		// }
		// if (Input.GetKeyDown(KeyCode.D)) {
		// 	this.transform.Translate(1, 0, 0);
		// }


	}

	void HandleInput() {
		// Movement
		float movementX = Input.GetAxis("Horizontal") * movementSpeed * Time.deltaTime;
		// Has authority, so translate immediately
		transform.Translate(movementX, 0, 0);
		// Update the server's position
		// TODO: clump these updates to improve network usage?
		CmdUpdatePosition(transform.position);

	}

	// COMMANDS

	[Command]
	void CmdUpdatePosition(Vector3 newPosition) {
		// TODO: verify new position is legal
		serverPosition = newPosition;

	}

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
