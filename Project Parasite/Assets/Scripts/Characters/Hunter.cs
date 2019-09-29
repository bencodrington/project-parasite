using System.Collections.Generic;
using UnityEngine;
using System;
using Photon.Pun;
using System.Collections;

public class Hunter : Character {

	#region [Public Variables]

	public AudioClip cantPlaceOrbSound;
	public AudioClip placeOrbSound;
	public AudioClip throwOrbSound;
	public GameObject orbPrefab;
	public GameObject orbUiManagerPrefab;
	public GameObject orbInactivePrefab;
	public bool isNpcControlled = false;
	
	#endregion

	#region [Private Variables]

	float jumpVelocity = 15f;
	// The maximum number of orbs that this hunter can have spawned at any given time 
	const int MAX_ORB_COUNT = 4;
	// The amount of time a hunter will cling to a wall for if they collide at maximum speed
	const float MAX_CLING_TIME = 1.5f;
	// After this distance from the hunter, the orb will take MAX_ORB_THROW_DELAY to get to its destination
	const float ORB_THROW_DELAY_CAP_DISTANCE = 5f;
	// How long the orb will take to get to its destination at ORB_THROW_DELAY_CAP_DISTANCE or farther
	const float MAX_ORB_THROW_DELAY = 0.3f;

	// The size of the box around newly-placed orbs, inside of which
	// 	NPCs will be alerted to run away
	Vector2 NPC_ALERT_RANGE = new Vector2(12, 4);

	OrbUiManager orbUiManager;
	OrbBeamRangeManager orbBeamRangeManager;
	AudioSource cantPlaceOrbAudioSource;
	AudioSource placeOrbAudioSource;
	AudioSource throwOrbAudioSource;
	// Backpack's transform is cached so that orbs can be sent and recalled
	// 	from/to its position
	Transform backpackTransform;

	Queue<Orb> orbs;

	bool isClingingToLeftWall;
	bool isClingingToRightWall;

	bool IsClingingToWall {
		get { return isClingingToLeftWall || isClingingToRightWall; }
	}

	float timeSpentClinging;
	Utility.Directions mostRecentWallClingDirection = Utility.Directions.Null;

	#endregion

	protected override void OnStart() {
		orbs = new Queue<Orb>();
		// Cache reference to orb beam range manager
		orbBeamRangeManager = GetComponentInChildren<OrbBeamRangeManager>();
		if (!isNpcControlled && HasAuthority()) {
			// Spawn orb UI manager to display how many orbs are remaining
			orbUiManager = Instantiate(orbUiManagerPrefab,
										Vector3.zero,
										Quaternion.identity,
										// Anchor it to the canvas
										UiManager.Instance.GetCanvas()
			).GetComponent<OrbUiManager>();
			// Initialize it with the maximum orbs to spawn
			orbUiManager.setMaxOrbCount(MAX_ORB_COUNT);

			cantPlaceOrbAudioSource = Utility.AddAudioSource(gameObject, cantPlaceOrbSound);

		} else {
			orbBeamRangeManager.shouldShowMarkers = false;
		}
		placeOrbAudioSource = Utility.AddAudioSource(gameObject, placeOrbSound);
		throwOrbAudioSource = Utility.AddAudioSource(gameObject, throwOrbSound);
		backpackTransform = Utility.GetChildWithTag("OrbDestination", gameObject).transform;
	}

	protected override void HandleInput()  {
		if (HasAuthority()) {
			input.UpdateInputState();
		}

		// Movement
		isMovingLeft = false;
		isMovingRight = false;
		bool wasClingingToWall = IsClingingToWall;
		if (input.isDown(PlayerInput.Key.right) && !input.isDown(PlayerInput.Key.left)) {
			isMovingRight = true;
			isClingingToRightWall = physicsEntity.IsOnRightWall();
			isClingingToLeftWall = false;
		} else if (input.isDown(PlayerInput.Key.left) && !input.isDown(PlayerInput.Key.right)) {
			isMovingLeft = true;
			isClingingToLeftWall = physicsEntity.IsOnLeftWall();
			isClingingToRightWall = false;
		} else {
			isClingingToLeftWall = false;
			isClingingToRightWall = false;
		}
		HandleWallClinging(wasClingingToWall);

		// If up was pressed this frame for the first time and the player is on the ground
		if (HasAuthority()
			&& physicsEntity.IsOnGround()
			&& (input.isJustPressed(PlayerInput.Key.up)
				|| input.isJustPressed(PlayerInput.Key.jump))
			) {
			// Only send jump event if this client owns this hunter
			photonView.RPC("RpcJump", RpcTarget.All);
		}

		if (input.isJustPressed(PlayerInput.Key.interact)) {
			InteractWithObjectsInRange();
		}

		// Place orb
		if (input.isJustPressed(PlayerInput.Key.action1)) {
			// CLEANUP: this can be cleaner, once InputManager is implemented
			// Don't spawn orb if clicking elevator button
			if (Physics2D.OverlapPoint(input.getMousePosition(), Utility.GetLayerMask("clickable")) == null) {
				AttemptToSpawnOrb(input.getMousePosition());
			}
		}
		// Recall orb
		if (input.isJustPressed(PlayerInput.Key.action2)) {
			AttemptToRecallOrb();
		}
	}

