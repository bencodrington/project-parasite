﻿using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Networking;

public class Parasite : Character {

	#region [Public Variables]
	
	public AudioClip screechSound;

	public Color[] flashColours;
	
	#endregion

	#region [Private Variables]

	// The distance from the parasite that it can infect NPCs
	const float INFECT_RADIUS = 1f;

	AudioSource screechAudioSource;
	
	Color IS_ATTEMPTING_INFECTION_COLOUR = new Color(1, 0, 0, 1);
	Color RESTING_COLOUR = Color.white;

	// Used for cycling colours
	ColourRotator flashColourRotator;

	float jumpVelocity = 12f;
	const float MAX_POUNCE_VELOCITY = 30f;

	// How many seconds the parasite has been charging to pounce
	float timeSpentCharging = 0f;
	// The pounce speed will be capped off after this many seconds
	const float MAX_CHARGE_TIME = 1.5f;
	bool IsChargingPounce() {
		if (HasAuthority()) {
			return timeSpentCharging > 0f;
		}
		return remoteIsChargingPounce;
	}
	// Used by remote clients to know whether to maintain a wall cling or not
	// 	The owner client should ignore this
	bool remoteIsChargingPounce = false;

	bool _isAttemptingInfection = false;
	bool IsAttemptingInfection {
		get { return _isAttemptingInfection; }
		set {
			if (value != _isAttemptingInfection) {
				// Update sprite
				SetIsAttemptingInfectionSprite(value);
				_isAttemptingInfection = value;
			}
		}
	}
	void SetIsAttemptingInfectionSprite(bool isAttempting) {
		SetSpriteRenderersColour(isAttempting ? IS_ATTEMPTING_INFECTION_COLOUR : RESTING_COLOUR);
	}

	PounceIndicator pounceIndicator;
	PounceIndicator PounceIndicator {
		get {
			if (pounceIndicator == null) {
				pounceIndicator = GetComponentInChildren<PounceIndicator>();
			}
			return pounceIndicator;
		}
	}
	InfectRangeIndicator infectRangeIndicator;

	// The direction that the parasite is attached to (left wall, right wall, ceiling)
	// 	when it began charging a pounce
	Utility.Directions attachedDirection = Utility.Directions.Null;
	bool IsAttachedToWall{
		get { 
			return attachedDirection == Utility.Directions.Left
				|| attachedDirection == Utility.Directions.Right;
		}
	}
	
	#endregion

	protected override void HandleInput()  {
		if (HasAuthority()) {
			input.UpdateInputState();
		}

		if (IsChargingPounce()) {
			// HandlePounceChargeInput doesn't affect attached direction, so the parasite will stay attached
			// 	to the wall/ceiling it started the pounce on
			HandlePounceChargeInput();
			HandlePounceChargeMovementState();
		} else {
			// Only reset the movement state variables if we're not charging pounce, so that the physicsEntity
			// 	keeps checking for colliders on the wall we're attached to
			ResetMovementState();
			HandleHorizontalInput();
			HandleVerticalInput();
		}
		// Update attached direction before handling the pounce input, otherwise it won't be stored
		// 	for the duration of the pounce
		UpdateAttachedDirection();
		HandlePounceInput();
		HandleInfection();
		HandleInteraction();
		physicsEntity.SetIsTryingToStickInDirection(attachedDirection, true);
		UpdateSpriteRotation();
	}

	#region [Public Methods]

	public void TakeDamage(int damage) {
		if (photonView.IsMine) {
			CharacterSpawner.parasiteData.ParasiteTakeDamage(damage);
			if (input.ShouldCameraFollowOwner()) {
				// Only shake the screen if this client is playing as this parasite
				FindObjectOfType<CameraFollow>().ShakeScreen(0.1f, 0.1f);
			}
		}
		if (gameObject.activeInHierarchy) {
			// This gameObject hasn't been marked for deletion when we just applied the damage
			OnTakingDamage();
		}
	}

