using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Hunter : PlayerCharacter {

	protected override void HandleInput()  {
		// Movement
		float movementX = Input.GetAxis("Horizontal") * movementSpeed * Time.deltaTime;
		// Has authority, so translate immediately
		transform.Translate(movementX, 0, 0);

		// Attack
		if (Input.GetMouseButtonDown(0)) {
			// TODO: restrict to layer mask
			Collider2D target = Physics2D.OverlapPoint(Camera.main.ScreenToWorldPoint(Input.mousePosition));
			if (target != null) {
				CmdAttackTarget(target.transform.parent.GetComponent<NetworkIdentity>().netId);
			}
		}
	}
	
	public override void ImportStats() {
		height = .5f;
		movementSpeed = 10f;
		type = "HUNTER";
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
}
