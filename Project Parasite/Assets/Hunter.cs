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
			Collider2D npc = Physics2D.OverlapPoint(Camera.main.ScreenToWorldPoint(Input.mousePosition));
			if (npc != null) {
				CmdAttackNpc(npc.transform.parent.GetComponent<NetworkIdentity>().netId);
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
	void CmdAttackNpc(NetworkInstanceId npcNetId) {
		GameObject npcGameObject = NetworkServer.FindLocalObject(npcNetId);
		NonPlayerCharacter npc = npcGameObject.GetComponentInChildren<NonPlayerCharacter>();
		if (!npc.isInfected) {
			// Instant kill npcs
			FindObjectOfType<NpcManager>().DespawnNpc(npcNetId);
		}
	}
}
