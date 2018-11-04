using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Hunter : Character {

	// The time it takes to fully scan an NPC
	const float SCAN_TIME = 0.75f;

	protected override void HandleInput()  {
		// Movement
		HandleHorizontalMovement();
		// Attack
		if (Input.GetMouseButtonDown(0)) {
			Collider2D target = Physics2D.OverlapPoint(Camera.main.ScreenToWorldPoint(Input.mousePosition), characterLayerMask);
			if (target != null) {
				CmdAttackTarget(target.transform.parent.GetComponent<NetworkIdentity>().netId);
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
	
	public override void ImportStats() {
		height = .5f;
		width = .5f;
		movementSpeed = .07f;
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
		// Get PlayerCharacter script
		Character targetPlayerCharacter = targetGameObject.GetComponentInChildren<Character>();
		bool isNpc = targetPlayerCharacter is NonPlayerCharacter;
		// Inflict damage
		if (!isNpc || (isNpc && ((NonPlayerCharacter)targetPlayerCharacter).isInfected)) {
			// Damage parasite
			targetPlayerCharacter.playerObject.RpcTakeDamage(25);
		} else if (isNpc) {
			// Instant kill npcs
			FindObjectOfType<NpcManager>().DespawnNpc(targetNetId);
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
}
