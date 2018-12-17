using UnityEngine;
using UnityEditor;

public class PhysicsEntity {
	/// Constants ///
	private const float DEFAULT_GRAVITY = -2f;
	// While on the ground, horizontal velocity is divided by this constant
	// 	a value of 1 indicates no friction
	//  a value of 2 indicates that horizontal velocity should be halved each physics update
	private const float DEFAULT_FRICTION_DENOMINATOR = 1.25f;
	// Gravity increases by a rate of 1 unit/second per second
	float gravityAcceleration;
	public float GravityAcceleration() { return gravityAcceleration; }
	// Hitbox dimensions: 2*height by 2*width
	float height;
	float width;
	/// Velocity ///
	float velocityX = 0f;
	float velocityY = 0f;
	public void AddVelocity(float x, float y) {
		velocityX += x;
		velocityY += y;
	}
	public void AddVelocity(Vector2 velocity) {
		velocityX += velocity.x;
		velocityY += velocity.y;
	}
	// Maintain the velocity from movement input separately
	// 	this allows us to limit movement speed on its own
	float inputVelocityX = 0f;
	float inputVelocityY = 0f;
	public void AddInputVelocity(float velocityX, float velocityY) {
		inputVelocityX += velocityX;
		inputVelocityY += velocityY;
	}
	void ResetInputVelocity() {
		inputVelocityX = 0;
		inputVelocityY = 0;
	}
	/// Objects ///
	Transform transform;
	// Keep track of obstacles for determining how far they've moved since last frame
	// 	so that this object can absorb their momentum (elevators, etc.)
	private Collider2D obstacleBelow;
	private Collider2D obstacleAbove;
	// These obstacles do not currently require their oldPosition to be stored
	//	because there do not yet exist any obstacles that can move the player horizontally
	Collider2D obstacleToTheLeft;
	Collider2D obstacleToTheRight;
	private Vector2 obstacleBelowOldPosition;
	private Vector2 obstacleAboveOldPosition;
	// The old position of the entity, used for collision checking
	private Vector2 oldPixelBelow;
	private Vector2 oldPixelAbove;
	private Vector2 oldPixelToTheLeft;
	private Vector2 oldPixelToTheRight;
	// Public-facing methods for determining the state of the entity
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
	/// Constructor and Methods ///
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
		HandleGravity();
		ApplyFriction();
		// Store attempted new position
		Vector2 newPosition = (Vector2)transform.position + new Vector2(velocityX, velocityY) * Time.deltaTime;
		// Add velocity from moving obstacles nearby
		// Note: movingObstacleVelocity is currently only in the y dimension as there do not currently exist
		// 	any objects that can move the player horizontally
		float movingObstacleVelocity = GetMovingObstacleVelocity();
		// Don't multiply movingObstacleVelocity by Time.deltaTime
		//	because it is calculated based on displacement since last frame
		newPosition.y += movingObstacleVelocity;
		// Add velocity from character movement input
		newPosition += new Vector2(inputVelocityX, inputVelocityY) * Time.deltaTime;
		// Check for and resolve collisions
		newPosition = ResolveCollisionsAt(newPosition);
		// Set new position
		transform.position = newPosition;
		// Cache the current sensor pixels for tracking movement next frame
		CacheSensorPixels(newPosition);
		// Reset inputVelocity, as this should be manually controlled by the character each frame
		ResetInputVelocity();
	}

	void HandleGravity() {
		if (applyGravity && !_isStuckToCeiling) {
			// Apply Gravity
			velocityY += gravityAcceleration;
		}
	}

	void ApplyFriction() {
		// Apply horizontal friction
		if (applyGravity && _isOnGround) {
			velocityX /= DEFAULT_FRICTION_DENOMINATOR;
		}
		// Apply vertical friction
		if (IsOnWall()) {
			velocityY /= DEFAULT_FRICTION_DENOMINATOR;
		}
	}

	float GetMovingObstacleVelocity() {
		float floorVelocity = 0;
		float ceilingVelocity = 0;
		float collisionVelocity;
		// Check if obstacle below has moved
		// Note that obstacleBelow is leftover from last frame
		if (obstacleBelow != null && (Vector2)obstacleBelow.transform.position != obstacleBelowOldPosition) {
			floorVelocity = obstacleBelow.transform.position.y - obstacleBelowOldPosition.y;
		}
		// Check if obstacle above has moved
		if (obstacleAbove != null && (Vector2)obstacleAbove.transform.position != obstacleAboveOldPosition) {
			ceilingVelocity = obstacleAbove.transform.position.y - obstacleAboveOldPosition.y;
		}
		collisionVelocity = floorVelocity;
		// Don't transfer upward velocity unless we're stuck to ceiling
		collisionVelocity += (ceilingVelocity > 0 && !_isStuckToCeiling) ? 0 : ceilingVelocity;
		return collisionVelocity;
	}

	Vector2 ResolveCollisionsAt(Vector2 newPosition) {
		// Always check for relevant collisions at the most recent version of newPosition
		CheckCollisionsAbove(newPosition);
		newPosition = ResolveCollisionsAbove(newPosition);
		CheckCollisionsBelow(newPosition);
		newPosition = ResolveCollisionsBelow(newPosition);
		CheckCollisionsLeftOf(newPosition);
		newPosition = ResolveCollisionsLeftOf(newPosition);
		CheckCollisionsRightOf(newPosition);
		newPosition = ResolveCollisionsRightOf(newPosition);
		return newPosition;
	}

	void CheckCollisionsAbove(Vector2 newPosition) {
		Vector2 pixelAbove = GetPixelAbove(newPosition);
		obstacleAbove = Physics2D.OverlapArea(oldPixelAbove, pixelAbove, Utility.GetLayerMask("obstacle"));
	}
	void CheckCollisionsBelow(Vector2 newPosition) {
		Vector2 pixelBelow = GetPixelBelow(newPosition);
		obstacleBelow = Physics2D.OverlapArea(oldPixelBelow, pixelBelow, Utility.GetLayerMask("obstacle"));
	}
	void CheckCollisionsLeftOf(Vector2 newPosition) {
		Vector2 pixelToTheLeft 	= GetPixelToTheLeft(newPosition);
		obstacleToTheLeft = Physics2D.OverlapArea(oldPixelToTheLeft, pixelToTheLeft, Utility.GetLayerMask("obstacle"));
	}
	void CheckCollisionsRightOf(Vector2 newPosition) {
		Vector2 pixelToTheRight = GetPixelToTheRight(newPosition);
		obstacleToTheRight = Physics2D.OverlapArea(oldPixelToTheRight, pixelToTheRight, Utility.GetLayerMask("obstacle"));
	}
	
	Vector2 ResolveCollisionsAbove(Vector2 newPosition) {
		float obstacleHeight;
		_isOnCeiling = false;
		if (obstacleAbove != null && ShouldCollideWithObstacleAbove()) {
			// Then new velocityY is 0 and position self directly below the obstacle
			velocityY = 0;
			obstacleHeight = obstacleAbove.transform.localScale.y / 2;
			newPosition.y = obstacleAbove.transform.position.y - obstacleHeight - height;
			// Cache obstacle position for calculating displacement next frame
			obstacleAboveOldPosition = obstacleAbove.transform.position;
			_isOnCeiling = true;
		}
		return newPosition;
	}
	Vector2 ResolveCollisionsBelow(Vector2 newPosition) {
		float obstacleHeight;
		_isOnGround = false;
		if (obstacleBelow != null && ShouldCollideWithObstacleBelow()) {
			// Then new velocityY is 0 and position self directly above the obstacle
			velocityY = 0;
			obstacleHeight = obstacleBelow.transform.localScale.y / 2;
			newPosition.y = obstacleBelow.transform.position.y + obstacleHeight + height;
			// Cache obstacle position for calculating displacement next frame
			obstacleBelowOldPosition = obstacleBelow.transform.position;
			_isOnGround = true;
		}
		return newPosition;
	}
	Vector2 ResolveCollisionsLeftOf(Vector2 newPosition) {
		_isOnLeftWall = false;
		float obstacleWidth;
		if (obstacleToTheLeft != null && ShouldCollideWithObstacleToTheLeft()) {
			// Stop moving
			velocityX = 0;
			// Align left edge of entity with right edge of obstacle
			obstacleWidth = obstacleToTheLeft.transform.localScale.x / 2;
			newPosition.x = obstacleToTheLeft.transform.position.x + obstacleWidth + width;
			_isOnLeftWall = true;
		}
		return newPosition;
	}
	Vector2 ResolveCollisionsRightOf(Vector2 newPosition) {
		_isOnRightWall = false;
		float obstacleWidth;
		if (obstacleToTheRight != null && ShouldCollideWithObstacleToTheRight()) {
			// Stop moving
			velocityX = 0;
			// Align right edge of entity with left edge of obstacle
			obstacleWidth = obstacleToTheRight.transform.localScale.x / 2;
			newPosition.x = obstacleToTheRight.transform.position.x - obstacleWidth - width;
			_isOnRightWall = true;
		}
		return newPosition;
	}

	bool ShouldCollideWithObstacleAbove() {
		// Return true unless
		bool shouldCollide = true;
		// TODO: implement the below
		// -- our downward velocity is higher than the obstacle above's downward velocity
		if (!isMovingUp()) {
			shouldCollide = false;
		}
		// -- and not stuck to the ceiling
		if (_isStuckToCeiling) {
			shouldCollide = true;
		}
		return shouldCollide;
	}
	bool ShouldCollideWithObstacleBelow() {
		// Return true unless
		bool shouldCollide = true;
		// TODO: implement the below
		// -- our upward velocity is higher than the obstacle below's upward velocity
		if (isMovingUp()) {
			shouldCollide = false;
		}
		return shouldCollide;
	}
	bool ShouldCollideWithObstacleToTheLeft() {
		// Return true unless
		bool shouldCollide = true;
		// -- we're moving right
		if (isMovingRight()) {
			shouldCollide = false;
		}
		return shouldCollide;
	}
	bool ShouldCollideWithObstacleToTheRight() {
		// Return true unless
		bool shouldCollide = true;
		// -- we're moving left
		if (isMovingLeft()) {
			shouldCollide = false;
		}
		return shouldCollide;
	}
	
	bool isMovingUp() {
		return velocityY + inputVelocityY > 0;
	}
	bool isMovingLeft() {
		return velocityX + inputVelocityX < 0;
	}
	bool isMovingRight() {
		return velocityX + inputVelocityX > 0;
	}

	void CacheSensorPixels(Vector2 newPosition) {
		oldPixelAbove = GetPixelAbove(newPosition);
		oldPixelBelow = GetPixelBelow(newPosition);
		oldPixelToTheLeft = GetPixelToTheLeft(newPosition);
		oldPixelToTheRight = GetPixelToTheRight(newPosition);
	}

	Vector2 GetPixelBelow(Vector2 position) {
		return position + new Vector2(0, -height);
	}
	Vector2 GetPixelAbove(Vector2 position) {
		return position + new Vector2(0, height);
	}
	Vector2 GetPixelToTheLeft(Vector2 position) {
		return position + new Vector2(-width, 0);
	}
	Vector2 GetPixelToTheRight(Vector2 position) {
		return position + new Vector2(width, 0);
	}
}
