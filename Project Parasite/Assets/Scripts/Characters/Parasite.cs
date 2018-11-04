using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Parasite : PlayerCharacter {

	private float jumpVelocity = .5f;

	protected override void HandleInput()  {
		// Movement
		// TODO: add possibility for being moved outside of input
		bool right = Input.GetKey(KeyCode.D);
		bool left = Input.GetKey(KeyCode.A);
		if (right && !left) {
			physicsEntity.velocityX = movementSpeed;
		} else if (left && !right) {
			physicsEntity.velocityX = -movementSpeed;
		} else {
			physicsEntity.velocityX = 0;
		}

		// Jump
		if (Input.GetKeyDown(KeyCode.W) && physicsEntity.IsOnGround()) {
			physicsEntity.AddVelocity(0, jumpVelocity);
		}

		// Infect
		if (Input.GetMouseButtonDown(0)) {
			// TODO: restrict to layer mask
			Collider2D npc = Physics2D.OverlapPoint(Camera.main.ScreenToWorldPoint(Input.mousePosition));
			if (npc != null) {
				CmdInfectNpc(npc.transform.parent.GetComponent<NetworkIdentity>().netId);
				CmdDestroyParasite();
			}
		}
	}

	public override void ImportStats() {
		// TODO: get stats like this from imported files
		height = .25f;
		width = .5f;
		movementSpeed = .2f;
		type = "PARASITE";
	}

	// COMMANDS

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
		npc.playerObject = playerObject;
		// Give Parasite player authority over the NPC
		networkIdentity.AssignClientAuthority(playerObject.connectionToClient);
		// TODO: transfer velocity from current physics entity?
		npc.RpcGeneratePhysicsEntity(Vector2.zero);
		// TODO: delete physics entity off the server for performance?
		// Set isInfected to true/update sprite on new authority's client
		npc.RpcInfect();
		npc.RpcSetCameraFollow();
		// TODO: will likely need to remove the npc from the npcManager's list to have a proper count of uninfected NPCs
	}
}