	protected override void OnUpdate() {
		animator.SetBool("isAscending", physicsEntity.IsAscending());
		animator.SetBool("isOnGround", physicsEntity.IsOnGround());
	}

	#region [Public Methods]
	
	public void Repel(Vector2 forceDirection, float force) {
		// Distribute the force between the x and y coordinates
		forceDirection.Normalize();
		forceDirection *= force;
		// Transfer this force to the physics entity to handle it
		physicsEntity.AddVelocity(forceDirection.x, forceDirection.y);
		// Allow hunters to jump up the same wall
		mostRecentWallClingDirection = Utility.Directions.Null;
	}
	
	#endregion

	protected override void OnCharacterDestroy() {
		DestroyAllOrbs();
		if (!isNpcControlled && HasAuthority()) {
			Destroy(orbUiManager.gameObject);
		}
	}

	#region [Private Methods]
	
	void AttemptToSpawnOrb(Vector2 atPosition) {
		if (orbs.Count >= MAX_ORB_COUNT) { 
			OrbSpawnFailed();
			return; 
		}
		photonView.RPC("RpcSpawnOrb", RpcTarget.All, atPosition);
	}

	void OrbSpawnFailed() {
		orbUiManager.FlashPlaceholders();
		orbUiManager.ShowRecallAlert();
		cantPlaceOrbAudioSource.Play();
	}

	void AttemptToRecallOrb() {
		if (orbs.Count <= 0) { return; }
		photonView.RPC("RpcRecallOrb", RpcTarget.All);
	}

	void DestroyAllOrbs() {
		while (orbs.Count > 0) {
			AttemptToRecallOrb();
		}
	}

	void AlertNpcsInRange(Vector2 ofPosition) {
		// Find all NPCs in range
		Collider2D[] npcs = Physics2D.OverlapBoxAll(ofPosition, NPC_ALERT_RANGE, 0, Utility.GetLayerMask(CharacterType.NPC));
		NonPlayerCharacter npc;
		// Alert each NPC
		foreach (Collider2D npcCollider in npcs) {
			npc = npcCollider.transform.parent.gameObject.GetComponentInChildren<NonPlayerCharacter>();
			npc.NearbyOrbAlert(ofPosition);
		}
	}

	void RecallOrb() {
		Destroy(orbs.Dequeue().gameObject);
		if (!isNpcControlled && HasAuthority()) {
			// Update the number of remaining orbs currently displayed onscreen
			orbUiManager.OnOrbCountChange(orbs.Count);
			// User can definitely place at least one orb, so show markers
			orbBeamRangeManager.shouldShowMarkers = true;
		}
	}

	void Jump() {
		animator.SetTrigger("startJump");
		physicsEntity.AddVelocity(0, jumpVelocity);
	}

	void HandleWallClinging(bool wasClingingToWall) {
		if (physicsEntity.IsOnGround()) {
			// After landing on the ground, make grabbing a wall in either direction
			// 	reset the cling timer
			mostRecentWallClingDirection = Utility.Directions.Null;
		}
		// Convert cling direction to a Utility.Directions value for easy comparison
		Utility.Directions wallClingDirection = Utility.Directions.Null;
		if (isClingingToLeftWall) {
			wallClingDirection = Utility.Directions.Left;
		} else if (isClingingToRightWall) {
			wallClingDirection = Utility.Directions.Right;
		}
		// Don't reset the cling timer if this hunter has grabbed the same wall repeatedly
		bool isFirstClingOnThisWall = wallClingDirection != mostRecentWallClingDirection;
		// Whether or not this is the first frame we've grabbed this wall
		bool didJustGrabWall = !wasClingingToWall && IsClingingToWall;
		if (isFirstClingOnThisWall && didJustGrabWall) {
			// Reset cling time
			timeSpentClinging = 0;
			// Store direction
			mostRecentWallClingDirection = wallClingDirection;
		}
		// Reset physics entity's instructions from last frame
		physicsEntity.SetIsTryingToStickInDirection(Utility.Directions.Null);
		if (IsClingingToWall && timeSpentClinging <= MAX_CLING_TIME) {
			timeSpentClinging += Time.deltaTime;
			physicsEntity.SetIsTryingToStickInDirection(wallClingDirection);
			animator.SetBool("isClingingToWall", true);
			animator.SetFloat("wallClingProgress", timeSpentClinging / MAX_CLING_TIME);
		} else {
			animator.SetBool("isClingingToWall", false);
		}
	}

