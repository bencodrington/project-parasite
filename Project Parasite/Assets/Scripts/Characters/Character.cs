using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public abstract class Character : NetworkBehaviour {

	protected SpriteRenderer spriteRenderer;
	protected PhysicsEntity physicsEntity;

	protected Vector3 serverPosition;
	protected bool shouldSnapToServerPosition = false;
	
	protected int characterLayerMask;
	protected int parasiteLayerMask;
	protected int npcLayerMask;
	protected int obstacleLayerMask;

	const float lagLerpFactor = 0.4f;

	// Only initialized for Character objects on the server
	public PlayerObject playerObject;

	public CharacterStats stats;

	protected abstract void HandleInput();

	void Start() {
		// Initialize layer mask
		obstacleLayerMask = 1 << LayerMask.NameToLayer("Obstacles");
		parasiteLayerMask = 1 << LayerMask.NameToLayer("Parasites");
		npcLayerMask = 1 << LayerMask.NameToLayer("NPCs");
		// Character combines both of the above layer masks
		characterLayerMask = parasiteLayerMask + npcLayerMask;
		spriteRenderer = GetComponentInChildren<SpriteRenderer>();
	}

	
	public virtual void Update () {
		// Called once per frame for each Character
		if (hasAuthority) {
			// This character belongs to this client
			HandleInput();
		} else {
			// Verify current position is up to date with server position
			if (shouldSnapToServerPosition) {
				transform.position = serverPosition;
				shouldSnapToServerPosition = false;
			} else {
				transform.position = Vector3.Lerp(transform.position, serverPosition, lagLerpFactor);
			}
		}
	}

	public virtual void FixedUpdate() {
		if (hasAuthority && physicsEntity != null) {
			physicsEntity.Update();
			// Update the server's position
			// TODO: clump these updates to improve network usage?
			CmdUpdatePosition(transform.position, false);
		}
	}

	protected void HandleHorizontalMovement() {
		// TODO: add possibility for being moved outside of input
		bool right = Input.GetKey(KeyCode.D);
		bool left = Input.GetKey(KeyCode.A);
		if (right && !left) {
			physicsEntity.velocityX = stats.movementSpeed;
			SetSpriteFlip(false);
			CmdSetSpriteFlip(false);
		} else if (left && !right) {
			physicsEntity.velocityX = -stats.movementSpeed;
			SetSpriteFlip(true);
			CmdSetSpriteFlip(true);
		} else {
			physicsEntity.velocityX = 0;
		}
	}

	void SetSpriteFlip(bool isFacingLeft) {
		spriteRenderer.flipX = isFacingLeft;
	}

	// COMMANDS

	[Command]
	public void CmdUpdatePosition(Vector3 newPosition, bool snapToNewPos) {
		// TODO: verify new position is legal
		// Only change serverPosition if newPosition is different, to reduce unnecessary Rpc calls
		if (serverPosition != newPosition) {
			serverPosition = newPosition;
			RpcUpdateServerPosition(serverPosition, snapToNewPos);
		}
	}

	[Command]
	public void CmdDeletePhysicsEntity() {
		physicsEntity = null;
	}

	[Command]
	void CmdSetSpriteFlip(bool isFacingLeft) {
		RpcSetSpriteFlip(isFacingLeft);
	}

	// CLIENTRPC

	[ClientRpc]
	void RpcUpdateServerPosition(Vector3 newPosition, bool snapToNewPos) {
		serverPosition = newPosition;
		shouldSnapToServerPosition = snapToNewPos;
	}

	[ClientRpc]
	public void RpcGeneratePhysicsEntity(Vector2 velocity) {
		if (hasAuthority) {
			// Add physics entity
			physicsEntity = new PhysicsEntity(transform, stats.height, stats.width);
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

	[ClientRpc]
	void RpcSetSpriteFlip(bool isFacingLeft) {
		if (!hasAuthority) {
			SetSpriteFlip(isFacingLeft);
		}
	}

}
