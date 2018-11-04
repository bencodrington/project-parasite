using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Hunter : Character {

	// The time it takes to fully scan an NPC
	const float SCAN_TIME = 0.75f;
	const float DETONATION_RADIUS = 1.5f;

	public GameObject detonationPrefab;

	protected override void HandleInput()  {
		Vector2 mousePosition;
		// Movement
		HandleHorizontalMovement();
		// Attack
		if (Input.GetMouseButtonDown(0)) {
			mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			Collider2D target = Physics2D.OverlapPoint(mousePosition, characterLayerMask);
			if (target != null) {
				CmdAttackTarget(target.transform.parent.GetComponent<NetworkIdentity>().netId);
			} else {
				CmdAttackPoint(mousePosition);
			}
		}
		// Scan NPC
		if (Input.GetMouseButtonDown(1)) {
			Collider2D npc = Physics2D.OverlapPoint(Camera.main.ScreenToWorldPoint(Input.mousePosition), npcLayerMask);
			if (npc != null) {
				StartCoroutine(StartScan(npc.transform.parent.GetComponent<NetworkIdentity>().netId));
			}
		}
	}

	IEnumerator StartScan(NetworkInstanceId npcNetId) {
		yield return new WaitForSeconds(SCAN_TIME);
		CmdScanTarget(npcNetId);
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

	// ClientRpc

	[ClientRpc]
	void RpcSpawnDetonation(Vector3 detonationPosition) {
		Instantiate(detonationPrefab, detonationPosition, Quaternion.identity);
	}
}
