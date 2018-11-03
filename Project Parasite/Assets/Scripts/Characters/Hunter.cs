using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Hunter : PlayerCharacter {

	// The time it takes to fully scan an NPC
	const float SCAN_TIME = 0.75f;

	protected override void HandleInput()  {
		// Movement
		float movementX = Input.GetAxis("Horizontal") * movementSpeed * Time.deltaTime;
		// Has authority, so translate immediately
		transform.Translate(movementX, 0, 0);

		// TODO: optimize so this doesn't run every input cycle
		int layerMask = 1 << LayerMask.NameToLayer("Characters");

		// Attack
		if (Input.GetMouseButtonDown(0)) {
			Collider2D target = Physics2D.OverlapPoint(Camera.main.ScreenToWorldPoint(Input.mousePosition), layerMask);
			if (target != null) {
				CmdAttackTarget(target.transform.parent.GetComponent<NetworkIdentity>().netId);
			}
		}

		// Scan
		if (Input.GetMouseButtonDown(1)) {
			// TODO: restrict layermask to NPCs only?
			Collider2D target = Physics2D.OverlapPoint(Camera.main.ScreenToWorldPoint(Input.mousePosition), layerMask);
			if (target != null) {
				StartCoroutine(StartScan(target.transform.parent.GetComponent<NetworkIdentity>().netId));
			}
		}
	}
	
	public override void ImportStats() {
		height = .5f;
		width = .5f;
		movementSpeed = 10f;
		type = "HUNTER";
	}

	IEnumerator StartScan(NetworkInstanceId targetNetId) {
		float scanTime = SCAN_TIME;
		while (scanTime > 0) {
			scanTime -= Time.deltaTime;
			yield return null;
		}
		CmdScanTarget(targetNetId);
	}

	// Commands

	[Command]
	void CmdAttackTarget(NetworkInstanceId targetNetId) {
		GameObject targetGameObject = NetworkServer.FindLocalObject(targetNetId);
		PlayerCharacter npc = targetGameObject.GetComponentInChildren<PlayerCharacter>();
		bool isNpc = npc is NonPlayerCharacter;
		if (!isNpc || (isNpc && ((NonPlayerCharacter)npc).isInfected)) {
			// Damage parasite
			npc.playerObject.RpcTakeDamage(25);
		} else if (isNpc) {
			// Instant kill npcs
			FindObjectOfType<NpcManager>().DespawnNpc(targetNetId);
		}
	}

	[Command]
	void CmdScanTarget(NetworkInstanceId targetNetId) {
		GameObject targetGameObject = NetworkServer.FindLocalObject(targetNetId);
		PlayerCharacter npc = targetGameObject.GetComponentInChildren<PlayerCharacter>();
		bool isNpc = npc is NonPlayerCharacter;
		if (!isNpc) {
			return;
		} else if (isNpc) {
			((NonPlayerCharacter)npc).RpcVerify();
		}
	}
}
