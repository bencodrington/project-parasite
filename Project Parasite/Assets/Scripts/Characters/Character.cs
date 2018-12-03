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
	protected int hunterLayerMask;
	protected int npcLayerMask;
	protected int obstacleLayerMask;

	// Horizontal movement is divided by this each physics update
	// 	a value of 1 indicates that the character will never stop walking once they start
	// 	a value of 2 indicates the walking speed will be halved each frame
	const float MOVEMENT_INPUT_FRICTION = 2f;
	protected bool isMovingRight;
	protected bool isMovingLeft;
	float inputVelocity = 0;

	const float lagLerpFactor = 0.4f;

	// Only initialized for Character objects on the server
	private PlayerObject _playerObject;
	public PlayerObject PlayerObject {
		get { return _playerObject; }
		set {
			_playerObject = value;
			_playerObject.RegisterOnCharacterDestroyCallback(unregisterAndDestroy);
		}
	}
	private void unregisterAndDestroy() {
		PlayerObject.UnRegisterOnCharacterDestroyCallback(unregisterAndDestroy);
		OnCharacterDestroy();
	}

	public CharacterStats stats;

	protected abstract void HandleInput();

	void Start() {
		// Initialize layer masks
		obstacleLayerMask = 1 << LayerMask.NameToLayer("Obstacles");
		
		parasiteLayerMask = 1 << LayerMask.NameToLayer("Parasites");
		npcLayerMask = 1 << LayerMask.NameToLayer("NPCs");
		hunterLayerMask = 1 << LayerMask.NameToLayer("Hunters");
		// Character combines all three of the above layer masks
		characterLayerMask = parasiteLayerMask + npcLayerMask + hunterLayerMask;
		spriteRenderer = GetComponentInChildren<SpriteRenderer>();

		OnStart();
	}

	// Overridden by child classes to be called by the base Start() method
	protected virtual void OnStart() {}
	
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
			// Based on input, accelerate in direction that's being pressed
			if (isMovingLeft) {
				inputVelocity -= stats.accelerationSpeed;
			} else if (isMovingRight) {
				inputVelocity += stats.accelerationSpeed;
			} else {
				inputVelocity /= MOVEMENT_INPUT_FRICTION;
				// If inputVelocity is sufficiently close to 0
				if (inputVelocity < 0.001) {
					// snap to 0
					inputVelocity = 0;
				}
			}
			// Clamp to maximum input speed
			inputVelocity = Mathf.Clamp(inputVelocity, -stats.movementSpeed, stats.movementSpeed);
			// Pass calculated velocity to physics entity
			physicsEntity.AddInputVelocity(inputVelocity);

			physicsEntity.Update();
			// Update the server's position
			// TODO: clump these updates to improve network usage?
			CmdUpdatePosition(transform.position, false);
		}
	}

	protected void HandleHorizontalMovement() {
		isMovingLeft = false;
		isMovingRight = false;
		bool right = Input.GetKey(KeyCode.D);
		bool left = Input.GetKey(KeyCode.A);
		if (right && !left) {
			isMovingRight = true;
			SetSpriteFlip(false);
			CmdSetSpriteFlip(false);
		} else if (left && !right) {
			isMovingLeft = true;
			SetSpriteFlip(true);
			CmdSetSpriteFlip(true);
		}
	}

	protected virtual void OnCharacterDestroy() {
		Debug.Log("ON CHARACTER DESTROY");
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
