using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public abstract class Character : MonoBehaviourPun {

	protected SpriteRenderer[] spriteRenderers;
	protected Animator animator;
	protected PhysicsEntity physicsEntity;

	protected List<InteractableObject> objectsInRange = new List<InteractableObject>();

	protected bool shouldSnapToTruePosition = false;

	protected bool isMovingRight;
	protected bool isMovingLeft;
	protected bool isMovingUp;
	protected bool isMovingDown;

    protected InputSource input;

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
	// The colour used when drawing debugging visuals
	Color debugDrawColour = Color.magenta;
	
	#endregion

	// Only initialized for Character objects on the server
	private CharacterSpawner _characterSpawner;
	public CharacterSpawner CharacterSpawner {
		get { return _characterSpawner; }
		set {
			_characterSpawner = value;
		}
	}

	public CharacterStats stats;


	#region [MonoBehaviour Callbacks]

	void Awake() {
		GeneratePhysicsEntity();
	}

	void Start() {
		spriteRenderers = GetSpriteRenderers();
		animator = GetComponentInChildren<SpriteTransform>().GetComponent<Animator>();
		OnStart();
		// Only continue if this client owns this gameObject
		if (!HasAuthority()) { return; }
		SendPositionUpdate(true);
		timeBetweenPositionUpdates = 1f / POSITION_UPDATES_PER_SECOND;
	}
	
	public virtual void Update () {
		HandleInput();
		// Called once per frame for each Character
		if (HasAuthority()) {
			// This character belongs to this client
			HandlePositionAndInputUpdates();
		}
		if (animator) {
			animator.SetBool("isRunning", (isMovingLeft || isMovingRight));
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
		} else if (shouldSnapToTruePosition) {
			// Verify current position is up to date with server position
			transform.position = physicsEntity.transformPosition;
			shouldSnapToTruePosition = false;
		} else {
			// Move visual representation a bit closer to the correct position
			transform.position = Vector3.Lerp(transform.position, physicsEntity.transformPosition, LAG_LERP_FACTOR);
		}
	}

	public void SetCameraFollow(bool forceSnapToTarget) {
		FindObjectOfType<CameraFollow>().SetTarget(transform, forceSnapToTarget);
	}

	public void RegisterInteractableObject(InteractableObject netId) {
		objectsInRange.Add(netId);
	}
	public void UnregisterInteractableObject(InteractableObject netId) {
		objectsInRange.Remove(netId);
	}

	public void SetRenderLayer(string renderLayerName = "ClientCharacter") {
		// CLEANUP: This can be neater
		if (spriteRenderers == null) {
			spriteRenderers = GetSpriteRenderers();
		}
		foreach (SpriteRenderer sR in spriteRenderers) {
			sR.sortingLayerName = renderLayerName;
		}
	}

	public void SetStartingVelocity(Vector2 velocity) {
		if (physicsEntity == null) {
			// This will likely always happen, as the physics entity is generated
			// 	in Start(), and this function is called before then
			GeneratePhysicsEntity();
		}
		physicsEntity.AddVelocity(velocity.x, velocity.y);
	}

	public void SetInputSource(InputSource newInputSource, bool shouldCameraFollowSnap = false) {
		input = newInputSource;
		if (newInputSource.ShouldCameraFollowOwner()) {
			SetCameraFollow(shouldCameraFollowSnap);
		}
		input.SetOwner(this);
		photonView.RPC("AddRemoteInputSource", RpcTarget.Others);
	}

	public virtual bool IsUninfectedNpc() {
		return false;
	}

	public void DebugDrawBounds(bool drawPhysicsEntity = true, bool bottomOnly = false, float duration = 15) {
		DebugDrawBounds(debugDrawColour, drawPhysicsEntity, bottomOnly, duration);
		ModifyDebugDrawColour();
	}

	public void DebugDrawBounds(Color colour, bool drawPhysicsEntity = true, bool bottomOnly = false, float duration = 15) {
		if (drawPhysicsEntity) {
			physicsEntity.DebugDrawRayCastOrigins(duration);
		}
		BoxCollider2D collider = GetComponentInChildren<BoxCollider2D>();
		float width = collider.size.x;
		float height = collider.size.y;
		Vector2 bottomLeft = (Vector2)transform.position + new Vector2(-width / 2, -height / 2);
		Vector2 bottomRight = (Vector2)transform.position + new Vector2(width / 2, -height / 2);
		Debug.DrawLine(bottomLeft, bottomRight, colour, duration);
		if (!bottomOnly) {
			Vector2 topLeft = (Vector2)transform.position + new Vector2(-width / 2, height / 2);
			Vector2 topRight = (Vector2)transform.position + new Vector2(width / 2, height / 2);
			Debug.DrawLine(topLeft, topRight, colour, duration);
			Debug.DrawLine(topLeft, bottomLeft, colour, duration);
			Debug.DrawLine(bottomRight, topRight, colour, duration);
		}
	}

	public void Move(Vector2 displacement) {
		// TODO: ensure that transform.position is always updated after physicsEntity's transformPosition is updated (update() and move()) ?
		physicsEntity.Move(displacement);
		transform.position = physicsEntity.transformPosition;
	}

	public void Destroy() {
		OnCharacterDestroy();
		PhotonNetwork.Destroy(gameObject);
	}
	
	#endregion

	#region [Protected Methods]

	protected abstract void HandleInput();

	// Overridden by child classes to be called by the base Start() method
	protected virtual void OnStart() {}

	protected void HandleHorizontalMovement() {
		isMovingLeft = false;
		isMovingRight = false;
		if (input.isDown(PlayerInput.Key.right) && !input.isDown(PlayerInput.Key.left)) {
			isMovingRight = true;
		} else if (input.isDown(PlayerInput.Key.left) && !input.isDown(PlayerInput.Key.right)) {
			isMovingLeft = true;
		}
	}

	protected virtual void OnCharacterDestroy() {}
	
	protected bool HasAuthority() {
		return photonView.IsMine;
	}

	protected void HandlePositionAndInputUpdates() {
		timeUntilNextPositionUpdate -= Time.deltaTime;
		if (timeUntilNextPositionUpdate <= 0) {
			SendPositionUpdate();
			SendInputUpdate();
			timeUntilNextPositionUpdate += timeBetweenPositionUpdates;
		}
	}

	protected void SetSpriteRenderersColour(Color color) {
		foreach (SpriteRenderer sR in spriteRenderers) {
			sR.color = color;
		}
	}

	#endregion

	#region [Private Methods]

	SpriteRenderer[] GetSpriteRenderers() {
		List<SpriteRenderer> renderers = new List<SpriteRenderer>();
		foreach (SpriteRenderer sR in GetComponentsInChildren<SpriteRenderer>()) {
			// Exclude renderers that shouldn't be controlled by this code (e.g. arrow indicator for parasite)
			if (!sR.gameObject.CompareTag("IgnoreComponent")) {
				renderers.Add(sR);
			}
		}
		return renderers.ToArray();
	}
	
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
		byte serverInputs = PackInputs();
		// Don't send input update if this isn't our character or if we haven't started/stopped pressing buttons
		if (!HasAuthority() || (serverInputs == lastSentInputs)) {
			return;
		}
		photonView.RPC("RpcUpdateInputs", RpcTarget.Others, serverInputs);
		lastSentInputs = serverInputs;
	}

	byte PackInputs() {
		byte packedInputs = input.isDown(InputSource.Key.up) ? (byte)1 : (byte)0;
		if (input.isDown(InputSource.Key.down)) {
			packedInputs |= ((byte)1 << 1);
		}
		if (input.isDown(InputSource.Key.left)) {
			packedInputs |= ((byte)1 << 2);
		}
		if (input.isDown(InputSource.Key.right)) {
			packedInputs |= ((byte)1 << 3);
		}
		return packedInputs;
	}

	void UnpackInputs(byte packedInputs) {
		bool _isMovingUp = ((byte) 1 & packedInputs) == 1;
		bool _isMovingDown = ((byte) 2 & packedInputs) == 2;
		bool _isMovingLeft = ((byte) 4 & packedInputs) == 4;
		bool _isMovingRight = ((byte) 8 & packedInputs) == 8;
		((RemoteInputSource)input).SetInputState(_isMovingUp, _isMovingDown, _isMovingLeft, _isMovingRight);
	}

	void ModifyDebugDrawColour() {
		float rModifier = 1;
		float gModifier = 1;
		float bModifier = 1;
		if (debugDrawColour.r >= 0.5) {
			rModifier = 0.9f;
		} else if (debugDrawColour.g >= 0.5) {
			gModifier = 0.9f;
		} else {
			bModifier = 0.9f;
		}
		debugDrawColour = new Color(debugDrawColour.r * rModifier, debugDrawColour.g * gModifier, debugDrawColour.b * bModifier);
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

	[PunRPC]
	protected void AddRemoteInputSource() {
		input = new RemoteInputSource();
	}

	protected void InteractWithObjectsInRange() {
		foreach (InteractableObject interactableObject in objectsInRange) {
			interactableObject.OnInteract();
		}
	}

}
