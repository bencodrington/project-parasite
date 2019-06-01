using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Photon.Pun;

public class Hunter : Character {

	#region [Public Variables]

	public AudioClip cantPlaceOrbSound;
	public AudioClip placeOrbSound;
	public GameObject orbPrefab;
	public GameObject orbBeamPrefab;
	public GameObject orbUiManagerPrefab;
	public bool isNpcControlled = false;
	
	#endregion

	#region [Private Variables]

	private float jumpVelocity = 15f;
	// The maximum number of orbs that this hunter can have spawned at any given time 
	const int MAX_ORB_COUNT = 4;

	// The size of the box around newly-placed orbs, inside of which
	// 	NPCs will be alerted to run away
	Vector2 NPC_ALERT_RANGE = new Vector2(12, 4);

	OrbUiManager orbUiManager;
	OrbBeamRangeManager orbBeamRangeManager;
	AudioSource cantPlaceOrbAudioSource;
	AudioSource placeOrbAudioSource;

	Queue<Orb> orbs;

	#endregion

	protected override void OnStart() {
		orbs = new Queue<Orb>();
		// Cache reference to orb beam range manager
		orbBeamRangeManager = GetComponentInChildren<OrbBeamRangeManager>();
		if (!isNpcControlled && HasAuthority()) {
			// Spawn orb UI manager to display how many orbs are remaining
			orbUiManager = Instantiate(orbUiManagerPrefab,
										Vector3.zero,
										Quaternion.identity,
										// Anchor it to the canvas
										UiManager.Instance.GetCanvas()
			).GetComponent<OrbUiManager>();
			// Initialize it with the maximum orbs to spawn
			orbUiManager.setMaxOrbCount(MAX_ORB_COUNT);

			cantPlaceOrbAudioSource = Utility.AddAudioSource(gameObject, cantPlaceOrbSound);

		} else {
			orbBeamRangeManager.shouldShowMarkers = false;
		}
		
		GetComponentInChildren<SpriteTransform>().SetTargetTransform(transform);
		placeOrbAudioSource = Utility.AddAudioSource(gameObject, placeOrbSound);
	}

	protected override void HandleInput()  {
		input.UpdateInputState();
		if (orbBeamRangeManager.mostRecentOrb != null) {
			// Debug.DrawLine(mostRecentOrb.transform.position, transform.position, Color.cyan);
			// Debug.DrawLine(mostRecentOrb.transform.position, (Vector2)mostRecentOrb.transform.position + new Vector2(0, 6f), Color.black);
		}

		// Movement
		HandleHorizontalMovement();

		// If up was pressed this frame for the first time and the player is on the ground
		if ((input.isJustPressed(PlayerInput.Key.up) || input.isJustPressed(PlayerInput.Key.jump))
					&& physicsEntity.IsOnGround()) {
			// Jump
			photonView.RPC("RpcJump", RpcTarget.All);
		}

		if (input.isJustPressed(PlayerInput.Key.interact)) {
			InteractWithObjectsInRange();
		}

		// Place orb
		if (input.isJustPressed(PlayerInput.Key.action1)) {
			// CLEANUP: this can be cleaner, once InputManager is implemented
			// Don't spawn orb if clicking elevator button
			if (Physics2D.OverlapPoint(input.getMousePosition(), Utility.GetLayerMask("clickable")) == null) {
				AttemptToSpawnOrb(input.getMousePosition());
			}
		}
		// Recall orb
		if (input.isJustPressed(PlayerInput.Key.action2)) {
			AttemptToRecallOrb();
		}
	}

	#region [Public Methods]
	
	public void Repel(Vector2 forceDirection, float force) {
		// Distribute the force between the x and y coordinates
		forceDirection.Normalize();
		forceDirection *= force;
		// Transfer this force to the physics entity to handle it
		physicsEntity.AddVelocity(forceDirection.x, forceDirection.y);
	}
	
	#endregion

	protected override void OnCharacterDestroy() {
		DestroyAllOrbs();
		if (!isNpcControlled && HasAuthority()) {
			Destroy(orbUiManager.gameObject);
		}
	}

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
		orbUiManager.ShowRecallAlert();
		cantPlaceOrbAudioSource.Play();
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
		if (!isNpcControlled && HasAuthority()) {
			// Update the number of remaining orbs currently displayed onscreen
			orbUiManager.OnOrbCountChange(orbs.Count);
			// User can definitely place at least one orb, so show markers
			orbBeamRangeManager.shouldShowMarkers = true;
		}
	}

	void Jump() {
		physicsEntity.AddVelocity(0, jumpVelocity);
	}
	
	#endregion

	[PunRPC]
	void RpcSpawnOrb(Vector2 atPosition) {
		Vector2 beamSpawnPosition;
		// Create orb game object
		GameObject orbGameObject = Instantiate(orbPrefab, atPosition, Quaternion.identity);
		Orb orb = orbGameObject.GetComponent<Orb>();
		placeOrbAudioSource.Play();
		FindObjectOfType<CameraFollow>().ShakeScreen(0.1f, 0.1f);

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
		if (!isNpcControlled && HasAuthority()) {
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

	[PunRPC]
	void RpcJump() {
		Jump();
	}
}
