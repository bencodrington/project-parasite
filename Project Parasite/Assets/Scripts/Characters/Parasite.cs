﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Parasite : Character {

	private float jumpVelocity = 30f;
	private const float MAX_POUNCE_VELOCITY = 45f;
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
	// The angle at which the parasite is currently set to pounce, UP by default
	private float pounceAngle = 90f;
	float PounceAngle {
		get {
			return pounceAngle;
		}
		set {
			pounceAngle = value;
			PounceIndicator.SetAngle(pounceAngle);
		}
	}
	// The number of degrees by which to modify the angle each key press
	private const float POUNCE_ANGLE_INCREMENT = 15f;
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
		spriteRenderer.color = new Color(1, isAttempting ? .5f : 0, 0, 1);
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
		if (IsChargingPounce()) {
			if (attachedDirection == Utility.Directions.Up) {
				// Reverse tilt controls if stuck to ceiling
				if (right && !oldRight) {
					// Tilt angle to the right
					PounceAngle += POUNCE_ANGLE_INCREMENT;
				} else if (left && !oldLeft) {
					// Tilt angle to the left
					PounceAngle -= POUNCE_ANGLE_INCREMENT;
				}
			} else {
				if (right && !oldRight) {
					// Tilt angle to the right
					PounceAngle -= POUNCE_ANGLE_INCREMENT;
				} else if (left && !oldLeft) {
					// Tilt angle to the left
					PounceAngle += POUNCE_ANGLE_INCREMENT;
				}

			}
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
			physicsEntity.AddVelocity(0, jumpVelocity);
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

		bool action1 = Input.GetKey(KeyCode.J);
		if (action1 && !oldAction1) {
			// Action key just pressed
			InitializePounceAngle();
			UpdateAttachedDirection();
			PounceIndicator.Show();
		}
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
		physicsEntity.SetIsTryingToStickToCeiling(isTryingToStickToCeiling);

		// Infect
		if (Input.GetKey(KeyCode.K)) {
			IsAttemptingInfection = true;
			Collider2D npc = Physics2D.OverlapCircle(transform.position, INFECT_RADIUS, Utility.GetLayerMask(CharacterType.NPC));
			if (npc != null) {
				CmdInfectNpc(npc.transform.parent.GetComponent<NetworkIdentity>().netId);
				CmdDestroyParasite();
			}
		} else {
			IsAttemptingInfection = false;
		}
	}

	void InitializePounceAngle() {
		PounceAngle = 90f;
		if (physicsEntity.IsOnCeiling()) {
			// Point down
			PounceAngle = 270f;
		} else if (physicsEntity.IsOnLeftWall()) {
			// Point up-right
			PounceAngle = 60f;
		} else if (physicsEntity.IsOnRightWall()) {
			// Point up-left
			PounceAngle = 120f;
		}
	}

	void UpdateAttachedDirection() {
		attachedDirection = Utility.Directions.Null;
		if (physicsEntity.IsOnCeiling()) {
			attachedDirection = Utility.Directions.Up;
		} else if (physicsEntity.IsOnLeftWall()) {
			attachedDirection = Utility.Directions.Left;
		} else if (physicsEntity.IsOnRightWall()) {
			attachedDirection = Utility.Directions.Right;
		}
	}

	Vector2 CalculatePounceVelocity() {
		float speed = Mathf.Lerp(0, MAX_POUNCE_VELOCITY, PounceChargePercentage());
		float pounceAngleRads = Mathf.Deg2Rad * PounceAngle;
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

	public void OnTakingDamage() {
		StartCoroutine(FlashColours());
	}

	IEnumerator FlashColours() {
		// How long to flash for
		float timeRemaining = 0.5f;
		Color currentColour = spriteRenderer.color;
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
			spriteRenderer.color = currentColour;
			yield return null;
		}
		// Return to default colour
		spriteRenderer.color = Color.red;

	}

	// Commands

	[Command]
	void CmdDestroyParasite() {
		NetworkServer.Destroy(gameObject);
	}

	[Command]
	void CmdInfectNpc(NetworkInstanceId npcNetId) {
		// Find NPC GameObject with matching NetId
		GameObject npcGameObject = NetworkServer.FindLocalObject(npcNetId);
		// Get NonPlayerCharacter script and NetworkIdentity component off of it
		NonPlayerCharacter npc = npcGameObject.GetComponentInChildren<NonPlayerCharacter>();
		NetworkIdentity networkIdentity = npcGameObject.GetComponentInChildren<NetworkIdentity>();
		// Let it know it will be player controlled from now on
		networkIdentity.localPlayerAuthority = true;
		npc.RpcSetLocalPlayerAuthority(true);
		// Store playerObject for eventual transfer back to parasite
		npc.PlayerObject = PlayerObject;
		// Give Parasite player authority over the NPC
		networkIdentity.AssignClientAuthority(PlayerObject.connectionToClient);
		// Delete current physics entity off the server for performance
		npc.CmdDeletePhysicsEntity();
		// TODO: transfer velocity from current physics entity?
		npc.RpcGeneratePhysicsEntity(Vector2.zero);
		// Set isInfected to true/update sprite on new authority's client
		npc.RpcInfect();
		// Update client's camera and render settings to reflect new character
		npc.RpcSetCameraFollow();
		npc.RpcSetRenderLayer();
		PlayerGrid.Instance.CmdSetCharacter(PlayerObject.netId, npc.netId);
	}
}
