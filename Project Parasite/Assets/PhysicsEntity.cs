using UnityEngine;

public class PhysicsEntity {
	Transform transform;

	float height = 0.5f;

	float gravityAcceleration = -1f;
	float maxSpeed = 5f;
	float velocityX = 0f;
	float velocityY = 0f;

	bool isOnGround = false;

	public PhysicsEntity(Transform transform, float height = 0.5f, float gravity = -1f, float maxSpeed = 5f) {
		this.transform = transform;
		this.height = height;
		this.gravityAcceleration = gravity;
	}

	public void Update () {
		float obstacleHeight;
		velocityY = Mathf.Clamp(velocityY + gravityAcceleration * Time.deltaTime, -maxSpeed, maxSpeed);
		// Check for collisions at new location
		// If pixel below entity overlaps an obstacle
		Vector2 newPosition = new Vector2(transform.position.x + velocityX, transform.position.y + velocityY);
		Collider2D obstacle = Physics2D.OverlapPoint(newPosition + new Vector2(0, -height));
		if (obstacle != null) {
			// Entity is touching the ground
			isOnGround = true;
			// Then new velocityY is 0 and position is directly above the obstacle
			velocityY = 0;
			obstacleHeight = obstacle.transform.localScale.y / 2;
			newPosition.y = obstacle.transform.position.y + obstacleHeight + height;
		} else {
			// Nothing is below entity
			isOnGround = false;
		}
		// TODO: if velocity is above a certain threshold (probably just under half of width, assuming it's smaller than height)
		// 	check for collisions in a line between position & new position
		transform.position = newPosition;
	}

	public void AddVelocity(float x, float y) {
		velocityX += x;
		velocityY += y;
	}
}
