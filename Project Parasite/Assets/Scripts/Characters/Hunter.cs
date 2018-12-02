using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;

public class Hunter : Character {

	private float jumpVelocity = 30f;

	// Used when getting user input to determine if key was down last frame
	private bool oldUp = false;

	public GameObject orbPrefab;

	protected override void HandleInput()  {
		// Movement
		HandleHorizontalMovement();

		bool up = Input.GetKey(KeyCode.W);
		// If up was pressed this frame for the first time and the player is on the ground
		if (up && !oldUp && physicsEntity.IsOnGround()) {
			// Jump
			physicsEntity.AddVelocity(0, jumpVelocity);
		}
		oldUp = up;

		// Place orb
		if (Input.GetKeyDown(KeyCode.J)) {
			// TODO: to decrease lag, spawn locally and then let server know
			CmdSpawnOrb(transform.position);
		}
	}

	public void Repel(Vector2 origin, float force) {
		// Calculate vector from the repelling force's point of origin to this object's center
		Vector2 displacement = (Vector2)transform.position - origin;
		// Distribute the force between the x and y coordinates
		displacement.Normalize();
		displacement *= force;
		// Transfer this force to the physics entity to handle it
		physicsEntity.AddVelocity(displacement.x, displacement.y);
	}

	// Commands

	[Command]
	void CmdSpawnOrb(Vector2 atPosition) {
		// Create orb game object on the server
		GameObject orbGameObject = Instantiate(orbPrefab, atPosition, Quaternion.identity);
		// Propogate to all clients
		NetworkServer.Spawn(orbGameObject);
	}
}
