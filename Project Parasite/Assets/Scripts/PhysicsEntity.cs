using UnityEngine;

public class PhysicsEntity {

	private const float DEFAULT_GRAVITY = -2f;

	Transform transform;

	// Hitbox dimensions: 2*height by 2*width
	float height;
	float width;

	// Gravity increases by a rate of 1 unit/second per second
	float gravityAcceleration;

	public float velocityX = 0f;
	public float velocityY = 0f;

	private Vector2 oldPixelBelow;
	private Vector2 oldPixelToTheLeft;
	private Vector2 oldPixelToTheRight;

	private bool _isOnGround = false;
	public bool IsOnGround() { return _isOnGround; }

	int obstacleLayerMask = 1 << LayerMask.NameToLayer("Obstacles");

	public PhysicsEntity(Transform transform, float height = 0.5f, float width = 0.5f) {
		this.transform = transform;
		this.height = height;
		this.width = width;
		this.gravityAcceleration = DEFAULT_GRAVITY;
		oldPixelBelow = transform.position;
	}

	public void Update () {
		// Apply Gravity
		velocityY += gravityAcceleration * Time.deltaTime;
		if (Input.GetKey(KeyCode.Q)) {
			Debug.Log("velocityY: " + velocityY);
		}
		// Store attempted new position
		Vector2 newPosition = new Vector2(transform.position.x + velocityX, transform.position.y + velocityY);
		// Check for, and resolve, collisions
		newPosition = new Vector2(CheckBeside(newPosition), CheckBelow(newPosition));
		// Set new position
		transform.position = newPosition;
	}

	float CheckBelow(Vector2 newPosition) {
		float obstacleHeight;
		Collider2D obstacleBelow;
		Vector2 pixelBelow = newPosition + new Vector2(0, -height);
		// TODO: remove
		Debug.DrawLine(oldPixelBelow, pixelBelow);
		// If moving down
		if (velocityY < 0) {
			// Check for obstacles encountered between current position and new position
			obstacleBelow = Physics2D.OverlapArea(oldPixelBelow, pixelBelow, obstacleLayerMask);
		} else {
			obstacleBelow = null;
		}
		oldPixelBelow = pixelBelow;
		// Handle Collisions
		if (obstacleBelow != null) {
			// Entity is touching the ground
			_isOnGround = true;
			// Then new velocityY is 0 and position self directly above the obstacle
			velocityY = 0;
			obstacleHeight = obstacleBelow.transform.localScale.y / 2;
			newPosition.y = obstacleBelow.transform.position.y + obstacleHeight + height;
		} else {
			// Nothing is below entity
			_isOnGround = false;
		}
		return newPosition.y;
	}

	float CheckBeside(Vector2 newPosition) {
		float obstacleWidth;
		Collider2D obstacleToTheLeft, obstacleToTheRight;
		Vector2 pixelToTheLeft 	= newPosition + new Vector2(-width, 0);
		Vector2 pixelToTheRight = newPosition + new Vector2(width, 0);
		// If moving left
		if (velocityX < 0) {
			obstacleToTheLeft	= Physics2D.OverlapArea(oldPixelToTheLeft, pixelToTheLeft, obstacleLayerMask);
			obstacleToTheRight 	= null;
		} else { // Moving right
			obstacleToTheRight 	= Physics2D.OverlapArea(oldPixelToTheRight, pixelToTheRight, obstacleLayerMask);
			obstacleToTheLeft	= null;
		}
		oldPixelToTheLeft = pixelToTheLeft;
		oldPixelToTheRight = pixelToTheRight;

		// Handle Collisions
		if (obstacleToTheLeft != null) {
			// Stop moving
			velocityX = 0;
			// Align left edge of entity with right edge of obstacle
			obstacleWidth = obstacleToTheLeft.transform.localScale.x / 2;
			newPosition.x = obstacleToTheLeft.transform.position.x + obstacleWidth + width;
		}
		if (obstacleToTheRight != null) {
			velocityX = 0;
			obstacleWidth = obstacleToTheRight.transform.localScale.x / 2;
			newPosition.x = obstacleToTheRight.transform.position.x - obstacleWidth - width;
		}

		return newPosition.x;
	}

	public void AddVelocity(float x, float y) {
		velocityX += x;
		velocityY += y;
	}
}
