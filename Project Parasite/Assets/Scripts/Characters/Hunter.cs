using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;

public class Hunter : Character {

	private float jumpVelocity = 30f;
	const int MAX_ORB_COUNT = 3;

	// Used when getting user input to determine if key was down last frame
	private bool oldUp = false;

	public GameObject orbPrefab;

	Queue<Orb> orbs;

	protected override void OnStart() {
		if (isServer) {
			orbs = new Queue<Orb>();
		}
	}

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
			CmdSpawnOrb(transform.position);
		}
		// Recall orb
		if (Input.GetKeyDown(KeyCode.K)) {
			CmdRecallOrb();
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
		if (orbs.Count >= MAX_ORB_COUNT) { return; }
		// Create orb game object on the server
		GameObject orbGameObject = Instantiate(orbPrefab, atPosition, Quaternion.identity);
		// Add to queue
		orbs.Enqueue(orbGameObject.GetComponent<Orb>());
		// Propogate to all clients
		NetworkServer.Spawn(orbGameObject);
	}

	[Command]
	void CmdRecallOrb() {
		if (orbs.Count <= 0) { return; }
		NetworkServer.Destroy(orbs.Dequeue().gameObject);
	}
}
