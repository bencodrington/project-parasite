using UnityEngine;
using UnityEditor;

public class PhysicsEntity : RaycastController {
	
	struct DirectionInfo {
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
	// The directions in which we're currently colliding with walls
	DirectionInfo collisionInfo;
	// The directions in which we're currently trying to stick to walls
	DirectionInfo isTryingToStick;

	/// Objects ///
	// Keep track of obstacles for determining how far they've moved since last frame
	// 	so that this object can absorb their momentum (elevators, etc.)
	Collider2D obstacleBelow;
	Collider2D obstacleAbove;
	Collider2D obstacleToTheLeft;
	Collider2D obstacleToTheRight;
	// The old position of the entity, used for collision checking
	Vector2 oldPixelBelow;
	Vector2 oldPixelAbove;
	Vector2 oldPixelToTheLeft;
	Vector2 oldPixelToTheRight;

	#endregion

	// Public-facing methods for determining the state of the entity
	public bool IsOnGround()	{ return collisionInfo.below; 	}
	public bool IsOnCeiling()	{ return collisionInfo.above; 	}
	public bool IsOnLeftWall()	{ return collisionInfo.left;  	}
	public bool IsOnRightWall()	{ return collisionInfo.right; 	}
	public bool IsOnWall() 		{ return IsOnLeftWall() || IsOnRightWall(); }
	public bool IsAscending() 	{ return velocityY > 0;			}

	#region [Public Methods]
	
	public PhysicsEntity(Transform transform, float height = 0.5f, float width = 0.5f) : base(transform, height, width) {
		this.gravityAcceleration = DEFAULT_GRAVITY;
	}

	public void Move(Vector2 displacement) {
		UpdateRayCastOrigins(transformPosition);
		// Pass 'true' flag to indicate that this movement was caused by a platform
		//  NOTE: if this function is ever updated to be more generic, that flag
		// 		may not always be true
		HandleVerticalCollisions(ref displacement, true);
		HandleHorizontalCollisions(ref displacement);
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
		AddVelocity(velocity.x, velocity.y);
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

	public void SetIsTryingToStickInDirection(Utility.Directions direction, bool resetFirst = false) {
		if (resetFirst) {
			isTryingToStick.Reset();
		}
		switch(direction) {
			case Utility.Directions.Up: 	isTryingToStick.above 	= true; break;
			case Utility.Directions.Down: 	isTryingToStick.below 	= true; break;
			case Utility.Directions.Left: 	isTryingToStick.left 	= true; break;
			case Utility.Directions.Right:  isTryingToStick.right 	= true; break;
			case Utility.Directions.Null:	isTryingToStick.Reset(); break;
		}
	}

	public Vector2 GetVelocity() {
		return new Vector2(velocityX, velocityY);
	}

	public void SetVelocity(Vector2 newVelocity) {
		velocityX = newVelocity.x;
		velocityY = newVelocity.y;
	}
	
	#endregion

	#region [Private Methods]
	
	void HandleVerticalCollisions(ref Vector2 attemptedDisplacement, bool isBeingMovedByPlatform = false) {
		float directionY = Mathf.Sign(attemptedDisplacement.y);
		float rayLength = Mathf.Abs(attemptedDisplacement.y) + SKIN_WIDTH;
		Vector2 rayOrigin;
		RaycastHit2D hit;
		bool hasBeenShunted = false;
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
				// Special case: we're being moved UP by a platform, colliding with a ceiling
				// 	By design, this should never be a full ceiling, but rather a corner.
				//	This means there should be a free space to one side of the entity.
				if (!hasBeenShunted && isBeingMovedByPlatform && directionY == 1) {
					attemptedDisplacement.x += GetShuntDistance(rayLength);
					// Don't shunt for more than one ray
					hasBeenShunted = true;
				}
				// Don't let entity move past the collision
				attemptedDisplacement.y = (hit.distance - SKIN_WIDTH) * directionY;
				// Don't bother checking farther than this collision for subsequent rays
				rayLength = hit.distance;
				// If we were travelling up, collision was above us
				collisionInfo.above = directionY == 1;
				collisionInfo.below = directionY == -1;
				// If we've input a jump action while on a platform
				// 	setting velocityY to 0 now will stop the jump from ever being applied
				if (!isBeingMovedByPlatform) {
					velocityY = 0;
				}
			}
		}
	}

	float GetShuntDistance(float rayLength) {
		Vector2 rayOrigin;
		RaycastHit2D hit;
		bool shuntRight = false;
		int indexOfClearRay;
		float shuntDistance = 0;
		for (int i = 0; i < VERTICAL_RAY_COUNT; i++) {
			rayOrigin = rayCastOrigins.topLeft + (Vector2.right * i * verticalRaySpacing);
			hit = Physics2D.Raycast(rayOrigin, Vector2.up, rayLength, OBSTACLE_MASK);
			if (i == 0) {
				// If the first ray hits the ceiling, we'll be shunting in the other direction (RIGHT)
				shuntRight = hit.collider != null;
			} else if (!shuntRight && hit) {
				// First ray didn't hit the ceiling, but a subsequent ray did
				// 	Therefore the previous ray was the last ray that didn't hit the ceiling
				// SHUNT LEFT
				indexOfClearRay = i - 1;
				shuntDistance = GetTopRayCastOriginAtIndex(indexOfClearRay).x - GetTopRayCastOriginAtIndex(VERTICAL_RAY_COUNT - 1).x - SKIN_WIDTH;
				return shuntDistance;
			} else if (shuntRight && !hit) {
				// First ray hit the ceiling, but a subsequent ray didn't
				// 	Therefore this ray is the first ray that didn't hit the ceiling
				// SHUNT RIGHT
				indexOfClearRay = i;
				shuntDistance = GetTopRayCastOriginAtIndex(indexOfClearRay).x - GetTopRayCastOriginAtIndex(0).x + SKIN_WIDTH;
				return shuntDistance;
			}
		}
		Debug.LogError("PhysicsEntity: GetShuntDistance(): Unable to find clear space to shunt to.");
		return 0;
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
		if (IsStickingToCeiling()){
			// Keep attempting to move up so that the entity keeps colliding with the ceiling
			// 	so IsOnCeiling() keeps evaluating to true
			_velocityY -= gravityAcceleration;
		} else if (!IsStickingToSurface()) {
			_velocityY += gravityAcceleration;
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
		if (IsStickingToWall()) {
			// Only add vertical friction if character is sliding down a wall
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
		return IsOnGround() || IsStickingToCeiling();
	}

	bool IsStickingToSurface() {
		return IsStickingToCeiling()
			|| IsStickingToWall();
	}

	bool IsStickingToWall() {
		return (isTryingToStick.left && IsOnLeftWall())
			|| (isTryingToStick.right && IsOnRightWall());
	}

	bool IsStickingToCeiling() {
		return isTryingToStick.above && IsOnCeiling();
	}

	#endregion

	
}
