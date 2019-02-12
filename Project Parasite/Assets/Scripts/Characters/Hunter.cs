﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;
using Photon.Pun;

public class Hunter : Character {

	private float jumpVelocity = 30f;
	// The maximum number of orbs that this hunter can have spawned at any given time 
	const int MAX_ORB_COUNT = 4;

	// The size of the box around newly-placed orbs, inside of which
	// 	NPCs will be alerted to run away
	Vector2 NPC_ALERT_RANGE = new Vector2(6, 4);

	// Used when getting user input to determine if key was down last frame
	private bool oldUp = false;

	bool isSuitActivated = true;

	Color SuitActivatedColour = new Color(0, 1f, 1f, 1);
	Color SuitDeactivatedColour = new Color(0, .5f, 0.6f, 1);

	public GameObject orbPrefab;
	public GameObject orbBeamPrefab;
	public GameObject orbUiManagerPrefab;
	OrbUiManager orbUiManager;
	OrbBeamRangeManager orbBeamRangeManager;

	Queue<Orb> orbs;

	protected override void OnStart() {
		if (HasAuthority()) {
			orbs = new Queue<Orb>();
			PlayerObject.RegisterOnCharacterDestroyCallback(DestroyAllOrbs);
			// Spawn orb UI manager to display how many orbs are remaining
			orbUiManager = Instantiate(orbUiManagerPrefab).GetComponent<OrbUiManager>();
			// Anchor it to the bottom right corner
			orbUiManager.transform.SetParent(FindObjectOfType<Canvas>().transform);
			// Initialize it with the maximum orbs to spawn
			orbUiManager.setMaxOrbCount(MAX_ORB_COUNT);
			// Cache reference to orb beam range manager
			orbBeamRangeManager = GetComponentInChildren<OrbBeamRangeManager>();
		} else {
			Destroy(GetComponentInChildren<OrbBeamRangeManager>().gameObject);
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

		if (Input.GetKeyDown(KeyCode.E)) {
			// TODO:
			// CmdInteractWithObjectsInRange();
		}

		// Place orb
		if (Input.GetMouseButtonDown(0)) {
			// TODO: this can be cleaner, once InputManager is implemented
			// Don't spawn orb if clicking elevator button
			if (Physics2D.OverlapPoint(Utility.GetMousePos(), Utility.GetLayerMask("clickable")) == null) {
				CmdSpawnOrb(Utility.GetMousePos());
			}
		}
		// Recall orb
		if (Input.GetMouseButtonDown(1)) {
			CmdRecallOrb();
		}
		// De-activate suit
		isSuitActivated = !Input.GetKey(KeyCode.LeftShift);
		spriteRenderer.color = isSuitActivated ? SuitActivatedColour : SuitDeactivatedColour;
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
		if (HasAuthority()) {
			Destroy(orbUiManager.gameObject);
		}
	}

	// Commands

	void CmdSpawnOrb(Vector2 atPosition) {
		if (orbs.Count >= MAX_ORB_COUNT) { 
			RpcOrbSpawnFailed();
			return; 
		}
		Vector2 beamSpawnPosition;
		// Create orb game object on the server
		GameObject orbGameObject = Instantiate(orbPrefab, atPosition, Quaternion.identity);
		Orb orb = orbGameObject.GetComponent<Orb>();

		CmdAlertNpcsInRange(atPosition);

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

	void CmdRecallOrb() {
		if (orbs.Count <= 0) { return; }
		NetworkServer.Destroy(orbs.Dequeue().gameObject);
		RpcOnOrbRecalled(orbs.Count);
	}

	void CmdAlertNpcsInRange(Vector2 ofPosition) {
		// Find all NPCs in range
		Collider2D[] npcs = Physics2D.OverlapBoxAll(ofPosition, NPC_ALERT_RANGE, 0, Utility.GetLayerMask(CharacterType.NPC));
		NonPlayerCharacter npc;
		// Alert each NPC
		foreach (Collider2D npcCollider in npcs) {
			npc = npcCollider.transform.parent.gameObject.GetComponentInChildren<NonPlayerCharacter>();
			npc.RpcNearbyOrbAlert(ofPosition);
		}
	}

	// ClientRpc

	void RpcOnOrbSpawned(NetworkInstanceId orbNetId, int newOrbCount) {
		if (HasAuthority()) {
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

	void RpcOnOrbRecalled(int newOrbCount) {
		if (HasAuthority()) {
			// Update the number of remaining orbs currently displayed onscreen
			orbUiManager.OnOrbCountChange(newOrbCount);
			// User can definitely place at least one orb, so show markers
			orbBeamRangeManager.shouldShowMarkers = true;
		}
	}

	void RpcOrbSpawnFailed() {
		if (HasAuthority()) {
			orbUiManager.FlashPlaceholders();
		}
	}
}
