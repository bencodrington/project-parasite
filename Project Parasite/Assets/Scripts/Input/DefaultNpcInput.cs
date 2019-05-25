using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DefaultNpcInput : InputSource
{

	#region [Private Variables]

	// The range of distances that NPCs will randomly select from
	// 	when choosing a new point to travel to
	const float MAX_TARGET_DISTANCE = 5f;
	const float MIN_TARGET_DISTANCE = 2f;
	// The minimum distance from a movement target that will be considered close enough to stop
	const float VALID_DISTANCE_FROM_TARGET = .5f;
	// The farthest that NPCs will try to move when running away
	const float FLEE_DISTANCE = 8f;
	// The x coordinate of the movement target
	float targetX;
	float minTimeUntilNewPath = 2f;
	float maxTimeUntilNewPath = 5f;
	bool hasTarget = false;

	Coroutine idle;
	
	#endregion

    #region [Public Methods]

	public DefaultNpcInput() : base() {
		StartIdling();
	}
    
    public override void UpdateInputState() {
        base.UpdateInputState();
        TraversePath();
    }

	public virtual void StartIdling() {
		if (idle != null) {
			MatchManager.Instance.StopCoroutine(idle);
		}
		idle = MatchManager.Instance.StartCoroutine(Idle());
	}

	public override void SetOwner(Character owner) {
		base.SetOwner(owner);
		((NonPlayerCharacter)owner).RegisterOnNearbyOrbAlertCallback(FleeOrbAtPosition);
		// TODO: Unregister when this is replaced by another input
	}
    
    #endregion

    #region [Private Methods]

	void TraversePath() {
		if (!hasTarget) { return; }
		if (Mathf.Abs(owner.transform.position.x - targetX) < VALID_DISTANCE_FROM_TARGET) {
			// Reached target
			StartIdling();
			// Stop traversing path
			hasTarget = false;
		} else {
			// Still moving
			if (targetX >= owner.transform.position.x) {
				state[InputSource.Key.right] = true;
			} else {
				state[InputSource.Key.left] = true;
			}
		}
	}

	void FindNewPath() {
		// Randomly select offset that is +/-[minTargetDistance, maxTargetDistance]
		float rangeDifference = MAX_TARGET_DISTANCE - MIN_TARGET_DISTANCE;
		float offset = Random.Range(-rangeDifference, rangeDifference);
		offset += (offset >= 0) ? MIN_TARGET_DISTANCE : -MIN_TARGET_DISTANCE;
		// Set target relative to current location
		targetX = owner.transform.position.x + offset;
		// If there is a wall/ beam in the way, don't move to new target
		targetX = ModifyTargetToAvoidObstacles(targetX);
		// Begin traversing
		hasTarget = true;
	}

	float ModifyTargetToAvoidObstacles(float target) {
		Vector2 pathHitboxSize = new Vector2(target - owner.transform.position.x, owner.stats.height * 2);
		// TODO: the below can cause npcs to walk into beams at head height,
		//  but also stops the hitbox from being triggered by the floor
		pathHitboxSize.y -= 0.1f;
		// Calculate corners of hitbox
		Vector2 pathHitboxTopStart = new Vector2(owner.transform.position.x, owner.transform.position.y + pathHitboxSize.y / 2);
		Vector2 pathHitboxBottomEnd = new Vector2(target, owner.transform.position.y - pathHitboxSize.y / 2);
		bool isObstacleInTheWay = Physics2D.OverlapArea(pathHitboxTopStart, pathHitboxBottomEnd, Utility.GetLayerMask("npcPathObstacle"));
		if (isObstacleInTheWay) {
			target = owner.transform.position.x;
		}
		return target;
	}

	void FleeOrbAtPosition(Vector2 position) {
		Utility.Directions fleeDirection = position.x < owner.transform.position.x ?
			Utility.Directions.Right :
			Utility.Directions.Left;
		FleeInDirection(fleeDirection);
	}

	void FleeInDirection(Utility.Directions direction) {
		// Target a location that is the maximum movement unit away from the current position
		float offset = direction == Utility.Directions.Right ? FLEE_DISTANCE : -FLEE_DISTANCE;
		// Without running into obstacles (walls/beams)
		targetX = FindTargetBeforeObstacle(owner.transform.position.x + offset);
		hasTarget = true;
	}

	float FindTargetBeforeObstacle(float target) {
		// Size of box that will be cast to look for obstacles
		Vector2 size = new Vector2(owner.stats.width * 2, owner.stats.height * 2);
		// TODO: the below can cause npcs to walk into beams at head height,
		//  but also stops the hitbox from being triggered by the floor
		size.y -= 0.1f;
		// If we're pressed against a wall, don't let that count as an obstacle
		// NOTE: this value must be lower than the valid distance from target, otherwise we might set an unreachable target
		size.x -= 0.1f;
		// The direction we're attempting to move in
		Vector2 direction = target > owner.transform.position.x ? Vector2.right : Vector2.left;
		RaycastHit2D hit = Physics2D.BoxCast(owner.transform.position, size, 0, direction, Mathf.Abs(target - owner.transform.position.x), Utility.GetLayerMask("npcPathObstacle"));
		if (hit) {
			// Set target for half of the npc's width from actual point of contact
			float padding = size.x / 2;
			return hit.point.x + (direction == Vector2.left ? padding : -padding);
		}
		// Otherwise no obstacle
		return target;
	}

	IEnumerator Idle() {
		yield return new WaitForSeconds(Random.Range(minTimeUntilNewPath, maxTimeUntilNewPath));
		// CLEANUP: StopCoroutine() should be called when the NPC is destroyed
		if (owner != null && owner.IsUninfectedNpc()) { 
			FindNewPath();
		}
	}
    
    #endregion

}
