using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public abstract class Character : NetworkBehaviour {

	protected SpriteRenderer spriteRenderer;
	protected PhysicsEntity physicsEntity;

	protected List<NetworkInstanceId> objectsInRange = new List<NetworkInstanceId>();

	protected Vector3 serverPosition;
	protected bool shouldSnapToServerPosition = false;

	// Horizontal movement is divided by this each physics update
	// 	a value of 1 indicates that the character will never stop walking once they start
	// 	a value of 2 indicates the walking speed will be halved each frame
	const float MOVEMENT_INPUT_FRICTION = 2f;
	protected bool isMovingRight;
	protected bool isMovingLeft;
	protected bool isMovingUp;
	protected bool isMovingDown;
	float inputVelocityX = 0;
	float inputVelocityY = 0;

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

	public void PhysicsUpdate() {
		if (hasAuthority && physicsEntity != null) {
			// Based on input, accelerate in direction that's being pressed
			// Horizontal
			if (isMovingLeft) {
				inputVelocityX -= stats.accelerationSpeed;
			} else if (isMovingRight) {
				inputVelocityX += stats.accelerationSpeed;
			} else {
				inputVelocityX /= MOVEMENT_INPUT_FRICTION;
				// If inputVelocity is sufficiently close to 0
				if (inputVelocityX < 0.001) {
					// snap to 0
					inputVelocityX = 0;
				}
			}
			// Vertical
			if (isMovingDown) {
				inputVelocityY -= stats.accelerationSpeed;
			} else if (isMovingUp) {
				inputVelocityY += stats.accelerationSpeed;
			} else {
				inputVelocityY /= MOVEMENT_INPUT_FRICTION;
				// If inputVelocity is sufficiently close to 0
				if (inputVelocityY < 0.001) {
					// snap to 0
					inputVelocityY = 0;
				}
			}
			// Clamp to maximum input speed
			inputVelocityX = Mathf.Clamp(inputVelocityX, -stats.movementSpeed, stats.movementSpeed);
			inputVelocityY = Mathf.Clamp(inputVelocityY, -stats.movementSpeed, stats.movementSpeed);
			// Pass calculated velocity to physics entity
			physicsEntity.AddInputVelocity(inputVelocityX, inputVelocityY);

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

	protected virtual void OnCharacterDestroy() {}

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

	[Command]
	protected void CmdInteractWithObjectsInRange() {
		foreach (NetworkInstanceId netId in objectsInRange) {
			GameObject gameObject = Utility.GetLocalObject(netId, isServer);
			gameObject.GetComponentInChildren<InteractableObject>().OnInteract();
		}
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

	[ClientRpc]
	public void RpcRegisterObject(NetworkInstanceId netId) {
		objectsInRange.Add(netId);
		// TODO: just get the InteractableObject and store that
		if (hasAuthority) {
			// Show 'E' help key
			Utility.GetLocalObject(netId, isServer).GetComponentInChildren<InteractableObject>().SetIsInRange(true);
		}
	}

	[ClientRpc]
	public void RpcUnregisterObject(NetworkInstanceId netId) {
		objectsInRange.Remove(netId);
		if (hasAuthority) {
			// Hide 'E' help key
			Utility.GetLocalObject(netId, isServer).GetComponentInChildren<InteractableObject>().SetIsInRange(false);
		}
	}

}
