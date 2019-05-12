using UnityEngine;
using UnityEditor;

public class PhysicsEntity : RaycastController {
	
	public struct CollisionInfo {
		public bool above, below;
		public bool left, right;
	
		public void Reset() {
			above = below = false;
			left = right = false;
		}
	}

	#region [Private Variables]
	/// Constants ///
	int OBSTACLE_MASK = Utility.GetLayerMask("obstacle");
	const float DEFAULT_GRAVITY = -1f;
	// While on the ground, horizontal velocity is divided by this constant
	// 	a value of 1 indicates no friction
	//  a value of 2 indicates that horizontal velocity should be halved each physics update
	const float DEFAULT_FRICTION_DENOMINATOR = 1.25f;
	// How far above and below to check for floor to see if we're outside the map
	const float OUTSIDE_MAP_DISTANCE = 100f;
	// The (currently only vertical) velocity of kinematic objects affecting this entity
	//	e.g. elevators, etc.
	float movingObstacleVelocity;

	/// Velocity ///
	// Speed in units / second
	float velocityX = 0f;
	float velocityY = 0f;

	// Gravity increases by a rate of 1 unit/second per second
	float gravityAcceleration;

	// Maintain the velocity from movement input separately
	// 	this allows us to limit movement speed on its own
	float inputVelocityX = 0f;
	float inputVelocityY = 0f;
	public CollisionInfo collisionInfo;

	#endregion
	/// Objects ///
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
	public bool IsOnGround()	{ return collisionInfo.below; }
	public bool IsOnCeiling()	{ return collisionInfo.above; }
	public bool IsOnLeftWall()	{ return collisionInfo.left;  }
	public bool IsOnRightWall()	{ return collisionInfo.right; }
	public bool IsOnWall() { return IsOnLeftWall() || IsOnRightWall(); }
	public bool applyGravity = true;
	bool _isTryingToStickToCeiling = false;
	public void SetIsTryingToStickToCeiling(bool isTryingToStickToCeiling) {
		_isTryingToStickToCeiling = isTryingToStickToCeiling;
	}
	bool IsStuckToCeiling() {
		return _isTryingToStickToCeiling && IsOnCeiling();
	}

	#region [Public Methods]
	
	public PhysicsEntity(Transform transform, float height = 0.5f, float width = 0.5f) : base(transform, height, width) {
		this.gravityAcceleration = DEFAULT_GRAVITY;
	}

	public void Move(Vector2 displacement) {
		UpdateRayCastOrigins(transformPosition);
		// TODO: collision checks
		SetTransformPosition(transformPosition + displacement);
		UpdateRayCastOrigins(transformPosition);
	}

	// Called by the component that this entity simulates the physics for
	// 	Should be called in the FixedUpdate() method of that MonoBehaviour
	//	Therefore, should run every physics update, every ~0.02 seconds
	public void Update () {

		if (IsOutsideMap()) {
			Debug.LogError("PLAYER OUTSIDE MAP");
			transformPosition = Vector2.zero;
			velocityX = 0;
			velocityY = 0;
			ResetInputVelocity();
			return;
		}

		UpdateRayCastOrigins(transformPosition);
		velocityY = HandleGravity(velocityY);
		velocityY = HandleVerticalFriction(velocityY);
		velocityX = HandleHorizontalFriction(velocityX);

		Vector2 attemptedDisplacement = new Vector2(velocityX, velocityY) * Time.deltaTime;
		// Add velocity from character movement input
		attemptedDisplacement += new Vector2(inputVelocityX, inputVelocityY) * Time.deltaTime;

		collisionInfo.Reset();
		// Check for and resolve collisions
		UpdateRayCastOrigins(transformPosition);
		if (attemptedDisplacement.y != 0) {
			HandleVerticalCollisions(ref attemptedDisplacement);
		}
		if (attemptedDisplacement.x != 0) {
			HandleHorizontalCollisions(ref attemptedDisplacement);
		}
		// Set new position
		transformPosition += attemptedDisplacement;
		// Reset inputVelocity, as this should be manually controlled by the character each frame
		ResetInputVelocity();
		// Don't accumulate gravity if we're on the ground
		if (collisionInfo.below) {
			velocityY = 0;
		}
	}

	public void SetTransformPosition(Vector2 newPosition) {
		transformPosition = newPosition;
	}

	public void AddVelocity(float x, float y) {
		velocityX += x;
		velocityY += y;
	}
	public void AddVelocity(Vector2 velocity) {
		velocityX += velocity.x;
		velocityY += velocity.y;
	}

	public void AddInputVelocity(float velocityX, float velocityY) {
		inputVelocityX += velocityX;
		inputVelocityY += velocityY;
	}

	public void DebugDrawRayCastOrigins(float duration = 15) {
		Debug.DrawLine(rayCastOrigins.topLeft, rayCastOrigins.topRight, Color.yellow, duration);
		Debug.DrawLine(rayCastOrigins.topLeft, rayCastOrigins.bottomLeft, Color.yellow, duration);
		Debug.DrawLine(rayCastOrigins.topRight, rayCastOrigins.bottomRight, Color.yellow, duration);
		Debug.DrawLine(rayCastOrigins.bottomLeft, rayCastOrigins.bottomRight, Color.yellow, duration);
	}
	
	
	#endregion

