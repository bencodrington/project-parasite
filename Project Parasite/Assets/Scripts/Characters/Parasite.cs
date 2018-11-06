using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Parasite : Character {

	private float jumpVelocity = .5f;
	// Whether the "UP" key was being pressed last frame
	private bool oldUp = false;

	protected override void HandleInput()  {
		// Movement
		// TODO: add possibility for being moved outside of input
		bool right = Input.GetKey(KeyCode.D);
		bool left = Input.GetKey(KeyCode.A);
		if (right && !left) {
			physicsEntity.applyGravity = !physicsEntity.IsOnRightWall();
			physicsEntity.velocityX = stats.movementSpeed;
		} else if (left && !right) {
			physicsEntity.applyGravity = !physicsEntity.IsOnLeftWall();
			physicsEntity.velocityX = -stats.movementSpeed;
		} else {
			physicsEntity.applyGravity = true;
			physicsEntity.velocityX = 0;
		}

		if (physicsEntity.IsOnLeftWall() || physicsEntity.IsOnRightWall()) {
			// Don't maintain momentum from previous movement
			physicsEntity.velocityY = 0;
		}

		bool up = Input.GetKey(KeyCode.W);
		bool down = Input.GetKey(KeyCode.S);
		if (up && !oldUp && physicsEntity.IsOnGround()) {
			// Jump
			physicsEntity.AddVelocity(0, jumpVelocity);
		} else if (up && physicsEntity.IsOnCeiling()) {
			// Stick to ceiling
			physicsEntity.applyGravity = false;
		} else if (up && isOnWall()) {
			// Climb Up
			physicsEntity.velocityY = stats.movementSpeed;
		}
		if (down && isOnWall()) {
			// Climb Down
			physicsEntity.velocityY = -stats.movementSpeed;
		}
		oldUp = up;
		


		// Infect
		if (Input.GetMouseButtonDown(0)) {
			Collider2D npc = Physics2D.OverlapPoint(Camera.main.ScreenToWorldPoint(Input.mousePosition), npcLayerMask);
			if (npc != null) {
				CmdInfectNpc(npc.transform.parent.GetComponent<NetworkIdentity>().netId);
				CmdDestroyParasite();
			}
		}
	}

	bool isOnWall() {
		return physicsEntity.IsOnLeftWall() || physicsEntity.IsOnRightWall();
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
		// Delete current physics entity off the server for performance
		npc.CmdDeletePhysicsEntity();
		// TODO: transfer velocity from current physics entity?
		npc.RpcGeneratePhysicsEntity(Vector2.zero);
		// Set isInfected to true/update sprite on new authority's client
		npc.RpcInfect();
		// Update client's camera and render settings to reflect new character
		npc.RpcSetCameraFollow();
		npc.RpcSetRenderLayer();
	}
}
