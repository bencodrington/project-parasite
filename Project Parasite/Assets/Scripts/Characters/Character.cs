using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public abstract class Character : NetworkBehaviour {

	protected SpriteRenderer spriteRenderer;
	protected float height;
	protected float width;
	protected PhysicsEntity physicsEntity;
	protected float movementSpeed;

	protected Vector3 serverPosition;
	
	protected int characterLayerMask;
	protected int parasiteLayerMask;
	protected int npcLayerMask;

	const float lagLerpFactor = 0.4f;

	// Only initialized for Character objects on the server
	public PlayerObject playerObject;

	public abstract void ImportStats();
	protected abstract void HandleInput();

	void Start() {
		// Initialize layer mask
		parasiteLayerMask = 1 << LayerMask.NameToLayer("Parasites");
		npcLayerMask = 1 << LayerMask.NameToLayer("NPCs");
		// Character combines both of the above layer masks
		characterLayerMask = parasiteLayerMask + npcLayerMask;
	}

	
	public virtual void Update () {
		// Called once per frame for each Character
		if (hasAuthority) {
			// This character belongs to this client
			HandleInput();
		} else {
			// Verify current position is up to date with server position
			transform.position = Vector3.Lerp(transform.position, serverPosition, lagLerpFactor);
		}
	}

	public virtual void FixedUpdate() {
		if (hasAuthority && physicsEntity != null) {
			physicsEntity.Update();
			// Update the server's position
			// TODO: clump these updates to improve network usage?
			CmdUpdatePosition(transform.position);
		}
	}

	protected void HandleHorizontalMovement() {
		// TODO: add possibility for being moved outside of input
		bool right = Input.GetKey(KeyCode.D);
		bool left = Input.GetKey(KeyCode.A);
		if (right && !left) {
			physicsEntity.velocityX = movementSpeed;
		} else if (left && !right) {
			physicsEntity.velocityX = -movementSpeed;
		} else {
			physicsEntity.velocityX = 0;
		}
	}

	// COMMANDS

	[Command]
	protected void CmdUpdatePosition(Vector3 newPosition) {
		// TODO: verify new position is legal
		// Only change serverPosition if newPosition is different, to reduce unnecessary Rpc calls
		if (serverPosition != newPosition) {
			serverPosition = newPosition;
			RpcUpdateServerPosition(serverPosition);
		}
	}

	[Command]
	public void CmdDeletePhysicsEntity() {
		physicsEntity = null;
	}

	// CLIENTRPC

	[ClientRpc]
	void RpcUpdateServerPosition(Vector3 newPosition) {
		serverPosition = newPosition;
	}

	[ClientRpc]
	public void RpcGeneratePhysicsEntity(Vector2 velocity) {
		if (hasAuthority) {
			ImportStats();
			// Add physics entity
			physicsEntity = new PhysicsEntity(transform, height, width);
			// With starting velocity
			physicsEntity.AddVelocity(velocity.x, velocity.y);
		}
	}

	[ClientRpc]
	public void RpcSetCameraFollow() {
		if (hasAuthority) {
			FindObjectOfType<CameraFollow>().SetTarget(transform);
		}
	}

	[ClientRpc]
	public void RpcSetRenderLayer() {
		if (hasAuthority) {
			GetComponentInChildren<SpriteRenderer>().sortingLayerName = "ClientCharacter";
		}
	}

}