	#region [Private Methods]
	
	void HandleVerticalCollisions(ref Vector2 attemptedDisplacement) {
		float directionY = Mathf.Sign(attemptedDisplacement.y);
		float rayLength = Mathf.Abs(attemptedDisplacement.y) + SKIN_WIDTH;
		Vector2 rayOrigin;
		RaycastHit2D hit;
		for (int i = 0; i < VERTICAL_RAY_COUNT; i++) {
			// Check above if we're moving up, check below if we're moving down
			rayOrigin = directionY == 1 ? rayCastOrigins.topLeft : rayCastOrigins.bottomLeft;
			// Spread the rays out along the width of the entity
			rayOrigin += Vector2.right * (i * verticalRaySpacing);
			// Cast each ray
			hit = Physics2D.Raycast(rayOrigin, Vector2.up * directionY, rayLength, OBSTACLE_MASK);
			// Visuals for debugging
			if (MatchManager.Instance.GetDebugMode()) {
				// Draw ray origin
				Debug.DrawLine(rayOrigin + Vector2.right * -0.01f, rayOrigin + Vector2.right * 0.01f, Color.blue);
				// Draw ray we're actually firing
				Debug.DrawRay(rayOrigin, Vector2.up * directionY * rayLength, Color.red);
			}
			if (hit) {
				// Don't let entity move past the collision
				attemptedDisplacement.y = (hit.distance - SKIN_WIDTH) * directionY;
				// Don't bother checking farther than this collision for subsequent rays
				rayLength = hit.distance;
				// If we were travelling up, collision was above us
				collisionInfo.above = directionY == 1;
				collisionInfo.below = directionY == -1;
				velocityY = 0;
			}
		}
	}

	void HandleHorizontalCollisions(ref Vector2 attemptedDisplacement) {
		float directionX = Mathf.Sign(attemptedDisplacement.x);
		float rayLength = Mathf.Abs(attemptedDisplacement.x) + SKIN_WIDTH;
		Vector2 rayOrigin;
		RaycastHit2D hit;
		for (int i = 0; i < HORIZONTAL_RAY_COUNT; i++) {
			// Check in the direction we're moving
			rayOrigin = directionX == 1 ? rayCastOrigins.bottomRight : rayCastOrigins.bottomLeft;
			// Spread the rays out along the height of the entity
			rayOrigin += Vector2.up * (i * horizontalRaySpacing);
			// Cast each ray
			hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, OBSTACLE_MASK);
			// Visuals for debugging
			if (MatchManager.Instance.GetDebugMode()) {
				// Draw ray origin
				Debug.DrawLine(rayOrigin + Vector2.up * -0.01f, rayOrigin + Vector2.up * 0.01f, Color.blue);
				// Draw ray we're actually firing
				Debug.DrawRay(rayOrigin, Vector2.right * directionX * rayLength, Color.red);

			}
			if (hit) {
				// Don't let entity move past the collision
				attemptedDisplacement.x = (hit.distance - SKIN_WIDTH) * directionX;
				// Don't bother checking farther than this collision for subsequent rays
				rayLength = hit.distance;
				// If we were travelling right, collision was to the right of us
				collisionInfo.right = directionX == 1;
				collisionInfo.left = directionX == -1;
				velocityX = 0;
			}
		}
	}
	
	bool IsOutsideMap() {
		Vector3 pos = transformPosition;
		// Check if there is an obstacle OUTSIDE_MAP_DISTANCE above or OUTSIDE_MAP_DISTANCE below us
		RaycastHit2D hit = Physics2D.Raycast(new Vector2(pos.x, pos.y - OUTSIDE_MAP_DISTANCE), Vector2.up, OUTSIDE_MAP_DISTANCE * 2, Utility.GetLayerMask("obstacle"));
		return !hit;
	}

	float HandleGravity(float _velocityY) {
		if (applyGravity) {
			// CLEANUP:
			_velocityY += IsStuckToCeiling() ? -gravityAcceleration : gravityAcceleration;
		}
		return _velocityY;
	}

	float HandleHorizontalFriction(float _velocityX) {
		if (ShouldApplyHorizontalFriction()) {
			_velocityX /= DEFAULT_FRICTION_DENOMINATOR;
		}
		return _velocityX;
	}

	float HandleVerticalFriction(float _velocityY) {
		if (IsMovingIntoWall()) {
			_velocityY /= DEFAULT_FRICTION_DENOMINATOR;
			// Snap to 0
			if (Mathf.Abs(_velocityY) < 0.001) { _velocityY = 0; }
		}
		return _velocityY;
	}

	void ResetInputVelocity() {
		inputVelocityX = 0;
		inputVelocityY = 0;
	}
	bool isMovingLeft() {
		return velocityX + inputVelocityX < 0;
	}
	bool isMovingRight() {
		return velocityX + inputVelocityX > 0;
	}

	bool IsMovingIntoWall() {
		return (IsOnLeftWall() && isMovingLeft()) || (IsOnRightWall() && isMovingRight());
	}

	bool ShouldApplyHorizontalFriction() {
		return (applyGravity && IsOnGround()) || IsStuckToCeiling();
	}

	#endregion

	
}
