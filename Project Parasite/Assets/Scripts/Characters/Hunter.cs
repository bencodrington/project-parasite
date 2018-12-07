using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;

public class Hunter : Character {

	private float jumpVelocity = 30f;
	// The maximum number of orbs that this hunter can have spawned at any given time 
	const int MAX_ORB_COUNT = 4;

	// Used when getting user input to determine if key was down last frame
	private bool oldUp = false;

	bool isSuitActivated = true;

	Color SuitDeactivatedColour = Color.blue;

	public GameObject orbPrefab;
	public GameObject orbBeamPrefab;
	public GameObject orbUiManagerPrefab;
	OrbUiManager orbUiManager;
	OrbBeamRangeManager orbBeamRangeManager;

	Queue<Orb> orbs;

	protected override void OnStart() {
		if (isServer) {
			orbs = new Queue<Orb>();
			PlayerObject.RegisterOnCharacterDestroyCallback(DestroyAllOrbs);
		}
		if (hasAuthority) {
			// Spawn orb UI manager to display how many orbs are remaining
			orbUiManager = Instantiate(orbUiManagerPrefab).GetComponent<OrbUiManager>();
			// Anchor it to the bottom right corner
			orbUiManager.transform.SetParent(FindObjectOfType<Canvas>().transform);
			// Initialize it with the maximum orbs to spawn
			orbUiManager.setMaxOrbCount(MAX_ORB_COUNT);
			// Cache reference to orb beam range manager
			orbBeamRangeManager = GetComponentInChildren<OrbBeamRangeManager>();
		} else {
			Destroy(GetComponent<OrbBeamRangeManager>().gameObject);
		}
	}

	protected override void HandleInput()  {

		if (orbBeamRangeManager.mostRecentOrb != null) {
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

	void DestroyAllOrbs() {
		while (orbs.Count > 0) {
			CmdRecallOrb();
		}
	}

	protected override void OnCharacterDestroy() {
		Destroy(orbUiManager.gameObject);
	}

	// Commands

	[Command]
	void CmdSpawnOrb(Vector2 atPosition) {
		if (orbs.Count >= MAX_ORB_COUNT) { return; }
		Vector2 beamSpawnPosition;
		// Create orb game object on the server
		GameObject orbGameObject = Instantiate(orbPrefab, atPosition, Quaternion.identity);
		Orb orb = orbGameObject.GetComponent<Orb>();

		if (orbBeamRangeManager.isInRange(atPosition)) {
			// Spawn beam halfway between orbs
			beamSpawnPosition = Vector2.Lerp(orbBeamRangeManager.mostRecentOrb.transform.position, atPosition, 0.5f);
			OrbBeam orbBeam = Instantiate(orbBeamPrefab, beamSpawnPosition, Quaternion.identity).GetComponent<OrbBeam>();
			// Store beam in most recent orb so when the orb is destroyed it can take the beam with it
			orbBeamRangeManager.mostRecentOrb.AttachBeam(orbBeam);
			// Propogate to all clients
			NetworkServer.Spawn(orbBeam.gameObject);
			orbBeam.RpcInitialize(orbBeamRangeManager.mostRecentOrb.transform.position, atPosition);
		}

		// Add to queue
		orbs.Enqueue(orb);
		// Propogate to all clients
		NetworkServer.Spawn(orbGameObject);
		RpcOnOrbSpawned(orb.netId, orbs.Count);
		orbBeamRangeManager.mostRecentOrb = orb;
	}

	[Command]
	void CmdRecallOrb() {
		if (orbs.Count <= 0) { return; }
		NetworkServer.Destroy(orbs.Dequeue().gameObject);
		RpcOnOrbRecalled(orbs.Count);
	}

	// ClientRpc

	[ClientRpc]
	void RpcOnOrbSpawned(NetworkInstanceId orbNetId, int newOrbCount) {
		if (hasAuthority) {
			// This client spawned the orb
			// Update reference to most recent orb for displaying distance limit to player
			orbBeamRangeManager.mostRecentOrb = ClientScene.FindLocalObject(orbNetId).GetComponent<Orb>();
			// Update the number of remaining orbs currently displayed onscreen
			orbUiManager.OnOrbCountChange(newOrbCount);
			// Hide markers if the user can't place more orbs
			if (newOrbCount == MAX_ORB_COUNT) {
				orbBeamRangeManager.shouldShowMarkers = false;
			}
		}
	}

	[ClientRpc]
	void RpcOnOrbRecalled(int newOrbCount) {
		if (hasAuthority) {
			// Update the number of remaining orbs currently displayed onscreen
			orbUiManager.OnOrbCountChange(newOrbCount);
			// User can definitely place at least one orb, so show markers
			orbBeamRangeManager.shouldShowMarkers = true;
		}
	}
}
