using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Networking;

public class Parasite : Character {

	Color IS_ATTEMPTING_INFECTION_COLOUR = new Color(1, 0, 0, 1);
	Color RESTING_COLOUR = Color.white;

	private float jumpVelocity = 12f;
	private const float MAX_POUNCE_VELOCITY = 30f;
	// Whether the directional keys were being pressed last frame
	private bool oldUp = false;
	private bool oldRight = false;
	private bool oldLeft = false;
	// Whether the action1 key was being pressed last frame
	private bool oldAction1 = false;

	// How many seconds the parasite has been charging to pounce
	private float timeSpentCharging = 0f;
	// The pounce speed will be capped off after this many seconds
	private const float MAX_CHARGE_TIME = 1.5f;
	private bool IsChargingPounce() {
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

	SpriteTransform spriteTransform;

	// The direction that the parasite is attached to (left wall, right wall, ceiling)
	// 	when it began charging a pounce
	private Utility.Directions attachedDirection = Utility.Directions.Null;
	// The distance from the parasite that it can infect NPCs
	const float INFECT_RADIUS = 1f;

	protected override void HandleInput()  {
		// Movement
		bool right = Input.GetKey(KeyCode.D);
		bool left = Input.GetKey(KeyCode.A);
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
			if (right && !left) {
				physicsEntity.applyGravity = !physicsEntity.IsOnRightWall();
				isMovingRight = true;
			} else if (left && !right) {
				physicsEntity.applyGravity = !physicsEntity.IsOnLeftWall();
				isMovingLeft = true;
			} else {
				physicsEntity.applyGravity = true;
			}
		}
		oldRight = right;
		oldLeft = left;

		bool up = Input.GetKey(KeyCode.W);
		bool down = Input.GetKey(KeyCode.S);
		isMovingUp = false;
		isMovingDown = false;
		if (up && !oldUp && physicsEntity.IsOnGround() && !IsChargingPounce()) {
			// Jump
			photonView.RPC("RpcJump", RpcTarget.All);
		}  else if (up && physicsEntity.IsOnWall() && !IsChargingPounce()) {
			// Climb Up
			isMovingUp = true;
		} else if (down && physicsEntity.IsOnWall() && !IsChargingPounce()) {
			// Climb Down
			isMovingDown = true;
		}
		if (up) {
			// Attempt to stick to ceiling
			isTryingToStickToCeiling = true;
		}
		oldUp = up;

		bool action1 = Input.GetMouseButton(0);
		if (action1 && !oldAction1) {
			// Action key just pressed
			PounceIndicator.Show();
			UpdateAttachedDirection();
		}
		// TODO:
		UpdateAttachedDirection();
		if (action1) {
			// Action key is down
			// Charge leap
			timeSpentCharging += Time.deltaTime;
			PounceIndicator.SetPercentage(PounceChargePercentage());
		} else if (oldAction1 && !action1) {
			// On action button release
			if (CanPounce()) {
				// Pounce
				physicsEntity.AddVelocity(CalculatePounceVelocity());
				isTryingToStickToCeiling = false;
			}
			ResetPounceCharge();
			PounceIndicator.Hide();
		}
		oldAction1 = action1;
		
		
		if (oldIsTryingToStickToCeiling != isTryingToStickToCeiling) {
			// Only send updates
			photonView.RPC("RpcSetIsTryingToStickToCeiling", RpcTarget.All, isTryingToStickToCeiling);
			oldIsTryingToStickToCeiling = isTryingToStickToCeiling;
		}

		// Infect
		if (Input.GetMouseButton(1)) {
			IsAttemptingInfection = true;
			Collider2D npc = Physics2D.OverlapCircle(transform.position, INFECT_RADIUS, Utility.GetLayerMask(CharacterType.NPC));
			if (npc != null) {
				InfectNpc(npc.transform.parent.GetComponent<NonPlayerCharacter>());
				DestroySelf();
			}
		} else {
			IsAttemptingInfection = false;
		}

		if (Input.GetKeyDown(KeyCode.E)) {
			InteractWithObjectsInRange();
		}
	}

	#region [Public Methods]
	
	public void OnTakingDamage() {
		StartCoroutine(FlashColours());
	}
	
	#endregion
	
	protected override void OnStart() {
		spriteTransform = GetComponentInChildren<SpriteTransform>();
		spriteTransform.SetTargetTransform(transform);
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
		npc.PlayerObject = PlayerObject;
		// Set isInfected to true/update sprite on new authority's client
		npc.Infect();
		// Update client's camera and render settings to reflect new character
		npc.SetCameraFollow();
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
