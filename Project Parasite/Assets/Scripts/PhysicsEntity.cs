using UnityEngine;

public class PhysicsEntity {
	Transform transform;

	// Hitbox dimensions: 2*height by 2*width
	float height = 0.5f;
	float width = 0.5f;

	// Gravity increases by a rate of 1 unit/second per second
	float gravityAcceleration = -1f;
	// TODO: Limit overall speed, not just velocityY
	float maxSpeed = 5f;
	public float velocityX = 0f;
	public float velocityY = 0f;

	private const float DEFAULT_GRAVITY = -2f;

	private bool _isOnGround = false;
	public bool IsOnGround() { return _isOnGround; }

	int obstacleLayerMask = 1 << LayerMask.NameToLayer("Obstacles");

	public PhysicsEntity(Transform transform, float height = 0.5f, float width = 0.5f) {
		this.transform = transform;
		this.height = height;
		this.width = width;
		this.gravityAcceleration = DEFAULT_GRAVITY;
	}

	public void Update () {
		float obstacleHeight, obstacleWidth;
		// Apply Gravity
		velocityY = Mathf.Clamp(velocityY + gravityAcceleration * Time.deltaTime, -maxSpeed, maxSpeed);
		// Store attempted new position
		Vector2 newPosition = new Vector2(transform.position.x + velocityX, transform.position.y + velocityY);
		// BELOW
		// Set up point to check for collisions
		Vector2 pixelBelow 			= newPosition + new Vector2(0, -height);
		// Cast ray
		Collider2D obstacleBelow 	= Physics2D.OverlapPoint(pixelBelow, obstacleLayerMask);
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
		// BESIDE
		// Set up points to check for collisions
		Vector2 pixelToTheLeft 			= newPosition + new Vector2(-width, 0);
		Vector2 pixelToTheRight 		= newPosition + new Vector2(width, 0);
		// Cast rays
		Collider2D obstacleToTheLeft 	= Physics2D.OverlapPoint(pixelToTheLeft, obstacleLayerMask);
		Collider2D obstacleToTheRight 	= Physics2D.OverlapPoint(pixelToTheRight, obstacleLayerMask);
		// Handle Collisions
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
		// Finally set new position
		transform.position = newPosition;
	}

	public void AddVelocity(float x, float y) {
		velocityX += x;
		velocityY += y;
	}
}
