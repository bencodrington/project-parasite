using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Networking;

public class Parasite : Character {

	#region [Public Variables]
	
	public AudioClip screechSound;
	
	#endregion

	#region [Private Variables]

	// The distance from the parasite that it can infect NPCs
	const float INFECT_RADIUS = 1f;

	AudioSource screechAudioSource;
	
	Color IS_ATTEMPTING_INFECTION_COLOUR = new Color(1, 0, 0, 1);
	Color RESTING_COLOUR = Color.white;

	float jumpVelocity = 12f;
	const float MAX_POUNCE_VELOCITY = 30f;

	// How many seconds the parasite has been charging to pounce
	float timeSpentCharging = 0f;
	// The pounce speed will be capped off after this many seconds
	const float MAX_CHARGE_TIME = 1.5f;
	bool IsChargingPounce() {
		return timeSpentCharging > 0f;
	}

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

	bool oldIsTryingToStickToCeiling;

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

	SpriteTransform spriteTransform;

	// The direction that the parasite is attached to (left wall, right wall, ceiling)
	// 	when it began charging a pounce
	Utility.Directions attachedDirection = Utility.Directions.Null;
	
	#endregion

	protected override void HandleInput()  {
		if (HasAuthority()) {
			input.UpdateInputState();
		}

		isMovingLeft = false;
		isMovingRight = false;
		bool isTryingToStickToCeiling = false;
		float pounceAngle;
		if (IsChargingPounce()) {
			pounceAngle = Utility.GetAngleToMouse(transform.position);
			PounceIndicator.SetAngle(pounceAngle);
			// Make sure physicsEntity keeps checking for walls we're attached to
			// 	so that CanPounce() resolves to true.
			if (!physicsEntity.IsOnGround()) {
				switch (attachedDirection) {
					case Utility.Directions.Right: isMovingRight = true; break;
					case Utility.Directions.Left: isMovingLeft = true; break;
					case Utility.Directions.Up: isTryingToStickToCeiling = true; break;
				}
			}
		} else {
			if (input.isDown(PlayerInput.Key.right) && !input.isDown(PlayerInput.Key.left)) {
				physicsEntity.applyGravity = !physicsEntity.IsOnRightWall();
				isMovingRight = true;
			} else if (input.isDown(PlayerInput.Key.left) && !input.isDown(PlayerInput.Key.right)) {
				physicsEntity.applyGravity = !physicsEntity.IsOnLeftWall();
				isMovingLeft = true;
			} else {
				physicsEntity.applyGravity = true;
			}
		}
		isMovingUp = false;
		isMovingDown = false;
		if ((input.isJustPressed(PlayerInput.Key.up) || input.isJustPressed(PlayerInput.Key.jump))
					&& physicsEntity.IsOnGround()
					&& !IsChargingPounce()) {
			// Jump
			photonView.RPC("RpcJump", RpcTarget.All);
		}  else if (input.isDown(PlayerInput.Key.up) && physicsEntity.IsOnWall() && !IsChargingPounce()) {
			// Climb Up
			isMovingUp = true;
		} else if (input.isDown(PlayerInput.Key.down) && physicsEntity.IsOnWall() && !IsChargingPounce()) {
			// Climb Down
			isMovingDown = true;
		}
		if (input.isDown(PlayerInput.Key.up)) {
			// Attempt to stick to ceiling
			isTryingToStickToCeiling = true;
		}
		if (input.isJustPressed(PlayerInput.Key.action1)) {
			// Action key just pressed
			PounceIndicator.Show();
		}
		UpdateAttachedDirection();
		if (input.isDown(PlayerInput.Key.action1)) {
			// Action key is down
			// Charge leap
			timeSpentCharging += Time.deltaTime;
			PounceIndicator.SetPercentage(PounceChargePercentage());
		} else if (input.isJustReleased(PlayerInput.Key.action1)) {
			// On action button release
			if (CanPounce()) {
				// Pounce
				physicsEntity.AddVelocity(CalculatePounceVelocity());
				isTryingToStickToCeiling = false;
			}
			ResetPounceCharge();
			PounceIndicator.Hide();
		}
		
		if (oldIsTryingToStickToCeiling != isTryingToStickToCeiling) {
			// Only send updates
			photonView.RPC("RpcSetIsTryingToStickToCeiling", RpcTarget.All, isTryingToStickToCeiling);
			oldIsTryingToStickToCeiling = isTryingToStickToCeiling;
		}

		// Infect
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

		if (input.isJustPressed(PlayerInput.Key.interact)) {
			InteractWithObjectsInRange();
		}
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
		if (HasAuthority()) {
			// This character belongs to this client
			HandlePositionAndInputUpdates();
		}
		if (animator) {
			bool shouldRun = false;
			if (physicsEntity.applyGravity) {
				// Either on the ceiling, floor, or in midair
				shouldRun = isMovingLeft || isMovingRight;
			} else {
				// Stuck to a wall
				shouldRun = isMovingDown || isMovingUp;
			}
			animator.SetBool("isRunning", shouldRun);
		}
	}
	
	#endregion
	
	protected override void OnStart() {
		spriteTransform = GetComponentInChildren<SpriteTransform>();
		spriteTransform.SetTargetTransform(transform);
		screechAudioSource = Utility.AddAudioSource(gameObject, screechSound, .2f);
		if (HasAuthority()) {
			infectRangeIndicator = GetComponentInChildren<InfectRangeIndicator>();
			infectRangeIndicator.SetOriginTransform(transform);
		}
	}

	#region [Private Methods]
	
	void UpdateAttachedDirection() {
		attachedDirection = Utility.Directions.Null;
		if (physicsEntity.IsOnCeiling()) {
			attachedDirection = Utility.Directions.Up;
		} else if (physicsEntity.IsOnLeftWall()) {
			attachedDirection = Utility.Directions.Left;
		} else if (physicsEntity.IsOnRightWall()) {
			attachedDirection = Utility.Directions.Right;
		}
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
		Color currentColour = Color.red;
		// Used for cycling colours
		Dictionary<Color, Color> nextColour = new Dictionary<Color, Color>();
		nextColour.Add(Color.red, Color.cyan);
		nextColour.Add(Color.cyan, Color.yellow);
		nextColour.Add(Color.yellow, Color.red);
		while (timeRemaining > 0) {
			timeRemaining -= Time.deltaTime;
			// Switch to next colour
			nextColour.TryGetValue(currentColour, out currentColour);
			// Update spriterenderer
			SetSpriteRenderersColour(currentColour);
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
	
	#endregion

	[PunRPC]
	void RpcJump() {
		Jump();
	}

	[PunRPC]
	void RpcSetIsTryingToStickToCeiling(bool isTrying) {
		physicsEntity.SetIsTryingToStickToCeiling(isTrying);
	}

}
