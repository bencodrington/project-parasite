using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;

public class Hunter : Character {

	const float DETONATION_RADIUS = 1.5f;
	const float TIME_UNTIL_CHARGE_READY = 1.5f;
	const float TIME_UNTIL_AUTO_DETONATE = 3.5f;
	const float TIME_BETWEEN_READY_AND_DETONATE = TIME_UNTIL_AUTO_DETONATE - TIME_UNTIL_CHARGE_READY;
	// Maximum distance from the surface of the ground to consider
	// 	that space valid for placing a scanner
	const float SCANNER_OFFSET_X = 0.1f;
	const float SCANNER_OFFSET_Y = 1f;

	private int _armourHealth = 100;

	public int ArmourHealth {
		get {return _armourHealth;}
		set {
			_armourHealth = value;
			if (OnArmourHealthChange != null) {
				OnArmourHealthChange(value);
			}
		}
	}

	Action<int> OnArmourHealthChange;

	public GameObject detonationPrefab;
	public GameObject scannerPrefab;

	bool isCharging = false;
	float timeSpentCharging = 0f;

	Color restingColour = Color.white;
	Color chargedColour = Color.green;

	protected override void HandleInput()  {
		// Movement
		HandleHorizontalMovement();
		// Attack
		if (Input.GetMouseButton(0)) {
			if (!isCharging) {
				// Start charging
				isCharging = true;
			} else if (timeSpentCharging > TIME_UNTIL_AUTO_DETONATE) {
				// Detonate on self
				CmdAttackPoint(transform.position);
				ResetCharge();
			} else {
				// Continue charging
				timeSpentCharging += Time.deltaTime;
				UpdateChargeRate(timeSpentCharging);
				CmdUpdateChargeRate(timeSpentCharging);
			}
		} else if (isCharging) {
			// Mouse1 just released
			if (timeSpentCharging > TIME_UNTIL_CHARGE_READY) {
				// Sufficiently charged
				FireCharge();
			} // TODO: else { Sputter }
			ResetCharge();
		}

		// Place Scanner
		if (Input.GetMouseButtonDown(1)) {
			float topOfGround;
			// Check for ground in a line above and below mouse location
			Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			// Maximum distance from mousePosition to check for ground
			Vector2 offset = new Vector2(SCANNER_OFFSET_X, SCANNER_OFFSET_Y);
			// Get all obstacles in the specified area
			Collider2D ground = Physics2D.OverlapArea(mousePosition - offset, mousePosition + offset, obstacleLayerMask);
			if (ground == null) { return; }
			// If ground found, get y coordinate of top of ground
			topOfGround = ground.transform.position.y + ground.transform.localScale.y / 2;
			// If y coordinate is within range of mouse location
			if (Mathf.Abs(topOfGround - mousePosition.y) < SCANNER_OFFSET_Y) {
				// Command server to spawn Scanner here
				CmdSpawnScanner(new Vector2(mousePosition.x, topOfGround));
			}
		}
	}

	void FireCharge() {
		Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		// TODO: If there is an obstacle in the way, detonate there instead
		// Collider2D target = Physics2D.OverlapPoint(mousePosition, characterLayerMask);
		// if (target != null) {
		// 	CmdAttackTarget(target.transform.parent.GetComponent<NetworkIdentity>().netId);
		// } else {
			CmdAttackPoint(mousePosition);
		// }
	}

	void UpdateChargeRate(float timeSpentCharging) {
		float flashRate, timeRemaining, chargeRate;
		Color newColour = Color.green;

		// Get more yellow the closer to being charged we are
		chargeRate = timeSpentCharging / TIME_UNTIL_CHARGE_READY;
		newColour = Color.Lerp(restingColour, chargedColour, chargeRate);
		// If we're fully charged
		if (timeSpentCharging > TIME_UNTIL_CHARGE_READY) {
			timeRemaining = TIME_UNTIL_AUTO_DETONATE - timeSpentCharging;
			// Flash faster the closer to self-combustion
			flashRate = timeRemaining > (TIME_BETWEEN_READY_AND_DETONATE / 3) ? 0.4f : 0.1f;
			// Flash red every [flashRate] seconds for 0.02 seconds
			if ((timeRemaining % flashRate) < 0.02f) {
				newColour = Color.red;
			}
		}
		spriteRenderer.color = newColour;
	}

	void ResetCharge() {
		isCharging = false;
		timeSpentCharging = 0f;
		UpdateChargeRate(0f);
		CmdUpdateChargeRate(0f);
	}

	// Actions

	public void RegisterOnArmourChangeCallback(Action<int> callback) {
		OnArmourHealthChange += callback;
	}

	// Commands

	[Command]
	void CmdAttackTarget(NetworkInstanceId targetNetId) {
		// Find target's game object on this client
		GameObject targetGameObject = NetworkServer.FindLocalObject(targetNetId);
		Vector3 targetPoint = targetGameObject.transform.position;
		CmdAttackPoint(targetPoint);
	}

	[Command]
	void CmdAttackPoint(Vector3 targetPoint) {
		Character character;
		bool isNpc, isHunter, isParasite;
		// Spawn detonation object
		RpcSpawnDetonation(targetPoint);
		// Find all characters in radius DETONATION_RADIUS
		Collider2D[] characterCollidersInRadius = Physics2D.OverlapCircleAll(targetPoint, DETONATION_RADIUS, characterLayerMask);
		// For each Character
		foreach (Collider2D characterCollider in characterCollidersInRadius) {
			// Get Character script
			character = characterCollider.transform.parent.GetComponentInChildren<Character>();
			isNpc = character is NonPlayerCharacter;
			isHunter = character is Hunter;
			isParasite = character is Parasite;
			// Inflict damage
			if (isParasite || (isNpc && ((NonPlayerCharacter)character).isInfected)) {
				// Damage parasite
				character.playerObject.RpcParasiteTakeDamage(25);
			} else if (isNpc) {
				// Instant kill npcs
				FindObjectOfType<NpcManager>().DespawnNpc(character.netId);
			} else if (isHunter) {
				// Damage armour
				((Hunter)character).RpcTakeArmourDamage(25);
			}
		}

	}

	[Command]
	void CmdUpdateChargeRate(float chargeRate) {
		RpcUpdateChargeRate(chargeRate);
	}

	[Command]
	void CmdSpawnScanner(Vector2 position) {
		// Create Scanner game object on the server
		GameObject scanner = Instantiate(scannerPrefab, position, Quaternion.identity);
		// Propogate to all clients
		NetworkServer.Spawn(scanner);
	}

	// ClientRpc

	[ClientRpc]
	void RpcSpawnDetonation(Vector3 detonationPosition) {
		Instantiate(detonationPrefab, detonationPosition, Quaternion.identity);
	}

	[ClientRpc]
	void RpcUpdateChargeRate(float chargeRate) {
		// TODO: send less often and smooth transition on client side? except after firing, will need a smooth flag
		if (!hasAuthority) {
			UpdateChargeRate(chargeRate);
		}
	}

	[ClientRpc]
	void RpcTakeArmourDamage(int damage) {
		if (hasAuthority) {
			ArmourHealth -= damage;
		}
	}
}
