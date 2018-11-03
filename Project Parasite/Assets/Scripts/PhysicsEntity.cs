using UnityEngine;

public class PhysicsEntity {
	Transform transform;

	float height = 0.5f;

	float gravityAcceleration = -1f;
	float maxSpeed = 5f;
	float velocityX = 0f;
	float velocityY = 0f;

	private bool _isOnGround = false;
	public bool IsOnGround() { return _isOnGround; }

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
		// Create layer mask by bitshifting 1 by the int that represents the obstacles layer
		int layerMask = 1 << LayerMask.NameToLayer("Obstacles");
		Collider2D obstacle = Physics2D.OverlapPoint(newPosition + new Vector2(0, -height), layerMask);
		if (obstacle != null) {
			// Entity is touching the ground
			_isOnGround = true;
			// Then new velocityY is 0 and position is directly above the obstacle
			velocityY = 0;
			obstacleHeight = obstacle.transform.localScale.y / 2;
			newPosition.y = obstacle.transform.position.y + obstacleHeight + height;
		} else {
			// Nothing is below entity
			_isOnGround = false;
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
