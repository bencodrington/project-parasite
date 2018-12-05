using UnityEngine;

public class PhysicsEntity {

	private const float DEFAULT_GRAVITY = -2f;
	// While on the ground, horizontal velocity is divided by this constant
	// 	a value of 1 indicates no friction
	//  a value of 2 indicates that horizontal velocity should be halved each physics update
	private const float DEFAULT_FRICTION_DENOMINATOR = 1.25f;

	Transform transform;

	// Hitbox dimensions: 2*height by 2*width
	float height;
	float width;

	// Gravity increases by a rate of 1 unit/second per second
	float gravityAcceleration;
	public float GravityAcceleration() { return gravityAcceleration; }

	float velocityX = 0f;
	float velocityY = 0f;
	public void AddVelocity(float x, float y) {
		velocityX += x;
		velocityY += y;
	}

	// Maintain the velocity from movement input separately
	// 	this allows us to limit movement speed on its own
	float inputVelocityX = 0f;
	float inputVelocityY = 0f;
	public void AddInputVelocity(float velocityX, float velocityY) {
		inputVelocityX += velocityX;
		inputVelocityY += velocityY;
	}

	private Vector2 oldPixelBelow;
	private Vector2 oldPixelAbove;
	private Vector2 oldPixelToTheLeft;
	private Vector2 oldPixelToTheRight;

	private Collider2D obstacleBelow;
	private Vector2 obstacleBelowOldPosition;

	private Collider2D obstacleAbove;
	private Vector2 obstacleAboveOldPosition;

	private bool _isOnGround = false;
	public bool IsOnGround() { return _isOnGround; }
	private bool _isOnCeiling = false;
	public bool IsOnCeiling() { return _isOnCeiling; }
	private bool _isOnLeftWall = false;
	public bool IsOnLeftWall() { return _isOnLeftWall; }
	private bool _isOnRightWall = false;
	public bool IsOnRightWall() { return _isOnRightWall; }
	public bool IsOnWall() { return _isOnLeftWall || _isOnRightWall; }

	public bool applyGravity = true;

	bool _isStuckToCeiling = false;
	public void SetIsStuckToCeiling(bool isStuckToCeiling) {
		_isStuckToCeiling = isStuckToCeiling;
	}

	public PhysicsEntity(Transform transform, float height = 0.5f, float width = 0.5f) {
		this.transform = transform;
		this.height = height;
		this.width = width;
		this.gravityAcceleration = DEFAULT_GRAVITY;
		oldPixelBelow = transform.position;
	}

	// Called by the component that this entity simulates the physics for
	// 	Should be called in the FixedUpdate() method of that MonoBehaviour
	//	Therefore, should run every physics update, every ~0.02 seconds
	public void Update () {
		float floorVelocity = 0;
		float ceilingVelocity = 0;

		if (applyGravity && !_isStuckToCeiling) {
			// Apply Gravity
			velocityY += gravityAcceleration;
		}
		// Apply horizontal friction
		if (applyGravity && _isOnGround) {
			velocityX /= DEFAULT_FRICTION_DENOMINATOR;
		}
		// Apply vertical friction
		if (IsOnWall()) {
			velocityY /= DEFAULT_FRICTION_DENOMINATOR;
		}

		// Check if obstacle below has moved
		if (obstacleBelow != null && (Vector2)obstacleBelow.transform.position != obstacleBelowOldPosition) {
			floorVelocity = obstacleBelow.transform.position.y - obstacleBelowOldPosition.y;
		}
		// Check if obstacle above has moved
		if (obstacleAbove != null && (Vector2)obstacleAbove.transform.position != obstacleAboveOldPosition) {
			ceilingVelocity = obstacleAbove.transform.position.y - obstacleAboveOldPosition.y;
			// Only maintain upward velocity if we're stuck to ceiling
			if (ceilingVelocity > 0 && !_isStuckToCeiling) {
				ceilingVelocity = 0;
			}
		}

		// Store attempted new position
		Vector2 newPosition = (Vector2)transform.position + new Vector2(velocityX, velocityY) * Time.deltaTime;
		// Don't multiply floor/ceiling velocities by Time.deltaTime
		//	because they are calculated based on displacement since last frame
		newPosition.y += floorVelocity;
		newPosition.y += ceilingVelocity;
		// Add velocity from character movement input
		newPosition += new Vector2(inputVelocityX, inputVelocityY) * Time.deltaTime;
		// TODO: It may become necessary to loop the below line until it doesn't change
		// Check for, and resolve, collisions below, and then beside
		newPosition = CheckBeside(CheckAboveAndBelow(newPosition));
		// Set new position
		transform.position = newPosition;
		// Reset inputVelocity, as this should be manually controlled by the character each frame
		inputVelocityX = 0;
		inputVelocityY = 0;
	}

