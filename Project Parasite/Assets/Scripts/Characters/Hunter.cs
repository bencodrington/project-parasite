using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Hunter : Character {

	const float DETONATION_RADIUS = 1.5f;
	const float TIME_UNTIL_CHARGE_READY = 1.5f;
	// Maximum distance from the surface of the ground to consider
	// 	that space valid for placing a scanner
	const float SCANNER_OFFSET_X = 0.1f;
	const float SCANNER_OFFSET_Y = 1f;

	public GameObject detonationPrefab;
	public GameObject scannerPrefab;

	bool isCharging = false;
	float timeSpentCharging = 0f;

	Color restingColour = Color.green;
	Color chargedColour = Color.yellow;

	protected override void HandleInput()  {
		// Movement
		HandleHorizontalMovement();
		// Attack
		if (Input.GetMouseButton(0)) {
			if (!isCharging) {
				// Start charging
				isCharging = true;
			} else {
				// Continue charging
				timeSpentCharging += Time.deltaTime;
				UpdateChargeRate(timeSpentCharging / TIME_UNTIL_CHARGE_READY);
				CmdUpdateChargeRate(timeSpentCharging / TIME_UNTIL_CHARGE_READY);
			}
		} else if (isCharging) {
			// Mouse1 just released
			if (timeSpentCharging > TIME_UNTIL_CHARGE_READY) {
				// Sufficiently charged
				FireCharge();
			} // TODO: else { Sputter }
			isCharging = false;
			timeSpentCharging = 0f;
			UpdateChargeRate(0f);
			CmdUpdateChargeRate(0f);
		}

		// Place Scanner
		if (Input.GetMouseButtonDown(1)) {
			float topOfGround;
			// Check for ground in a line above and below mouse location
			Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			Vector2 offset = new Vector2(SCANNER_OFFSET_X, SCANNER_OFFSET_Y);
			// TODO: obstacle layer mask
			Collider2D ground = Physics2D.OverlapArea(mousePosition - offset, mousePosition + offset);
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
		Collider2D target = Physics2D.OverlapPoint(mousePosition, characterLayerMask);
		if (target != null) {
			CmdAttackTarget(target.transform.parent.GetComponent<NetworkIdentity>().netId);
		} else {
			CmdAttackPoint(mousePosition);
		}
	}

	void UpdateChargeRate(float chargeRate) {
		Color newColour = Color.Lerp(restingColour, chargedColour, chargeRate);
		GetComponentInChildren<SpriteRenderer>().color = newColour;
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
		bool isNpc;
		// Spawn detonation object
		RpcSpawnDetonation(targetPoint);
		// Find all characters in radius DETONATION_RADIUS
		Collider2D[] characterCollidersInRadius = Physics2D.OverlapCircleAll(targetPoint, DETONATION_RADIUS, characterLayerMask);
		// For each Character
		foreach (Collider2D characterCollider in characterCollidersInRadius) {
			// Get Character script
			character = characterCollider.transform.parent.GetComponentInChildren<Character>();
			isNpc = character is NonPlayerCharacter;
			// Inflict damage
			if (!isNpc || (isNpc && ((NonPlayerCharacter)character).isInfected)) {
				// Damage parasite
				character.playerObject.RpcTakeDamage(25);
			} else if (isNpc) {
				// Instant kill npcs
				FindObjectOfType<NpcManager>().DespawnNpc(character.netId);
			}
		}

	}

	// TODO: remove
	[Command]
	void CmdScanTarget(NetworkInstanceId npcNetId) {
		// Find npc's game object on this client
		GameObject npcGameObject = NetworkServer.FindLocalObject(npcNetId);
		// Get NonPlayerCharacter script
		NonPlayerCharacter npc = npcGameObject.GetComponentInChildren<NonPlayerCharacter>();
		npc.RpcVerify();
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
}
