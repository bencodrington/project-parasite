using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public abstract class Character : MonoBehaviourPun {

	#region [Public Variables]
	
	// Used for sprite flipping and rotating
	public SpriteTransform spriteTransform {get; private set;}
	// Used for displaying the player's name above the character, set in the inspector
	public Nametag nametag;
	
	#endregion
	
	protected SpriteRenderer[] SpriteRenderers {
		get {
			if (spriteRenderers == null) {
				spriteRenderers = GetSpriteRenderers();
			}
			return spriteRenderers; }
	}
	protected Animator animator;
	protected PhysicsEntity physicsEntity;

	protected List<InteractableObject> objectsInRange = new List<InteractableObject>();

	protected bool isMovingRight;
	protected bool isMovingLeft;
	protected bool isMovingUp;
	protected bool isMovingDown;

    protected InputSource input;
	protected string characterName;

	#region [Private Variables]
	
	// Horizontal movement is divided by this each physics update
	// 	a value of 1 indicates that the character will never stop walking once they start
	// 	a value of 2 indicates the walking speed will be halved each frame
	const float MOVEMENT_INPUT_FRICTION = 2f;
	const float REMOTE_CLIENT_UPDATES_PER_SECOND = 5;
	const float TIME_BETWEEN_REMOTE_CLIENT_UPDATES = 1 / REMOTE_CLIENT_UPDATES_PER_SECOND;
	// Used by the owner's client to stagger updates to the remote clients
	float timeUntilNextRemoteClientUpdate;
	// Used by remote clients for position smoothing (see lerp factors below)
	float timeSinceLastOwnerClientUpdate;
	
	// The lerp value used for smoothing a remote client's position to its actual position
	// 	on the owner's client. Should be in the range of (0..1]
	// The higher this is, the snappier lag correction will be
	const float MIN_LAG_LERP_FACTOR = 0.2f;
	// Between updates from the owner's client, the lerp value approaches 1
	// 	This variable keeps track of it
	float currentLerpFactor = MIN_LAG_LERP_FACTOR;
	// By the time timeSinceLastOwnerUpdate >= this value, current lerp value will be 1
	const float MAX_TIME_UNTIL_IN_SYNC = TIME_BETWEEN_REMOTE_CLIENT_UPDATES;
	// If this remote client comes within this distance of where it thinks the owner's client
	// 	is, it will stop smoothing and jump the remaining small distance to the "correct" position
	const float REMOTE_CLIENT_SNAP_DISTANCE = 0.05f;
	// The colour used when drawing debugging visuals
	Color debugDrawColour = Color.magenta;

	float inputVelocityX = 0;
	float inputVelocityY = 0;
	Vector2 lastSentPosition;
	byte lastSentInputs;
	
	SpriteRenderer[] spriteRenderers;
	
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
		OnAwake();
	}

	void Start() {
		spriteTransform = GetComponentInChildren<SpriteTransform>();
		spriteTransform.SetTargetPhysicsEntity(physicsEntity);
		animator = spriteTransform.GetComponent<Animator>();
		OnStart();
		// Only continue if this client owns this gameObject
		if (!HasAuthority()) { return; }
		SendPositionUpdate(true);
	}
	
	public virtual void Update () {
		HandleInput();
		// Called once per frame for each Character
		HandleClientUpdates();
		if (animator) {
			animator.SetBool("isRunning", (isMovingLeft || isMovingRight));
		}
		OnUpdate();
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
		} else {
			// Move visual representation a bit closer to the correct position
			transform.position = Vector3.Lerp(transform.position, physicsEntity.transformPosition, GetCurrentLerpFactor());
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
		foreach (SpriteRenderer sR in SpriteRenderers) {
			// Need to check if each sR is null, because at the time of caching, there may be additional
			// 	sRs that have since been destroyed (i.e. NPC exclamation marks)
			if (sR == null) { continue; }
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

	public void SetName(string newName, bool broadcastUpdate = true) {
		if (broadcastUpdate) {
			photonView.RPC("RpcSetName", RpcTarget.All ,newName);
		} else {
			// This occurs when parasites infect NPC's, we don't need to let hunter clients know
			nametag.SetName(newName);
		}
		characterName = newName;
	}
	
	#endregion

	#region [Protected Methods]

	protected abstract void HandleInput();

	// Overridden by child classes to be called by the base Awake() method
	protected virtual void OnAwake() {}
	// Overridden by child classes to be called by the base Start() method
	protected virtual void OnStart() {}
	// Overridden by child classes to be called by the base Update() method
	protected virtual void OnUpdate() {}

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

	protected void HandleClientUpdates() {
		if (HasAuthority()) {
			timeUntilNextRemoteClientUpdate -= Time.deltaTime;
			if (timeUntilNextRemoteClientUpdate <= 0) {
				// This character belongs to this client
				SendPositionUpdate();
				SendVelocityUpdate();
				SendInputUpdate();
				timeUntilNextRemoteClientUpdate += TIME_BETWEEN_REMOTE_CLIENT_UPDATES;
			}
		} else {
			timeSinceLastOwnerClientUpdate += Time.deltaTime;
		}
	}

	protected void SetSpriteRenderersColour(Color color) {
		foreach (SpriteRenderer sR in SpriteRenderers) {
			// Need to check if each sR is null, because at the time of caching, there may be additional
			// 	sRs that have since been destroyed (i.e. NPC exclamation marks)
			if (sR == null) { continue; }
			sR.color = color;
		}
	}

	protected bool IsSpriteRendererColour(Color color) {
		return SpriteRenderers[0].color == color;
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
	
	void SendVelocityUpdate() {
		if (!HasAuthority()) { return; }
		photonView.RPC("RpcUpdateVelocity", RpcTarget.Others, physicsEntity.GetVelocity());
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
		// Input might not be RemoteInputSource if the client just had authority transferred to it
		//	e.g. if a parasite infects an npc on a remote client, the packedInputs will get here after the
		// 	input source has become local
		if (input is RemoteInputSource) {
			((RemoteInputSource)input).SetInputState(_isMovingUp, _isMovingDown, _isMovingLeft, _isMovingRight);
		}
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

	float GetCurrentLerpFactor() {
		// Returns a value between MIN_LAG_LERP_FACTOR and 1
		if (ClientShouldBeInSync()) {
			currentLerpFactor = 1;
		} else {
			currentLerpFactor = Mathf.Lerp(MIN_LAG_LERP_FACTOR, 1, PercentageUntilInSync());
		}
		return currentLerpFactor;
	}

	bool ClientShouldBeInSync() {
		// 1. If we're already in sync, stay in sync
		// 2. If we're almost exactly in sync, get in sync
		// 3. If we've spent the maximum amount of time smoothing we can, get in sync
		// 4. Else, keep smoothing
		return currentLerpFactor == 1
			|| Vector2.Distance(transform.position, physicsEntity.transformPosition) < REMOTE_CLIENT_SNAP_DISTANCE
			|| timeSinceLastOwnerClientUpdate > MAX_TIME_UNTIL_IN_SYNC;
	}

	float PercentageUntilInSync() {
		return Mathf.Min(1, timeSinceLastOwnerClientUpdate / MAX_TIME_UNTIL_IN_SYNC);
	}
	
	#endregion

	[PunRPC]
	protected void RpcUpdatePosition(Vector2 newPosition, bool snapToNewPos) {
		physicsEntity.SetTransformPosition(newPosition);
		if (spriteTransform != null) {
			spriteTransform.DontFlipThisFrame();
		}
		// Reset smoothing variables
		currentLerpFactor = snapToNewPos ? 1 : MIN_LAG_LERP_FACTOR;
		timeSinceLastOwnerClientUpdate = 0;
	}

	[PunRPC]
	protected void RpcUpdateVelocity(Vector2 velocity) {
		physicsEntity.SetVelocity(velocity);
	}

	[PunRPC]
	protected void RpcUpdateInputs(byte newInputs) {
		UnpackInputs(newInputs);
	}

	[PunRPC]
	protected void AddRemoteInputSource() {
		input = new RemoteInputSource();
	}

	[PunRPC]
	protected void RpcSetName(string newName) {
		nametag.SetName(newName);
	}

	protected void InteractWithObjectsInRange() {
		foreach (InteractableObject interactableObject in objectsInRange) {
			interactableObject.OnInteract();
		}
	}

}
