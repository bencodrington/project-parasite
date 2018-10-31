using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public abstract class PlayerCharacter : NetworkBehaviour {

	protected SpriteRenderer spriteRenderer;
	protected float height;
	protected PhysicsEntity physicsEntity;
	protected float movementSpeed;
	protected string type = "undefined type";

	[SyncVar]
	protected Vector3 serverPosition;
	Vector3 serverPositionSmoothVelocity;

	
	public virtual void Update () {
		// Called once per frame for each PlayerCharacter
		if (hasAuthority) {
			HandleInput();
			if (physicsEntity != null) {
				Debug.Log("PE.UPDATE for entity of type " + type);
				physicsEntity.Update();
			}
			// Update the server's position
			// TODO: clump these updates to improve network usage?
			CmdUpdatePosition(transform.position);
		} else {
			// Verify current position is up to date with server position
			transform.position = Vector3.SmoothDamp(transform.position, serverPosition, ref serverPositionSmoothVelocity, 0.1f);
		}
	}

	public abstract void ImportStats();
	protected abstract void HandleInput();

	// COMMANDS

	[Command]
	void CmdUpdatePosition(Vector3 newPosition) {
		// TODO: verify new position is legal
		serverPosition = newPosition;
	}

	// CLIENTRPC

	[ClientRpc]
	public void RpcGeneratePhysicsEntity() {
		Debug.Log("GENERATE PHYSICS ENTITY FOR: " + type + "?");
		if (hasAuthority) {
			// TODO: Consider importing stats for all characters on each client, if access to type is required
			ImportStats();
			Debug.Log("GENERATING PHYSICS ENTITY FOR: " + type);
			// Add physics entity
			physicsEntity = new PhysicsEntity(transform, height);
		}
	}

	[ClientRpc]
	public void RpcUpdatePlayerType(string playerType) {
		// TODO: replace strings and colours with constants
		// Color newColour;
		// switch (playerType) {
		// 	// Only set colour to red if this character is the parasite & on the parasite player's client
		// 	case "PARASITE": 	newColour = hasAuthority ? Color.red : Color.yellow; break;
		// 	case "HUNTER": 		newColour = Color.green; break;
		// 	case "NEUTRAL":		newColour = Color.yellow; break;
		// 	default: 			newColour = Color.white; break;
		// }
	}
}
