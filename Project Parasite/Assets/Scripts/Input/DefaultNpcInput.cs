using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DefaultNpcInput : InputSource
{
    #region [Public Methods]
    
    public override void UpdateInputState() {
        base.UpdateInputState();
        state[InputSource.Key.right] = true; // Keep moving right //TODO: fix
        // TraversePath();
    }

	public void StartIdling() {
		// TODO:
		// StartCoroutine(Idle());
	}
    
    #endregion

    #region [Private Methods]

	// void TraversePath() {
	// 	isMovingLeft = false;
	// 	isMovingRight = false;
	// 	if (!hasTarget) { return; }
	// 	if (Mathf.Abs(this.transform.position.x - targetX) < validDistanceFromTarget) {
	// 		// Reached target
	// 		StartIdling();
	// 		// Stop traversing path
	// 		hasTarget = false;
	// 	} else {
	// 		// Still moving
	// 		if (targetX >= transform.position.x) {
	// 			isMovingRight = true;
	// 		} else {
	// 			isMovingLeft = true;
	// 		}
	// 	}
	// }

	// void FindNewPath() {
	// 	// Randomly select offset that is +/-[minTargetDistance, maxTargetDistance]
	// 	float rangeDifference = MAX_TARGET_DISTANCE - MIN_TARGET_DISTANCE;
	// 	float offset = Random.Range(-rangeDifference, rangeDifference);
	// 	offset += (offset >= 0) ? MIN_TARGET_DISTANCE : -MIN_TARGET_DISTANCE;
	// 	// Set target relative to current location
	// 	targetX = transform.position.x + offset;
	// 	// If there is a wall/ beam in the way, don't move to new target
	// 	targetX = ModifyTargetToAvoidObstacles(targetX);
	// 	// Begin traversing
	// 	hasTarget = true;
	// }

	// float ModifyTargetToAvoidObstacles(float target) {
	// 	// TODO: fix following line
	// 	Vector2 pathHitboxSize = new Vector2(target - transform.position.x, spriteRenderers[0].transform.localScale.y);
	// 	// TODO: the below can cause npcs to walk into beams at head height,
	// 	//  but also stops the hitbox from being triggered by the floor
	// 	pathHitboxSize.y -= 0.1f;
	// 	// Calculate corners of hitbox
	// 	Vector2 pathHitboxTopStart = new Vector2(transform.position.x, transform.position.y + pathHitboxSize.y / 2);
	// 	Vector2 pathHitboxBottomEnd = new Vector2(target, transform.position.y - pathHitboxSize.y / 2);
	// 	bool isObstacleInTheWay = Physics2D.OverlapArea(pathHitboxTopStart, pathHitboxBottomEnd, Utility.GetLayerMask("npcPathObstacle"));
	// 	if (isObstacleInTheWay) {
	// 		target = transform.position.x;
	// 	}
	// 	return target;
	// }

	// void FleeOrbInDirection(Utility.Directions direction) {
	// 	// Target a location that is the maximum movement unit away from the current position
	// 	float offset = direction == Utility.Directions.Right ? FLEE_DISTANCE : -FLEE_DISTANCE;
	// 	// Without running into obstacles (walls/beams)
	// 	targetX = FindTargetBeforeObstacle(transform.position.x + offset);
	// 	hasTarget = true;
	// }

	// float FindTargetBeforeObstacle(float target) {
	// 	// Size of box that will be cast to look for obstacles
	// 	Vector2 size = new Vector2(spriteRenderers[0].transform.localScale.x, spriteRenderers[0].transform.localScale.y);
	// 	// TODO: the below can cause npcs to walk into beams at head height,
	// 	//  but also stops the hitbox from being triggered by the floor
	// 	size.y -= 0.1f;
	// 	// If we're pressed against a wall, don't let that count as an obstacle
	// 	// NOTE: this value must be lower than the valid distance from target, otherwise we might set an unreachable target
	// 	size.x -= 0.1f;
	// 	// The direction we're attempting to move in
	// 	Vector2 direction = target > transform.position.x ? Vector2.right : Vector2.left;
	// 	RaycastHit2D hit = Physics2D.BoxCast(transform.position, size, 0, direction, Mathf.Abs(target - transform.position.x), Utility.GetLayerMask("npcPathObstacle"));
	// 	if (hit) {
	// 		// Set target for half of the npc's width from actual point of contact
	// 		float padding = size.x / 2;
	// 		return hit.point.x + (direction == Vector2.left ? padding : -padding);
	// 	}
	// 	// Otherwise no obstacle
	// 	return target;
	// }

	// IEnumerator Idle() {
	// 	yield return new WaitForSeconds(Random.Range(minTimeUntilNewPath, maxTimeUntilNewPath));
	// 	// Check that we are still uninfected and still exist
	// 	if (this != null && !isInfected) { FindNewPath(); }
	// }
    
    #endregion

}
