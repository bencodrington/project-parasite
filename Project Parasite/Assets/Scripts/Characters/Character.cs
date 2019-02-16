﻿using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public abstract class Character : MonoBehaviourPun {

	protected SpriteRenderer spriteRenderer;
	protected PhysicsEntity physicsEntity;

	protected List<InteractableObject> objectsInRange = new List<InteractableObject>();

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


	#region [MonoBehaviour Callbacks]

	void Start() {
		spriteRenderer = GetComponentInChildren<SpriteRenderer>();
		OnStart();

		if (!HasAuthority()) { return; }
    	//  Set character as new target of camera
    	SetCameraFollow();
    	SetRenderLayer();
	}
	
	public virtual void Update () {
		// Called once per frame for each Character
		if (HasAuthority()) {
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
	
	#endregion

	#region [Public Methods]
	
	public void GeneratePhysicsEntity(Vector2 velocity) {
		// Add physics entity
		physicsEntity = new PhysicsEntity(transform, stats.height, stats.width);
		// With starting velocity
		physicsEntity.AddVelocity(velocity.x, velocity.y);
	}

	public void PhysicsUpdate() {
		if (HasAuthority() && physicsEntity != null) {
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
			photonView.RPC("RpcUpdatePosition", RpcTarget.Others, transform.position, false);
		}
	}

	public void SetCameraFollow() {
		FindObjectOfType<CameraFollow>().SetTarget(transform);
	}

	public void RegisterInteractableObject(InteractableObject netId) {
		objectsInRange.Add(netId);
	}
	public void UnregisterInteractableObject(InteractableObject netId) {
		objectsInRange.Remove(netId);
	}

	public void SetRenderLayer() {
		spriteRenderer.sortingLayerName = "ClientCharacter";
	}
	
	#endregion

	#region [Protected Methods]

	protected abstract void HandleInput();

	// Overridden by child classes to be called by the base Start() method
	protected virtual void OnStart() {}

	protected void HandleHorizontalMovement() {
		isMovingLeft = false;
		isMovingRight = false;
		bool right = Input.GetKey(KeyCode.D);
		bool left = Input.GetKey(KeyCode.A);
		if (right && !left) {
			isMovingRight = true;
		} else if (left && !right) {
			isMovingLeft = true;
		}
	}

	protected virtual void OnCharacterDestroy() {}
	
	protected bool HasAuthority() {
		return (photonView.IsMine || !PhotonNetwork.IsConnected);
	}

	#endregion

	[PunRPC]
	protected void RpcUpdatePosition(Vector3 newPosition, bool snapToNewPos) {
		serverPosition = newPosition;
		shouldSnapToServerPosition = snapToNewPos;
	}

	protected void InteractWithObjectsInRange() {
		foreach (InteractableObject interactableObject in objectsInRange) {
			interactableObject.OnInteract();
		}
	}

}
