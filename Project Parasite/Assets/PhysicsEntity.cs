using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhysicsEntity : MonoBehaviour {

	float gravityAcceleration = -1f;
	const float MAX_SPEED = 5f;
	float velocityX = 0f;
	float velocityY = 0f;

	float height = 0.5f;

	bool isOnGround = false;

	// Update is called once per frame
	void Update () {
		float obstacleHeight;
		velocityY = Mathf.Clamp(velocityY + gravityAcceleration * Time.deltaTime, -MAX_SPEED, MAX_SPEED);
		// Check for collisions at new location
		// If pixel below entity overlaps an obstacle
		Vector2 newPos = new Vector2(transform.position.x, transform.position.y + velocityY);
		Collider2D obstacle = Physics2D.OverlapPoint(newPos + new Vector2(0, -height));
		if (obstacle != null) {
			// Entity is touching the ground
			isOnGround = true;
			// Then new velocityY is 0 and position is directly above the obstacle
			velocityY = 0;
			obstacleHeight = obstacle.transform.localScale.y / 2;
			newPos.y = obstacle.transform.position.y + obstacleHeight + height;
		} else {
			// Nothing is below entity
			isOnGround = false;
		}
		// TODO: if velocity is above a certain threshold (probably just under half of width, assuming it's smaller than height)
		// 	check for collisions in a line between position & new position
		transform.position = newPos;
	}
}