	public override void Update() {
		HandleInput();
		// Called once per frame for each Parasite
		HandleClientUpdates();
		if (animator) {
			bool shouldRun = false;
			if (IsAttachedToWall) {
				shouldRun = isMovingUp || isMovingDown;
			} else {
				shouldRun = isMovingLeft || isMovingRight;
			}
			animator.SetBool("isRunning", shouldRun);
		}
	}
	
	#endregion
	
	protected override void OnStart() {
		screechAudioSource = Utility.AddAudioSource(gameObject, screechSound, .2f);
		infectRangeIndicator = GetComponentInChildren<InfectRangeIndicator>();
		if (HasAuthority()) {
			infectRangeIndicator.SetOriginTransform(transform);
		} else {
			infectRangeIndicator.enabled = false;
		}
		flashColourRotator = new ColourRotator(flashColours);
	}

	#region [Private Methods]
	
	void UpdateSpriteRotation() {
		spriteTransform.SetRotateDirection(attachedDirection);
	}

	Vector2 CalculatePounceVelocity() {
		float speed = Mathf.Lerp(0, MAX_POUNCE_VELOCITY, PounceChargePercentage());
		float pounceAngleRads = Mathf.Deg2Rad * Utility.GetAngleToMouse(transform.position);
		Vector2 velocity = new Vector2(Mathf.Cos(pounceAngleRads), Mathf.Sin(pounceAngleRads));
		velocity *= speed;
		return velocity;
	}

	float PounceChargePercentage() {
		return Mathf.Clamp01(timeSpentCharging / MAX_CHARGE_TIME);
	}

	bool CanPounce() {
		return physicsEntity.IsOnGround() || physicsEntity.IsOnCeiling() || physicsEntity.IsOnWall();
	}

	void ResetPounceCharge() {
		timeSpentCharging = 0f;
	}
	
	void OnTakingDamage() {
		StartCoroutine(FlashColours());
		if (screechAudioSource == null) {
			// This can happen the frame that an infected NPC gets fried
			// 	The newly spawned parasite can take damage before its OnStart() method
			// 	is called.
			return;
		}
		if (!screechAudioSource.isPlaying) {
			screechAudioSource.pitch = Random.Range(0.5f, 1.5f);
			screechAudioSource.Play();
		}
	}

	IEnumerator FlashColours() {
		// How long to flash for
		float timeRemaining = 0.5f;
		while (timeRemaining > 0) {
			timeRemaining -= Time.deltaTime;
			// Switch to next colour and update spriteRenderer
			SetSpriteRenderersColour(flashColourRotator.GetNextColour());
			yield return null;
		}
		// Return to default colour
		SetSpriteRenderersColour(RESTING_COLOUR);

	}

	void Jump() {
		physicsEntity.AddVelocity(0, jumpVelocity);
	}

	void DestroySelf() {
		PhotonNetwork.Destroy(gameObject);
	}

	void InfectNpc(NonPlayerCharacter npc) {
		// Let the npc know it will be controlled by this player from now on
		npc.photonView.RequestOwnership();
		// Store playerObject for eventual transfer back to parasite
		npc.CharacterSpawner = CharacterSpawner;
		// Set isInfected to true/update sprite on new authority's client
		npc.Infect(input);
		// Update client's camera and render settings to reflect new character
		npc.SetCameraFollow(false);
		npc.SetRenderLayer();
	}

	void ResetMovementState() {
		isMovingLeft = false;
		isMovingRight = false;
		isMovingUp = false;
		isMovingDown = false;
	}

	void HandlePounceChargeInput() {
		float pounceAngle;
		pounceAngle = Utility.GetAngleToMouse(transform.position);
		PounceIndicator.SetAngle(pounceAngle);
	}

