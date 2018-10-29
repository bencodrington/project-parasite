using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class PlayerCharacter : NetworkBehaviour {

	private SpriteRenderer spriteRenderer;
	private Color colour;

	private PhysicsEntity physicsEntity;

	float movementSpeed = 10f;
	float jumpVelocity = .25f;

	[SyncVar]
	Vector3 serverPosition;
	Vector3 serverPositionSmoothVelocity;

	// Use this for initialization
	void Start () {
		spriteRenderer = gameObject.GetComponentInChildren<SpriteRenderer>();
		RpcGeneratePhysicsEntity();
	}
	
	void Update () {
		// Called once per frame for each PlayerCharacter
		if (hasAuthority) {
			HandleInput();
			if (physicsEntity != null) {
				physicsEntity.Update();
			}
		} else {
			// Verify current position is up to date with server position
			transform.position = Vector3.SmoothDamp(transform.position, serverPosition, ref serverPositionSmoothVelocity, 0.25f);
		}

	}

	void HandleInput() {
		// Movement
		float movementX = Input.GetAxis("Horizontal") * movementSpeed * Time.deltaTime;
		// TODO: check if on ground
		if (Input.GetKeyDown(KeyCode.W)) {
			physicsEntity.AddVelocity(0, jumpVelocity);
		}
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
	void RpcGeneratePhysicsEntity() {
		if (hasAuthority) {
			// Add physics entity
			physicsEntity = new PhysicsEntity(transform);
		}
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
