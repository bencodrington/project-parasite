using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Parasite : PlayerCharacter {

	private float jumpVelocity = .25f;

	protected override void HandleInput()  {
		// Movement
		float movementX = Input.GetAxis("Horizontal") * movementSpeed * Time.deltaTime;

		// Jump
		if (Input.GetKeyDown(KeyCode.W) && physicsEntity.IsOnGround()) {
			physicsEntity.AddVelocity(0, jumpVelocity);
		}

		// Infect
		if (Input.GetMouseButtonDown(0)) {
			// TODO: restrict to layer mask
			Collider2D npc = Physics2D.OverlapPoint(Camera.main.ScreenToWorldPoint(Input.mousePosition));
			if (npc != null) {
				CmdDestroyNpc(npc.transform.parent.GetComponent<NetworkIdentity>().netId);
			}
		}

		// Has authority, so translate immediately
		transform.Translate(movementX, 0, 0);
	}

	public override void ImportStats() {
		// TODO: get stats like this from imported files
		height = .25f;
		movementSpeed = 15f;
		type = "PARASITE";
	}

	// COMMANDS

	[Command]
	void CmdDestroyNpc(NetworkInstanceId npc) {
		FindObjectOfType<NpcManager>().DespawnNpc(npc);
	}
}
