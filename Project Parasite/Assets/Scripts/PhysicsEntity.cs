using UnityEngine;

public class PhysicsEntity {
	Transform transform;

	float height = 0.5f;
	float width = 0.5f;

	float gravityAcceleration = -1f;
	float maxSpeed = 5f;
	float velocityX = 0f;
	float velocityY = 0f;

	private const float GRAVITY = -2f;

	private bool _isOnGround = false;
	public bool IsOnGround() { return _isOnGround; }

	public PhysicsEntity(Transform transform, float height = 0.5f, float width = 0.5f, float gravity = GRAVITY, float maxSpeed = 5f) {
		this.transform = transform;
		this.height = height;
		this.width = width;
		this.gravityAcceleration = gravity;
	}

	public void Update () {
		float obstacleHeight, obstacleWidth;
		// Apply Gravity
		velocityY = Mathf.Clamp(velocityY + gravityAcceleration * Time.deltaTime, -maxSpeed, maxSpeed);
		// Check for collisions at new location
		Vector2 newPosition = new Vector2(transform.position.x + velocityX, transform.position.y + velocityY);
		// Set up points to check for collisions
		Vector2 pixelBelow 		= newPosition + new Vector2(0, -height);
		Vector2 pixelToTheLeft 	= newPosition + new Vector2(-width, 0);
		Vector2 pixelToTheRight = newPosition + new Vector2(width, 0);
		// Create layer mask by bitshifting 1 by the int that represents the obstacles layer
		int layerMask = 1 << LayerMask.NameToLayer("Obstacles");
		// Cast rays
		Collider2D obstacleBelow 		= Physics2D.OverlapPoint(pixelBelow, layerMask);
		Collider2D obstacleToTheLeft 	= Physics2D.OverlapPoint(pixelToTheLeft, layerMask);
		Collider2D obstacleToTheRight 	= Physics2D.OverlapPoint(pixelToTheRight, layerMask);
		// Handle Collisions
		if (obstacleBelow != null) {
			// Entity is touching the ground
			_isOnGround = true;
			// Then new velocityY is 0 and position is directly above the obstacle
			velocityY = 0;
			obstacleHeight = obstacleBelow.transform.localScale.y / 2;
			newPosition.y = obstacleBelow.transform.position.y + obstacleHeight + height;
		} else {
			// Nothing is below entity
			_isOnGround = false;
		}
		if (obstacleToTheLeft != null && obstacleToTheRight != null) {
			Debug.LogError("Error: PhysicsEntity is being crushed.");
			return;
		}
		if (obstacleToTheLeft != null) {
			velocityX = 0;
			obstacleWidth = obstacleToTheLeft.transform.localScale.x / 2;
			newPosition.x = obstacleToTheLeft.transform.position.x + obstacleWidth + width;
		}
		if (obstacleToTheRight != null) {
			velocityX = 0;
			obstacleWidth = obstacleToTheRight.transform.localScale.x / 2;
			newPosition.x = obstacleToTheRight.transform.position.x - obstacleWidth - width;
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
