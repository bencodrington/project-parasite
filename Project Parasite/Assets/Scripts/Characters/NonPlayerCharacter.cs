using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class NonPlayerCharacter : Character {

	public bool isInfected = false;

	private const float PARASITE_LAUNCH_VELOCITY = 30f;

	// Pathfinding
	private float validDistanceFromTarget = .5f;
	// Note that target is currently only used to move horizontally,
	//	and as a result is only the x coordinate of the target location
	private float targetX;
	private float minTimeUntilNewPath = 2f;
	private float maxTimeUntilNewPath = 5f;
	private bool hasTarget = false;
	private float maxTargetDistance = 5f;
	private float minTargetDistance = 2f;

	// The exclamation mark that is shown when orbs are placed nearby
	public GameObject alertIconPrefab;
	// How far from the npc's center to display the icon
	Vector2 ALERT_ICON_OFFSET = new Vector2(0, 1);
	
	public override void Update() {
		if (isInfected && HasAuthority()) {
			// NPC is infected and this client is the Parasite player's client
			HandleInput();
		} else if (!isInfected && HasAuthority() && physicsEntity != null) {
			// NPC still belongs to the server
			TraversePath();
		} else {
			// This is a cloned representation of the authoritative NPC
			// 	So just verify current position is up to date with server position
			transform.position = Vector3.Lerp(transform.position, serverPosition, 0.8f);
		}
	}

    protected override void HandleInput()
    {
		// This function is only called when this NPC is infected,
		// 	and is only called on the Parasite player's client
		// Movement
		HandleHorizontalMovement();
		// Self Destruct
		if (Input.GetMouseButtonDown(1)) {
			// Destroy this NPC
			CmdDespawnSelf();
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
		Vector2 pathHitboxSize = new Vector2(target - transform.position.x, spriteRenderer.transform.localScale.y);
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
		Vector2 size = new Vector2(spriteRenderer.transform.localScale.x, spriteRenderer.transform.localScale.y);
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

	public IEnumerator Idle() {
		yield return new WaitForSeconds(Random.Range(minTimeUntilNewPath, maxTimeUntilNewPath));
		// Check that we are still uninfected and still exist
		if (this != null && !isInfected) { FindNewPath(); }
		
	}

	// Commands

	public void CmdDespawnSelf() {
		// Spawn new Parasite Object
		// TODO:
		// PlayerObject.CmdSpawnPlayerCharacter(CharacterType.Parasite, transform.position, new Vector2(0, PARASITE_LAUNCH_VELOCITY));
		// // Despawn this NPC object
		// FindObjectOfType<NpcManager>().DespawnNpc(netId);
	}

	// ClientRpc

	public void RpcSetLocalPlayerAuthority(bool newValue) {
		GetComponentInChildren<NetworkIdentity>().localPlayerAuthority = newValue;
	}

	public void RpcInfect() {
		isInfected = true;
		if (HasAuthority()) {
			// Only update sprite if on the Parasite player's client
			spriteRenderer.color = Color.magenta;
		}
	}

	public void RpcNearbyOrbAlert(Vector2 atPosition) {
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
}
