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

	protected Vector3 serverPosition;
	protected Vector3 serverPositionSmoothVelocity;

	// Only initialized for PlayerCharacter objects on the server
	public PlayerObject playerObject;

	
	public virtual void Update () {
		// Called once per frame for each PlayerCharacter
		if (hasAuthority) {
			HandleInput();
			if (physicsEntity != null) {
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
	protected void CmdUpdatePosition(Vector3 newPosition) {
		// TODO: verify new position is legal
		serverPosition = newPosition;
		RpcUpdateServerPosition(serverPosition);
		// TODO: only change serverPosition if newPosition is different, to reduce unnecessary SyncVar updates
	}

	// CLIENTRPC

	[ClientRpc]
	void RpcUpdateServerPosition(Vector3 newPosition) {
		serverPosition = newPosition;
	}

	[ClientRpc]
	public void RpcGeneratePhysicsEntity() {
		if (hasAuthority) {
			// TODO: Consider importing stats for all characters on each client, if access to type is required
			ImportStats();
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

	void OnServerPosChanged(Vector3 newPosition) {
		serverPosition = newPosition;
		Debug.Log("SPC: " + newPosition);
	}
}