	Orb SpawnOrb(Vector2 atPosition) {
		// Create orb game object
		GameObject orbGameObject = Instantiate(orbPrefab, atPosition, Quaternion.identity);
		Orb orb = orbGameObject.GetComponent<Orb>();

		// If new orb is within "beaming" range of most recently placed orb
		if (orbBeamRangeManager.isInRange(atPosition)) {
			orb.SpawnBeamToPreviousOrb(orbBeamRangeManager.MostRecentOrb);
		}

		// Add to queue
		orbs.Enqueue(orb);
		// Update reference to most recent orb for displaying distance limit to player
		orbBeamRangeManager.MostRecentOrb = orb;
		if (!isNpcControlled && HasAuthority()) {
			// Update the number of remaining orbs currently displayed onscreen
			orbUiManager.OnOrbCountChange(orbs.Count);
			// CLEANUP: this should probably be extracted to the rangemanager itself
			// Hide markers if the user can't place more orbs
			if (orbs.Count == MAX_ORB_COUNT) {
				orbBeamRangeManager.shouldShowMarkers = false;
			}
		}
		orb.gameObject.SetActive(false);
		return orb;
	}

	void ActivateOrb(Orb orb) {
		// Make sure orb hasn't been recalled already
		if (orb == null) { return; }
		orb.gameObject.SetActive(true);
		placeOrbAudioSource.Play();
		FindObjectOfType<CameraFollow>().ShakeScreen(0.3f, 0.1f);
		AlertNpcsInRange(orb.transform.position);
		orb.SetActive();
	}

	IEnumerator StartThrowingOrb(Vector2 atPosition) {
		// Play arm swing animation
		PlayOrbPlaceAnimation(atPosition);
		// Play orb throw sound
		throwOrbAudioSource.Play();
		Orb newOrb = SpawnOrb(atPosition);
		// Delay based on distance
		float percentOfMaxRange = Mathf.Clamp01(Vector2.Distance(backpackTransform.position, atPosition) / ORB_THROW_DELAY_CAP_DISTANCE);
		float delayLength = Mathf.Lerp(0f, MAX_ORB_THROW_DELAY, percentOfMaxRange);
		OrbInactive inactiveOrb = Instantiate(orbInactivePrefab, backpackTransform.position, Quaternion.identity).GetComponent<OrbInactive>();
		inactiveOrb.SetDestinationAndStartMoving(atPosition, delayLength);
		yield return new WaitForSeconds(delayLength);
		ActivateOrb(newOrb);
	}

	IEnumerator StartRecallingOrb() {
		// TODO: play recall sound
		// Delay based on distance
		Orb orbToRecall = orbs.Dequeue();
		Destroy(orbToRecall.gameObject);
		float percentOfMaxRange = Mathf.Clamp01(Vector2.Distance(backpackTransform.position, orbToRecall.transform.position) / ORB_THROW_DELAY_CAP_DISTANCE);
		float delayLength = Mathf.Lerp(0f, MAX_ORB_THROW_DELAY, percentOfMaxRange);
		OrbInactive inactiveOrb = Instantiate(orbInactivePrefab, orbToRecall.transform.position, Quaternion.identity).GetComponent<OrbInactive>();
		inactiveOrb.SetDestinationAndStartMoving(backpackTransform, delayLength);
		yield return new WaitForSeconds(delayLength);
		OnOrbReturned();
	}

	void OnOrbReturned() {
		if (!isNpcControlled && HasAuthority()) {
			// Update the number of remaining orbs currently displayed onscreen
			orbUiManager.OnOrbCountChange(orbs.Count);
			// User can definitely place at least one orb, so show markers
			orbBeamRangeManager.shouldShowMarkers = true;
		}
	}

	void PlayOrbPlaceAnimation(Vector2 orbPosition) {
		float angle = Vector2.SignedAngle(Vector2.up, orbPosition - (Vector2)transform.position);
		string trigger = "";
		// angle is 0 for up, 90 for left, 180 for down, -90 for right
		if (angle > 0 && angle < 160) {
			trigger = "isPlacingOrbBehind";
		} else if (angle >= 160 || angle <= -120) {
			trigger = "isPlacingOrbLower";
		} else if (angle <= 0 && angle > -45) {
			trigger = "isPlacingOrbUpper";
		} else { // angle <= -45 && angle > -120
			trigger = "isPlacingOrb";
		}
		animator.SetTrigger(trigger);
	}
	
	#endregion

	[PunRPC]
	void RpcSpawnOrb(Vector2 atPosition) {
		StartCoroutine(StartThrowingOrb(atPosition));
	}

	[PunRPC]
	void RpcRecallOrb() {
		StartCoroutine(StartRecallingOrb());
	}

	[PunRPC]
	void RpcJump() {
		Jump();
	}
}
