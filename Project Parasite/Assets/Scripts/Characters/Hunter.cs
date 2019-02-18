using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
		orbs = new Queue<Orb>();
		// Cache reference to orb beam range manager
		orbBeamRangeManager = GetComponentInChildren<OrbBeamRangeManager>();
		if (HasAuthority()) {
			// Spawn orb UI manager to display how many orbs are remaining
			orbUiManager = Instantiate(orbUiManagerPrefab,
										Vector3.zero,
										Quaternion.identity,
										// Anchor it to the canvas
										UiManager.Instance.GetCanvas()
			).GetComponent<OrbUiManager>();
			// Initialize it with the maximum orbs to spawn
			orbUiManager.setMaxOrbCount(MAX_ORB_COUNT);
		} else {
			orbBeamRangeManager.shouldShowMarkers = false;
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
			InteractWithObjectsInRange();
		}

		// Place orb
		if (Input.GetMouseButtonDown(0)) {
			// CLEANUP: this can be cleaner, once InputManager is implemented
			// Don't spawn orb if clicking elevator button
			if (Physics2D.OverlapPoint(Utility.GetMousePos(), Utility.GetLayerMask("clickable")) == null) {
				AttemptToSpawnOrb(Utility.GetMousePos());
			}
		}
		// Recall orb
		if (Input.GetMouseButtonDown(1)) {
			AttemptToRecallOrb();
		}
		// De-activate suit
		isSuitActivated = !Input.GetKey(KeyCode.LeftShift);
		spriteRenderer.color = isSuitActivated ? SuitActivatedColour : SuitDeactivatedColour;
	}

	#region [Public Methods]
	
	public void Repel(Vector2 forceDirection, float force) {
		if (!isSuitActivated) return;
		// Distribute the force between the x and y coordinates
		forceDirection.Normalize();
		forceDirection *= force;
		// Transfer this force to the physics entity to handle it
		physicsEntity.AddVelocity(forceDirection.x, forceDirection.y);
	}
	
	#endregion

	#region [MonoBehaviour Callbacks]
	
	void OnDestroy() {
		DestroyAllOrbs();
		if (HasAuthority()) {
			Destroy(orbUiManager.gameObject);
		}
	}
	
	#endregion

	#region [Private Methods]
	
	void AttemptToSpawnOrb(Vector2 atPosition) {
		if (orbs.Count >= MAX_ORB_COUNT) { 
			OrbSpawnFailed();
			return; 
		}
		photonView.RPC("RpcSpawnOrb", RpcTarget.All, atPosition);
	}

	void OrbSpawnFailed() {
		orbUiManager.FlashPlaceholders();
	}

	void AttemptToRecallOrb() {
		if (orbs.Count <= 0) { return; }
		photonView.RPC("RpcRecallOrb", RpcTarget.All);
	}

	void DestroyAllOrbs() {
		while (orbs.Count > 0) {
			RecallOrb();
		}
	}

	void AlertNpcsInRange(Vector2 ofPosition) {
		// Find all NPCs in range
		Collider2D[] npcs = Physics2D.OverlapBoxAll(ofPosition, NPC_ALERT_RANGE, 0, Utility.GetLayerMask(CharacterType.NPC));
		NonPlayerCharacter npc;
		// Alert each NPC
		foreach (Collider2D npcCollider in npcs) {
			npc = npcCollider.transform.parent.gameObject.GetComponentInChildren<NonPlayerCharacter>();
			npc.NearbyOrbAlert(ofPosition);
		}
	}

	void RecallOrb() {
		Destroy(orbs.Dequeue().gameObject);
		if (HasAuthority()) {
			// Update the number of remaining orbs currently displayed onscreen
			orbUiManager.OnOrbCountChange(orbs.Count);
			// User can definitely place at least one orb, so show markers
			orbBeamRangeManager.shouldShowMarkers = true;
		}
	}
	
	#endregion

	[PunRPC]
	void RpcSpawnOrb(Vector2 atPosition) {
		Vector2 beamSpawnPosition;
		// Create orb game object
		GameObject orbGameObject = Instantiate(orbPrefab, atPosition, Quaternion.identity);
		Orb orb = orbGameObject.GetComponent<Orb>();

		AlertNpcsInRange(atPosition);

		// If new orb is within "beaming" range of most recently placed orb
		if (orbBeamRangeManager.isInRange(atPosition)) {
			// Spawn beam halfway between orbs
			beamSpawnPosition = Vector2.Lerp(orbBeamRangeManager.mostRecentOrb.transform.position, atPosition, 0.5f);
			OrbBeam orbBeam = Instantiate(orbBeamPrefab, beamSpawnPosition, Quaternion.identity).GetComponent<OrbBeam>();
			// Store beam in most recent orb so when the orb is destroyed it can take the beam with it
			orbBeamRangeManager.mostRecentOrb.AttachBeam(orbBeam);
			orbBeam.Initialize(orbBeamRangeManager.mostRecentOrb.transform.position, atPosition);
		}

		// Add to queue
		orbs.Enqueue(orb);
		// Update reference to most recent orb for displaying distance limit to player
		orbBeamRangeManager.mostRecentOrb = orb;
		if (HasAuthority()) {
			// Update the number of remaining orbs currently displayed onscreen
			orbUiManager.OnOrbCountChange(orbs.Count);
			// CLEANUP: this should probably be extracted to the rangemanager itself
			// Hide markers if the user can't place more orbs
			if (orbs.Count == MAX_ORB_COUNT) {
				orbBeamRangeManager.shouldShowMarkers = false;
			}

		}
	}

	[PunRPC]
	void RpcRecallOrb() {
		RecallOrb();
	}
}
