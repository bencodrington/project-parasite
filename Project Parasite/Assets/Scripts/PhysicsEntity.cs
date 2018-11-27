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

	// public float pixelOffset = 0.05f;

	private Vector2 oldPixelBelow;
	private Vector2 oldPixelAbove;
	private Vector2 oldPixelToTheLeft;
	private Vector2 oldPixelToTheRight;

	private Collider2D obstacleBelow;
	private Vector2 obstacleBelowOldPosition;

	private bool _isOnGround = false;
	public bool IsOnGround() { return _isOnGround; }
	private bool _isOnCeiling = false;
	public bool IsOnCeiling() { return _isOnCeiling; }
	private bool _isOnLeftWall = false;
	public bool IsOnLeftWall() { return _isOnLeftWall; }
	private bool _isOnRightWall = false;
	public bool IsOnRightWall() { return _isOnRightWall; }

	public bool applyGravity = true;

	int obstacleLayerMask = 1 << LayerMask.NameToLayer("Obstacles");

	public PhysicsEntity(Transform transform, float height = 0.5f, float width = 0.5f) {
		this.transform = transform;
		this.height = height;
		this.width = width;
		this.gravityAcceleration = DEFAULT_GRAVITY;
		oldPixelBelow = transform.position;
	}

	public void Update () {
		Vector2 floorVelocity;

		// Check if obstacle below has moved
		if (obstacleBelow != null && (Vector2)obstacleBelow.transform.position != obstacleBelowOldPosition) {
			// Debug.Log(obstacleBelow.transform.position);
			// Debug.Log(obstacleBelowOldPosition);
			floorVelocity = (Vector2)obstacleBelow.transform.position - obstacleBelowOldPosition;
			// Debug.Log("Velocity BEFORE = " + velocityY);
			velocityY += floorVelocity.y;
			oldPixelBelow += new Vector2(0, velocityY);
			// Debug.Log("Floor velocity = " + floorVelocity);
			// Debug.Log("Velocity = " + velocityY);
		}

		if (applyGravity) {
			// Apply Gravity
			velocityY += gravityAcceleration * Time.deltaTime;
		}
		// Store attempted new position
		Vector2 newPosition = new Vector2(transform.position.x + velocityX, transform.position.y + velocityY);
		// TODO: It may become necessary to loop the below line until it doesn't change
		// Check for, and resolve, collisions below, and then beside
		newPosition = CheckBeside(CheckAboveAndBelow(newPosition));
		// Set new position
		transform.position = newPosition;
	}

	Vector2 CheckAboveAndBelow(Vector2 newPosition) {
		float obstacleHeight;
		obstacleBelow = null;
		Collider2D obstacleAbove = null;
		Vector2 pixelBelow = GetPixelBelow(newPosition);
		Vector2 pixelAbove = newPosition + new Vector2(0, height);
		_isOnGround = false;
		_isOnCeiling = false;
		// Check for obstacles encountered between current position and new position
		obstacleBelow = Physics2D.OverlapArea(oldPixelBelow, pixelBelow, obstacleLayerMask);
		obstacleAbove = Physics2D.OverlapArea(oldPixelAbove, pixelAbove, obstacleLayerMask);
		// Handle Collisions
		if (obstacleBelow != null) {
			// Entity is touching the ground
			_isOnGround = true;
			// Then new velocityY is 0 and position self directly above the obstacle
			velocityY = 0;
			obstacleHeight = obstacleBelow.transform.localScale.y / 2;
			newPosition.y = obstacleBelow.transform.position.y + obstacleHeight + height;
			obstacleBelowOldPosition = obstacleBelow.transform.position;
			pixelBelow = GetPixelBelow(newPosition);
		} else if (obstacleAbove != null) {
			_isOnCeiling = true;
			velocityY = 0;
			obstacleHeight = obstacleAbove.transform.localScale.y / 2;
			newPosition.y = obstacleAbove.transform.position.y - obstacleHeight - height;
		}
		Debug.DrawLine(oldPixelBelow, pixelBelow);
		oldPixelBelow = pixelBelow;
		oldPixelAbove = pixelAbove;
		return newPosition;
	}

	Vector2 CheckBeside(Vector2 newPosition) {
		float obstacleWidth;
		Collider2D obstacleToTheLeft = null;
		Collider2D obstacleToTheRight = null;
		_isOnLeftWall = false;
		_isOnRightWall = false;
		Vector2 pixelToTheLeft 	= newPosition + new Vector2(-width, 0);
		Vector2 pixelToTheRight = newPosition + new Vector2(width, 0);
		// TODO: remove
		Debug.DrawLine(oldPixelToTheLeft, pixelToTheLeft);
		Debug.DrawLine(oldPixelToTheRight, pixelToTheRight);
		// If moving left
		if (velocityX < 0) {
			obstacleToTheLeft	= Physics2D.OverlapArea(oldPixelToTheLeft, pixelToTheLeft, obstacleLayerMask);
		} else if (velocityX > 0) { // Moving right
			obstacleToTheRight 	= Physics2D.OverlapArea(oldPixelToTheRight, pixelToTheRight, obstacleLayerMask);
		}

		// Handle Collisions
		if (obstacleToTheLeft != null) {
			// Stop moving
			velocityX = 0;
			// Align left edge of entity with right edge of obstacle
			obstacleWidth = obstacleToTheLeft.transform.localScale.x / 2;
			newPosition.x = obstacleToTheLeft.transform.position.x + obstacleWidth + width;
			_isOnLeftWall = true;
		}
		if (obstacleToTheRight != null) {
			velocityX = 0;
			obstacleWidth = obstacleToTheRight.transform.localScale.x / 2;
			newPosition.x = obstacleToTheRight.transform.position.x - obstacleWidth - width;
			_isOnRightWall = true;
		}
		oldPixelToTheLeft = pixelToTheLeft;
		oldPixelToTheRight = pixelToTheRight;
		return newPosition;
	}

	public void AddVelocity(float x, float y) {
		velocityX += x;
		velocityY += y;
	}

	Vector2 GetPixelBelow(Vector2 position) {
		return position + new Vector2(0, -height + 0.03f);
	}
}
