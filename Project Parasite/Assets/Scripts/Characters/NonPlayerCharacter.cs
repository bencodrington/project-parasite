using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class NonPlayerCharacter : Character {

	#region [Public Variables]
	
	public bool isInfected = false;
	
	// The exclamation mark that is shown when orbs are placed nearby
	public GameObject alertIconPrefab;

	#endregion

	#region [Private Variables]
	
	const float PARASITE_LAUNCH_VELOCITY = 20f;
	// The amount of time Action 2 must be held before the NPC can be burst
	//	upon ejecting
	const float MIN_BURST_TIME = 1f;

	// The range of distances that NPCs will randomly select from
	// 	when choosing a new point to travel to
	const float MAX_TARGET_DISTANCE = 5f;
	const float MIN_TARGET_DISTANCE = 2f;

	// The farthest that NPCs will try to move when running away
	const float FLEE_DISTANCE = 8f;

	BurstIndicator burstIndicator;

	// Pathfinding
	float validDistanceFromTarget = .5f;
	// Note that target is currently only used to move horizontally,
	//	and as a result is only the x coordinate of the target location
	float targetX;
	float minTimeUntilNewPath = 2f;
	float maxTimeUntilNewPath = 5f;
	bool hasTarget = false;

	// The amount of time Action 2 has been held down since it was pressed
	float timeChargingForBurst = 0f;
	bool isChargingForBurst = false;

	// How far from the npc's center to display the icon
	Vector2 ALERT_ICON_OFFSET = new Vector2(0, 1);
	
	#endregion

	protected override void HandleInput() {
		input.UpdateInputState();
		if (!isInfected) { return; } //TODO: should actually have a branch for uninfected, feed the input state into the AI
		// Movement
		if (isChargingForBurst) {
			isMovingLeft = false;
			isMovingRight = false;
		} else {
			// Only allow movement if we're not charging
			HandleHorizontalMovement();
		}

		// Self Destruct
		if (input.isJustPressed(InputSource.Key.action2)) {
			OnAction2Down();
		} else if (input.isJustReleased(InputSource.Key.action2)) {
			OnAction2Up();
		}

		if (input.isJustPressed(PlayerInput.Key.interact)) {
			InteractWithObjectsInRange();
		}
	}

	#region [Public Methods]

	public void OnGotFried() {
		if (isInfected) {
			BurstMeatSuit();
		} else {
			DespawnSelf();
		}
	}

	public void Infect(InputSource parasiteInputSource, bool shouldUpdateNpcAppearance = true) {
		SetInputSource(parasiteInputSource);
		isInfected = true;
		if (!shouldUpdateNpcAppearance) { return; }
		// Only update sprite if on the Parasite player's client
		SetSpriteRenderersColour(Color.magenta);
	}

	public void NearbyOrbAlert(Vector2 atPosition) {
		// Show exclamation mark above NPC
		GameObject alertIcon = Instantiate(alertIconPrefab, (Vector2)transform.position + ALERT_ICON_OFFSET, Quaternion.identity);
		alertIcon.transform.SetParent(transform);
		if (!HasAuthority() || isInfected) { return; }
		// Only uninfected NPCs should flee, and the calculations
		// 	should only be done on the server
		Utility.Directions fleeDirection = atPosition.x < transform.position.x ?
			Utility.Directions.Right :
			Utility.Directions.Left;
		// TODO:
		// FleeOrbInDirection(fleeDirection);
	}
	
	#endregion
	
	protected override void OnStart() {
		burstIndicator = GetComponentInChildren<BurstIndicator>();
		burstIndicator.SetTimeToFill(MIN_BURST_TIME);
	}

	#region [MonoBehaviour Callbacks]
	
	public override void Update() {
		if (HasAuthority()) {
			HandleInput();
			HandlePositionAndInputUpdates();
			HandleBurstCharging();
		}
	}
	
	#endregion

	public override bool IsUninfectedNpc() {
		return !isInfected;
	}

	#region [Private Methods]

	void OnAction2Down() {
		timeChargingForBurst = 0f;
		isChargingForBurst = true;
		burstIndicator.StartFilling();
	}

	void OnAction2Up() {
		if (!isChargingForBurst) {
			// When Action 2 was pressed, it was used to infect this NPC
			// Require another press of Action 2 to eject/burst
			return;
		}
		isChargingForBurst = false;
		burstIndicator.StopFilling();
		if (timeChargingForBurst > MIN_BURST_TIME) {
			BurstMeatSuit();
		} else {
			EjectMeatSuit();
		}
	}

	void BurstMeatSuit() {
		DespawnSelf();
		SpawnParasite();
	}

	void EjectMeatSuit() {
		SpawnParasite();
		Uninfect();
	}

	void Uninfect() {
		isInfected = false;
		// Only update sprite if on the Parasite player's client
		SetSpriteRenderersColour(Color.white);
		// Return npc to the same render layer as the other NPCs
		SetRenderLayer("Characters");
		SetInputSource(new DefaultNpcInput());
	}

	void DespawnSelf() {
		// Send out an event to decrement counter
		EventCodes.RaiseEventAll(EventCodes.NpcDespawned, null);
		PhotonNetwork.Destroy(photonView);
	}

	void SpawnParasite() {
		Character parasite = CharacterSpawner.SpawnPlayerCharacter(CharacterType.Parasite, transform.position, new Vector2(0, PARASITE_LAUNCH_VELOCITY), false, false);
		parasite.SetInputSource(input);
	}

	void HandleBurstCharging() {
		if (isChargingForBurst) {
			timeChargingForBurst += Time.deltaTime;
		}
	}
	
	#endregion
}