	Vector2 CheckAboveAndBelow(Vector2 newPosition) {
		float obstacleHeight;
		obstacleBelow = null;
		obstacleAbove = null;
		Vector2 pixelBelow = GetPixelBelow(newPosition);
		Vector2 pixelAbove = GetPixelAbove(newPosition);
		_isOnGround = false;
		_isOnCeiling = false;
		// Check for obstacles encountered between current position and new position
		if (velocityY + inputVelocityY < 0 && !_isStuckToCeiling) {
			// Only check for obstacles below us if:
			// -- after all forces have been applied, we are descending and
			// -- we're not stuck to the ceiling
			obstacleBelow = Physics2D.OverlapArea(oldPixelBelow, pixelBelow, Utility.GetLayerMask("obstacle"));
		} else if (velocityY + inputVelocityY >= 0 || _isStuckToCeiling) {
			// Only check for obstacles above us if:
			// -- after all forces have been applied, we are ascending ( // TODO: or stationary?)
			// -- OR, we're stuck to the ceiling already
			obstacleAbove = Physics2D.OverlapArea(oldPixelAbove, pixelAbove, Utility.GetLayerMask("obstacle"));
		}
		// Handle Collisions
		if (obstacleBelow != null) {
			// Entity is touching the ground
			_isOnGround = true;
			// Then new velocityY is 0 and position self directly above the obstacle
			velocityY = 0;
			obstacleHeight = obstacleBelow.transform.localScale.y / 2;
			newPosition.y = obstacleBelow.transform.position.y + obstacleHeight + height;
			obstacleBelowOldPosition = obstacleBelow.transform.position;
			// Re-update triggers
			pixelBelow = GetPixelBelow(newPosition);
			pixelAbove = GetPixelAbove(newPosition);
		} else if (obstacleAbove != null) {
			_isOnCeiling = true;
			velocityY = 0;
			obstacleHeight = obstacleAbove.transform.localScale.y / 2;
			newPosition.y = obstacleAbove.transform.position.y - obstacleHeight - height;
			obstacleAboveOldPosition = obstacleAbove.transform.position;
			// Re-update triggers
			pixelBelow = GetPixelBelow(newPosition);
			pixelAbove = GetPixelAbove(newPosition);
		}
		// Technically the below should occur after the CheckBeside() call is made, might fix some bugs,
		// 	but is only definitely responsible for misplaced 'drawline trail' for pixelbelow
		Debug.DrawLine(oldPixelBelow, pixelBelow);
		Debug.DrawLine(oldPixelAbove, pixelAbove);
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
		Vector2 pixelToTheLeft 	= GetPixelToTheLeft(newPosition);
		Vector2 pixelToTheRight = GetPixelToTheRight(newPosition);
		// If moving left
		if (velocityX + inputVelocityX < 0) {
			obstacleToTheLeft	= Physics2D.OverlapArea(oldPixelToTheLeft, pixelToTheLeft, Utility.GetLayerMask("obstacle"));
		} else if (velocityX + inputVelocityX > 0) { // Moving right
			obstacleToTheRight 	= Physics2D.OverlapArea(oldPixelToTheRight, pixelToTheRight, Utility.GetLayerMask("obstacle"));
		}

		// Handle Collisions
		if (obstacleToTheLeft != null) {
			// Stop moving
			velocityX = 0;
			// Align left edge of entity with right edge of obstacle
			obstacleWidth = obstacleToTheLeft.transform.localScale.x / 2;
			newPosition.x = obstacleToTheLeft.transform.position.x + obstacleWidth + width;
			// Re-update triggers
			pixelToTheLeft 	= GetPixelToTheLeft(newPosition);
			pixelToTheRight = GetPixelToTheRight(newPosition);
			_isOnLeftWall = true;
		}
		if (obstacleToTheRight != null) {
			velocityX = 0;
			obstacleWidth = obstacleToTheRight.transform.localScale.x / 2;
			newPosition.x = obstacleToTheRight.transform.position.x - obstacleWidth - width;
			// Re-update triggers
			pixelToTheLeft 	= GetPixelToTheLeft(newPosition);
			pixelToTheRight = GetPixelToTheRight(newPosition);
			_isOnRightWall = true;
		}
		Debug.DrawLine(oldPixelToTheLeft, pixelToTheLeft);
		Debug.DrawLine(oldPixelToTheRight, pixelToTheRight);
		oldPixelToTheLeft = pixelToTheLeft;
		oldPixelToTheRight = pixelToTheRight;
		return newPosition;
	}

	Vector2 GetPixelBelow(Vector2 position) {
		return position + new Vector2(0, -height);
	}Vector2 GetPixelAbove(Vector2 position) {
		return position + new Vector2(0, height);
	}
	Vector2 GetPixelToTheLeft(Vector2 position) {
		return position + new Vector2(-width, 0);
	}Vector2 GetPixelToTheRight(Vector2 position) {
		return position + new Vector2(width, 0);
	}
}
