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
	// The (currently only vertical) velocity of kinematic objects affecting this entity
	//	e.g. elevators, etc.
	float movingObstacleVelocity;
	/// Objects ///
	Transform transform;
	// Keep track of obstacles for determining how far they've moved since last frame
	// 	so that this object can absorb their momentum (elevators, etc.)
	private Collider2D obstacleBelow;
	private Collider2D obstacleAbove;
	Collider2D obstacleToTheLeft;
	Collider2D obstacleToTheRight;
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
	bool _isTryingToStickToCeiling = false;
	public void SetIsTryingToStickToCeiling(bool isTryingToStickToCeiling) {
		_isTryingToStickToCeiling = isTryingToStickToCeiling;
	}
	bool IsStuckToCeiling() {
		return _isTryingToStickToCeiling && IsOnCeiling();
	}
	/// Constructor and Methods ///
	public PhysicsEntity(Transform transform, float height = 0.5f, float width = 0.5f) {
		this.transform = transform;
		this.height = height;
		this.width = width;
		this.gravityAcceleration = DEFAULT_GRAVITY;
		// Ensure that first collision check isn't from default positions (0, 0) to starting positions
		CacheSensorPixels(transform.position);
	}

	// Called by the component that this entity simulates the physics for
	// 	Should be called in the FixedUpdate() method of that MonoBehaviour
	//	Therefore, should run every physics update, every ~0.02 seconds
	public void Update () {
		HandleGravity();
		ApplyFriction();
		// Add velocity from moving obstacles nearby
		// Note: movingObstacleVelocity is currently only in the y dimension as there do not currently exist
		// 	any objects that can move the player horizontally
		movingObstacleVelocity = GetMovingObstacleVelocity();
		Vector2 attemptedDisplacement = new Vector2(velocityX, velocityY) * Time.deltaTime;
		// Don't multiply movingObstacleVelocity by Time.deltaTime
		//	because it is calculated based on displacement since last frame
		attemptedDisplacement += new Vector2(0, movingObstacleVelocity);
		// Store scaled up version of movingObstacleVelocity in velocityY
		// 	to incorporate it's changes in future frames
		velocityY += movingObstacleVelocity / Time.deltaTime;
		// Store attempted new position
		Vector2 newPosition = (Vector2)transform.position + attemptedDisplacement;
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
		if (applyGravity) {
			// Apply Gravity
			velocityY += IsStuckToCeiling() ? -gravityAcceleration : gravityAcceleration;
		}
	}

	void ApplyFriction() {
		// Apply horizontal friction
		if (applyGravity && _isOnGround) {
			velocityX /= DEFAULT_FRICTION_DENOMINATOR;
		}
		// Apply vertical friction
		// TODO: if moving towards wall
		if (IsOnWall()) {
			velocityY /= DEFAULT_FRICTION_DENOMINATOR;
		}
	}

	float GetMovingObstacleVelocity() {
		float relativeFloorVelocity = GetRelativeFloorVelocity();
		float relativeCeilingVelocity = GetRelativeCeilingVelocity();
		return relativeFloorVelocity + relativeCeilingVelocity;
	}

	float GetRelativeFloorVelocity() {
		float relativeFloorVelocity = 0;
		// Check if obstacle below has moved
		// Note that obstacleBelow is leftover from last frame
		if (obstacleBelow != null) {
			relativeFloorVelocity = GetKinematicObstacleVelocity(obstacleBelow.gameObject) - getVerticalVelocity();
			if (relativeFloorVelocity < 0) {
				relativeFloorVelocity = 0;
			}
		}
		return relativeFloorVelocity;
	}

	float GetRelativeCeilingVelocity() {
		float relativeCeilingVelocity = 0;
		// Check if obstacle above has moved
		if (obstacleAbove != null) {
			relativeCeilingVelocity = GetKinematicObstacleVelocity(obstacleAbove.gameObject) - getVerticalVelocity();
			if (relativeCeilingVelocity > 0) {
				relativeCeilingVelocity = 0;
			}
		}
		return relativeCeilingVelocity;
	}

	float getVerticalVelocity() {
		// Get the current velocity in the y dimension this frame
		// 	Note that this value changes over the course of the update cycle as
		//	additional factors are considered and added to velocityY
		return (velocityY + inputVelocityY) * Time.deltaTime;
	}

	Vector2 ResolveCollisionsAt(Vector2 newPosition) {
		// TODO: may be required to change the order of collision check and resolution
		// TODO:	based off of velocity, as currently an entity travelling upward too fast
		// TODO:	will check below first, which could result in a 'below' collision with the
		// TODO:	ceiling, teleporting the entity outside the map.
		// TODO:	Currently ordered this way because gravity can more easily surpass this limit
		// Always check for relevant collisions at the most recent version of newPosition
		CheckCollisionsBelow(newPosition);
		newPosition = ResolveCollisionsBelow(newPosition);
		CheckCollisionsAbove(newPosition);
		newPosition = ResolveCollisionsAbove(newPosition);
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
		// Set "is on ceiling" sensor to true whether we need to take it's velocity or not
		// 	this lets us stick to a ceiling that's ascending faster than we are, for example
		_isOnCeiling = obstacleAbove != null;
		if (_isOnCeiling && ShouldCollideWithObstacleAbove()) {
			// Set velocityY to the speed of the obstacle above
			velocityY = GetKinematicObstacleVelocity(obstacleAbove.gameObject) / Time.deltaTime;
			// Position self directly below the obstacle
			obstacleHeight = obstacleAbove.transform.localScale.y / 2;
			newPosition.y = obstacleAbove.transform.position.y - obstacleHeight - height;
		}
		return newPosition;
	}
	Vector2 ResolveCollisionsBelow(Vector2 newPosition) {
		float obstacleHeight;
		// Set "is on ground" sensor to true whether we need to take it's velocity or not
		// 	this lets us jump off a platform that's descending faster than we are, for example
		_isOnGround = obstacleBelow != null;
		if (_isOnGround && ShouldCollideWithObstacleBelow()) {
			// Set velocityY to the speed of the platform below
			velocityY = GetKinematicObstacleVelocity(obstacleBelow.gameObject) / Time.deltaTime;
			// Position self directly above the obstacle
			obstacleHeight = obstacleBelow.transform.localScale.y / 2;
			newPosition.y = obstacleBelow.transform.position.y + obstacleHeight + height;
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
		// Return true unless entity is moving down faster than ceiling is (e.g. falling away from it);
		return getVerticalVelocity() >= GetKinematicObstacleVelocity(obstacleAbove.gameObject);
	}
	bool ShouldCollideWithObstacleBelow() {
		// Return true unless our upward velocity is higher than the obstacle below's upward velocity
		return GetKinematicObstacleVelocity(obstacleBelow.gameObject) >= getVerticalVelocity();
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

	float GetKinematicObstacleVelocity(GameObject obstacle) {
		KinematicPhysicsEntity obstaclePhysicsEntity = obstacle.GetComponent<KinematicPhysicsEntity>();
		return obstaclePhysicsEntity == null ? 0 : obstaclePhysicsEntity.GetVelocity().y;
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
