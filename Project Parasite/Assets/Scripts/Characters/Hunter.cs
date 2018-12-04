using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;

public class Hunter : Character {

	private float jumpVelocity = 30f;
	// The maximum number of orbs that this hunter can have spawned at any given time 
	const int MAX_ORB_COUNT = 4;
	// The maximum distance from the most recent orb
	const float ORB_BEAM_RANGE = 6f;

	// Used when getting user input to determine if key was down last frame
	private bool oldUp = false;

	bool isSuitActivated = true;

	Color SuitDeactivatedColour = Color.blue;

	public GameObject orbPrefab;
	public GameObject orbBeamPrefab;

	Queue<Orb> orbs;
	Orb mostRecentOrb;

	protected override void OnStart() {
		if (isServer) {
			orbs = new Queue<Orb>();
		}
	}

	protected override void HandleInput()  {

		if (mostRecentOrb != null) {
			// Debug.DrawLine(mostRecentOrb.transform.position, transform.position, Color.cyan);
			// Debug.DrawLine(mostRecentOrb.transform.position, (Vector2)mostRecentOrb.transform.position + new Vector2(0, 6f), Color.black);
		}

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
		// De-activate suit
		isSuitActivated = !Input.GetKey(KeyCode.LeftShift);
		spriteRenderer.color = isSuitActivated ? Color.white : SuitDeactivatedColour;
	}

	public void Repel(Vector2 forceDirection, float force) {
		if (!isSuitActivated) return;
		// Distribute the force between the x and y coordinates
		forceDirection.Normalize();
		forceDirection *= force;
		// Transfer this force to the physics entity to handle it
		physicsEntity.AddVelocity(forceDirection.x, forceDirection.y);
	}

	bool MostRecentOrbInRange(Vector2 ofPosition) {
		return mostRecentOrb != null &&
				(Vector2.Distance(mostRecentOrb.transform.position, ofPosition) <= ORB_BEAM_RANGE);
	}

	// Commands

	[Command]
	void CmdSpawnOrb(Vector2 atPosition) {
		if (orbs.Count >= MAX_ORB_COUNT) { return; }
		Vector2 beamSpawnPosition;
		// Create orb game object on the server
		GameObject orbGameObject = Instantiate(orbPrefab, atPosition, Quaternion.identity);
		Orb orb = orbGameObject.GetComponent<Orb>();

		if (MostRecentOrbInRange(atPosition)) {
			// Spawn beam halfway between orbs
			beamSpawnPosition = Vector2.Lerp(mostRecentOrb.transform.position, atPosition, 0.5f);
			OrbBeam orbBeam = Instantiate(orbBeamPrefab, beamSpawnPosition, Quaternion.identity).GetComponent<OrbBeam>();
			// Store beam in most recent orb so when the orb is destroyed it can take the beam with it
			mostRecentOrb.AttachBeam(orbBeam);
			// Propogate to all clients
			NetworkServer.Spawn(orbBeam.gameObject);
			orbBeam.RpcInitialize(mostRecentOrb.transform.position, atPosition);
		}

		// Add to queue
		orbs.Enqueue(orb);
		// Propogate to all clients
		NetworkServer.Spawn(orbGameObject);
		RpcOnOrbSpawned(orb.netId);
		mostRecentOrb = orb;
	}

	[Command]
	void CmdRecallOrb() {
		if (orbs.Count <= 0) { return; }
		NetworkServer.Destroy(orbs.Dequeue().gameObject);
	}

	// ClientRpc

	[ClientRpc]
	void RpcOnOrbSpawned(NetworkInstanceId orbNetId) {
		if (hasAuthority) {
			// This client spawned the orb
			mostRecentOrb = ClientScene.FindLocalObject(orbNetId).GetComponent<Orb>();
		}
	}
}
