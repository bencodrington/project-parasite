using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Hunter : Character {

	// The time it takes to fully scan an NPC
	const float SCAN_TIME = 0.75f;
	const float DETONATION_RADIUS = 1.5f;
	const float TIME_UNTIL_CHARGE_READY = 1.5f;

	public GameObject detonationPrefab;

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

		// Scan NPC
		if (Input.GetMouseButtonDown(1)) {
			Collider2D npc = Physics2D.OverlapPoint(Camera.main.ScreenToWorldPoint(Input.mousePosition), npcLayerMask);
			if (npc != null) {
				StartCoroutine(StartScan(npc.transform.parent.GetComponent<NetworkIdentity>().netId));
			}
		}
	}

	void FireCharge() {
		Vector2 mousePosition;
		mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		// TODO: If there is an obstacle in the way, detonate there instead
		Collider2D target = Physics2D.OverlapPoint(mousePosition, characterLayerMask);
		if (target != null) {
			CmdAttackTarget(target.transform.parent.GetComponent<NetworkIdentity>().netId);
		} else {
			CmdAttackPoint(mousePosition);
		}
	}

	IEnumerator StartScan(NetworkInstanceId npcNetId) {
		yield return new WaitForSeconds(SCAN_TIME);
		CmdScanTarget(npcNetId);
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
