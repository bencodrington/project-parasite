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

	// Pathfinding
	float validDistanceFromTarget = .5f;
	// Note that target is currently only used to move horizontally,
	//	and as a result is only the x coordinate of the target location
	float targetX;
	float minTimeUntilNewPath = 2f;
	float maxTimeUntilNewPath = 5f;
	bool hasTarget = false;
	float maxTargetDistance = 5f;
	float minTargetDistance = 2f;

	// The amount of time Action 2 has been held down since it was pressed
	float timeChargingForBurst = 0f;
	bool isChargingForBurst = false;

	// Whether or not right click was being pressed last frame
	bool oldAction2;

	// How far from the npc's center to display the icon
	Vector2 ALERT_ICON_OFFSET = new Vector2(0, 1);
	
	#endregion

	protected override void HandleInput() {
		// This function is only called when this NPC is infected,
		// 	and is only called on the Parasite player's client
		// Movement
		if (isChargingForBurst) {
			isMovingLeft = false;
			isMovingRight = false;
		} else {
			// Only allow movement if we're not charging
			HandleHorizontalMovement();
		}

		// Self Destruct
		bool action2 = Input.GetMouseButton(1);
		if (action2 && !oldAction2) {
			OnAction2Down();
		} else if (oldAction2 && !action2) {
			OnAction2Up();
		}
		oldAction2 = action2;

		if (Input.GetKeyDown(KeyCode.E)) {
			InteractWithObjectsInRange();
		}
	}

	#region [Public Methods]

	public IEnumerator Idle() {
		yield return new WaitForSeconds(Random.Range(minTimeUntilNewPath, maxTimeUntilNewPath));
		// Check that we are still uninfected and still exist
		if (this != null && !isInfected) { FindNewPath(); }
		
	}

	public void OnGotFried() {
		if (isInfected) {
			BurstMeatSuit();
		} else {
			DespawnSelf();
		}
	}

	public void Infect() {
		isInfected = true;
		// Right click was pressed last frame on the parasite player's client
		oldAction2 = true;
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
		FleeOrbInDirection(fleeDirection);
	}
	
	#endregion

	#region [MonoBehaviour Callbacks]
	
	public override void Update() {
		if (isInfected && HasAuthority()) {
			// NPC is infected and this client is the Parasite player's client
			HandleInput();
			HandlePositionAndInputUpdates();
			HandleBurstCharging();
		} else if (!isInfected && HasAuthority()) {
			// NPC still belongs to the server
			TraversePath();
			HandlePositionAndInputUpdates();
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
	}

	void OnAction2Up() {
		if (!isChargingForBurst) {
			// When Action 2 was pressed, it was used to infect this NPC
			// Require another press of Action 2 to eject/burst
			return;
		}
		isChargingForBurst = false;
		if (timeChargingForBurst > MIN_BURST_TIME) {
			BurstMeatSuit();
		} else {
			EjectMeatSuit();
		}
	}

	void TraversePath() {
		isMovingLeft = false;
		isMovingRight = false;
		if (!hasTarget) { return; }
		if (Mathf.Abs(this.transform.position.x - targetX) < validDistanceFromTarget) {
			// Reached target
			StartCoroutine(Idle());
			// Stop traversing path
			hasTarget = false;
		} else {
			// Still moving
			if (targetX >= transform.position.x) {
				isMovingRight = true;
			} else {
				isMovingLeft = true;
			}
		}
	}

	void FindNewPath() {
		// Randomly select offset that is +/-[minTargetDistance, maxTargetDistance]
		float rangeDifference = maxTargetDistance - minTargetDistance;
		float offset = Random.Range(-rangeDifference, rangeDifference);
		offset += (offset >= 0) ? minTargetDistance : -minTargetDistance;
		// Set target relative to current location
		targetX = transform.position.x + offset;
		// If there is a wall/ beam in the way, don't move to new target
		targetX = ModifyTargetToAvoidObstacles(targetX);
		// Begin traversing
		hasTarget = true;
	}

	float ModifyTargetToAvoidObstacles(float target) {
		// TODO: fix following line
		Vector2 pathHitboxSize = new Vector2(target - transform.position.x, spriteRenderers[0].transform.localScale.y);
		// TODO: the below can cause npcs to walk into beams at head height,
		//  but also stops the hitbox from being triggered by the floor
		pathHitboxSize.y -= 0.1f;
		// Calculate corners of hitbox
		Vector2 pathHitboxTopStart = new Vector2(transform.position.x, transform.position.y + pathHitboxSize.y / 2);
		Vector2 pathHitboxBottomEnd = new Vector2(target, transform.position.y - pathHitboxSize.y / 2);
		bool isObstacleInTheWay = Physics2D.OverlapArea(pathHitboxTopStart, pathHitboxBottomEnd, Utility.GetLayerMask("npcPathObstacle"));
		if (isObstacleInTheWay) {
			target = transform.position.x;
		}
		return target;
	}

	void FleeOrbInDirection(Utility.Directions direction) {
		// Target a location that is the maximum movement unit away from the current position
		float offset = direction == Utility.Directions.Right ? maxTargetDistance : -maxTargetDistance;
		// Without running into obstacles (walls/beams)
		targetX = FindTargetBeforeObstacle(transform.position.x + offset);
		hasTarget = true;
	}

	float FindTargetBeforeObstacle(float target) {
		// Size of box that will be cast to look for obstacles
		Vector2 size = new Vector2(spriteRenderers[0].transform.localScale.x, spriteRenderers[0].transform.localScale.y);
		// TODO: the below can cause npcs to walk into beams at head height,
		//  but also stops the hitbox from being triggered by the floor
		size.y -= 0.1f;
		// If we're pressed against a wall, don't let that count as an obstacle
		// NOTE: this value must be lower than the valid distance from target, otherwise we might set an unreachable target
		size.x -= 0.1f;
		// The direction we're attempting to move in
		Vector2 direction = target > transform.position.x ? Vector2.right : Vector2.left;
		RaycastHit2D hit = Physics2D.BoxCast(transform.position, size, 0, direction, Mathf.Abs(target - transform.position.x), Utility.GetLayerMask("npcPathObstacle"));
		if (hit) {
			// Set target for half of the npc's width from actual point of contact
			float padding = size.x / 2;
			return hit.point.x + (direction == Vector2.left ? padding : -padding);
		}
		// Otherwise no obstacle
		return target;
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
	}

	void DespawnSelf() {
		// Send out an event to decrement counter
		EventCodes.RaiseEventAll(EventCodes.NpcDespawned, null);
		PhotonNetwork.Destroy(photonView);
	}

	void SpawnParasite() {
		PlayerObject.SpawnPlayerCharacter(CharacterType.Parasite, transform.position, new Vector2(0, PARASITE_LAUNCH_VELOCITY));
	}

	void HandleBurstCharging() {
		if (isChargingForBurst) {
			timeChargingForBurst += Time.deltaTime;
		}
	}
	
	#endregion
}