	void HandlePounceChargeMovementState() {
		// Reset movement variables except in the direction we're attached
		isMovingLeft	= attachedDirection == Utility.Directions.Left;
		isMovingRight 	= attachedDirection == Utility.Directions.Right;
		isMovingUp 		= attachedDirection == Utility.Directions.Up;
		isMovingDown 	= false; // Never attached to the floor
	}

	void HandleHorizontalInput() {
		if (input.isDown(PlayerInput.Key.right) && !input.isDown(PlayerInput.Key.left)) {
			isMovingRight = true;
		} else if (input.isDown(PlayerInput.Key.left) && !input.isDown(PlayerInput.Key.right)) {
			isMovingLeft = true;
		}
	}

	void HandleVerticalInput() {
		if (HasAuthority()
			&& physicsEntity.IsOnGround()
			&& (input.isJustPressed(PlayerInput.Key.up)
				|| input.isJustPressed(PlayerInput.Key.jump))
			) {
			// Only send jump event if we're the on the client that owns this parasite
			photonView.RPC("RpcJump", RpcTarget.All);
		}  else if (input.isDown(PlayerInput.Key.up) && physicsEntity.IsOnWall()) {
			// Climb Up
			isMovingUp = true;
		} else if (input.isDown(PlayerInput.Key.down) && physicsEntity.IsOnWall()) {
			// Climb Down
			isMovingDown = true;
		}
	}

	void UpdateAttachedDirection() {
		// Don't update the attached direction if we're charging a pounce
		// 	(could be stuck to a wall or ceiling, shouldn't have to hold down the button)
		if (IsChargingPounce()) { return; }
		attachedDirection = Utility.Directions.Null;
		if (input.isDown(PlayerInput.Key.left) && physicsEntity.IsOnLeftWall()) {
			attachedDirection = Utility.Directions.Left;
		} else if (input.isDown(PlayerInput.Key.right) && physicsEntity.IsOnRightWall()) {
			attachedDirection = Utility.Directions.Right;
		} else if (input.isDown(PlayerInput.Key.up) && physicsEntity.IsOnCeiling()) {
			attachedDirection = Utility.Directions.Up;
		}
	}

	void HandlePounceInput() {
		// None of this is relevant on remote clients
		if (!HasAuthority()) { return; }
		if (input.isJustPressed(PlayerInput.Key.action1)) {
			// Action key just pressed
			PounceIndicator.Show();
			photonView.RPC("RpcSetRemoteIsChargingPounce", RpcTarget.All, true);
		}
		if (input.isDown(PlayerInput.Key.action1)) {
			// Action key is down, so charge leap
			timeSpentCharging += Time.deltaTime;
			PounceIndicator.SetPercentage(PounceChargePercentage());
		} else if (input.isJustReleased(PlayerInput.Key.action1)) {
			if (CanPounce()) {
				// Pounce
				physicsEntity.AddVelocity(CalculatePounceVelocity());
				attachedDirection = Utility.Directions.Null;
			}
			ResetPounceCharge();
			PounceIndicator.Hide();
			photonView.RPC("RpcSetRemoteIsChargingPounce", RpcTarget.All, false);
		}
	}

	void HandleInfection() {
		if (input.isDown(PlayerInput.Key.action2)) {
			IsAttemptingInfection = true;
			Collider2D npc = infectRangeIndicator.GetNpcCollider();
			if (npc != null) {
				InfectNpc(npc.transform.parent.GetComponent<NonPlayerCharacter>());
				DestroySelf();
			}
		} else {
			IsAttemptingInfection = false;
		}
	}

	void HandleInteraction() {
		if (input.isJustPressed(PlayerInput.Key.interact)) {
			InteractWithObjectsInRange();
		}
	}
	
	#endregion

	[PunRPC]
	void RpcJump() {
		Jump();
	}

	[PunRPC]
	void RpcSetRemoteIsChargingPounce(bool isChargingPounce) {
		remoteIsChargingPounce = isChargingPounce;
	}

}
