using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public abstract class Character : MonoBehaviourPun {

	protected SpriteRenderer spriteRenderer;
	protected PhysicsEntity physicsEntity;

	protected List<InteractableObject> objectsInRange = new List<InteractableObject>();

	protected byte serverInputs;
	protected bool shouldSnapToTruePosition = false;

	protected bool isMovingRight;
	protected bool isMovingLeft;
	protected bool isMovingUp;
	protected bool isMovingDown;

	#region [Private Variables]
	
	// Horizontal movement is divided by this each physics update
	// 	a value of 1 indicates that the character will never stop walking once they start
	// 	a value of 2 indicates the walking speed will be halved each frame
	const float MOVEMENT_INPUT_FRICTION = 2f;
	const float POSITION_UPDATES_PER_SECOND = 5;
	float timeUntilNextPositionUpdate;
	static float timeBetweenPositionUpdates;

	float inputVelocityX = 0;
	float inputVelocityY = 0;
	Vector2 lastSentPosition;
	byte lastSentInputs;
	
	// The higher this is, the snappier lag correction will be
	// 	Should be in the range of (0..1]
	const float LAG_LERP_FACTOR = 0.2f;
	
	#endregion

	// Only initialized for Character objects on the server
	private PlayerObject _playerObject;
	public PlayerObject PlayerObject {
		get { return _playerObject; }
		set {
			_playerObject = value;
		}
	}

	public CharacterStats stats;


	#region [MonoBehaviour Callbacks]

	void Start() {
		spriteRenderer = GetComponentInChildren<SpriteRenderer>();
		GeneratePhysicsEntity();
		OnStart();

		if (!HasAuthority()) { return; }
    	//  Set character as new target of camera
    	SetCameraFollow();
    	SetRenderLayer();
		SendPositionUpdate(true);
		timeBetweenPositionUpdates = 1f / POSITION_UPDATES_PER_SECOND;
	}
	
	public virtual void Update () {
		// Called once per frame for each Character
		if (HasAuthority()) {
			// This character belongs to this client
			HandleInput();
			HandlePositionAndInputUpdates();
		}
	}
	
	#endregion

	#region [Public Methods]

	public void PhysicsUpdate() {
		if (physicsEntity == null) {
			Debug.LogError("Character: PhysicsUpdate(): physics entity is null");
			return;
		}
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
		if (HasAuthority()) {
			// Update the local position based on physics simulation
			transform.position = physicsEntity.transformPosition;
			serverInputs = PackInputs();
		} else if (shouldSnapToTruePosition) {
			// Verify current position is up to date with server position
			transform.position = physicsEntity.transformPosition;
			shouldSnapToTruePosition = false;
		} else {
			// Move visual representation a bit closer to the correct position
			transform.position = Vector3.Lerp(transform.position, physicsEntity.transformPosition, LAG_LERP_FACTOR);
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

	public void SetStartingVelocity(Vector2 velocity) {
		if (physicsEntity == null) {
			// This will likely always happen, as the physics entity is generated
			// 	in Start(), and this function is called before then
			GeneratePhysicsEntity();
		}
		physicsEntity.AddVelocity(velocity.x, velocity.y);
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

	protected void HandlePositionAndInputUpdates() {
		timeUntilNextPositionUpdate -= Time.deltaTime;
		if (timeUntilNextPositionUpdate <= 0) {
			SendPositionUpdate();
			SendInputUpdate();
			timeUntilNextPositionUpdate += timeBetweenPositionUpdates;
		}
	}

	#endregion

	#region [Private Methods]
	
	void GeneratePhysicsEntity() {
		if (physicsEntity != null) {
			// This will happen if SetStartingVelocity() has been called already
			// 	as this will be the second time this is being generated
			return;
		}
		// Add physics entity
		physicsEntity = new PhysicsEntity(transform, stats.height, stats.width);
	}
	
	void SendPositionUpdate(bool shouldSnapToNewPosition = false) {
		// Don't send position update if this isn't our character or if we haven't moved
		if (!HasAuthority() || ((Vector2)transform.position == lastSentPosition)) {
			return;
		}
		photonView.RPC("RpcUpdatePosition", RpcTarget.Others, (Vector2)transform.position, shouldSnapToNewPosition);
		lastSentPosition = transform.position;
	}
	
	void SendInputUpdate() {
		// Don't send input update if this isn't our character or if we haven't started/stopped pressing buttons
		if (!HasAuthority() || (serverInputs == lastSentInputs)) {
			return;
		}
		photonView.RPC("RpcUpdateInputs", RpcTarget.Others, serverInputs);
		lastSentInputs = serverInputs;
	}

	byte PackInputs() {
		byte packedInputs = isMovingUp ? (byte)1 : (byte)0;
		if (isMovingDown) {
			packedInputs |= ((byte)1 << 1);
		}
		if (isMovingLeft) {
			packedInputs |= ((byte)1 << 2);
		}
		if (isMovingRight) {
			packedInputs |= ((byte)1 << 3);
		}
		return packedInputs;
	}

	void UnpackInputs(byte packedInputs) {
		isMovingUp = ((byte) 1 & packedInputs) == 1;
		isMovingDown = ((byte) 2 & packedInputs) == 2;
		isMovingLeft = ((byte) 4 & packedInputs) == 4;
		isMovingRight = ((byte) 8 & packedInputs) == 8;
	}
	
	#endregion

	[PunRPC]
	protected void RpcUpdatePosition(Vector2 newPosition, bool snapToNewPos) {
		physicsEntity.SetTransformPosition(newPosition);
		shouldSnapToTruePosition = snapToNewPos;
	}

	[PunRPC]
	protected void RpcUpdateInputs(byte newInputs) {
		UnpackInputs(newInputs);
	}

	protected void InteractWithObjectsInRange() {
		foreach (InteractableObject interactableObject in objectsInRange) {
			interactableObject.OnInteract();
		}
	}

}
