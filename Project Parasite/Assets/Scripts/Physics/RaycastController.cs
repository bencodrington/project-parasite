using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RaycastController
{
	public struct RayCastOrigins {
		public Vector2 topLeft, topRight;
		public Vector2 bottomLeft, bottomRight;
	}

    #region [Public Variables]
	
	// The current in-world position of the physics entity
	// 	Separate from the object's transform.position as this
	// 	lets us accurately run the physics simulation while smoothly
	// 	interpolating the visual representation to the correct position
	public Vector2 transformPosition {get; protected set;}
    
    #endregion

    #region [Protected Variables]

    // Constants
	// Very small amount used to ensure we can detect surfaces we're flush against
	protected const float SKIN_WIDTH = 0.015f;
	// How many rays should be fired in each direction to detect obstacles
	// Each must be at least 2
	protected const int HORIZONTAL_RAY_COUNT = 4;
	protected const int VERTICAL_RAY_COUNT = 4;
	// The in-world coordinates of the bounds of this entity
	protected RayCastOrigins rayCastOrigins;
	// How far apart the rays are on each axis
	protected float horizontalRaySpacing;
	protected float verticalRaySpacing;
    // Hitbox demensions: 2*width by 2*height
    protected float width {get; private set;}
    protected float height {get; private set;}
    
    #endregion

    #region [Public Methods]
    
    public RaycastController(Transform transform, float height, float width) {
        this.transformPosition = transform.position;
        this.width = width;
        this.height = height;
		CalculateRaySpacing();
    }

	public Vector2 GetTopRayCastOriginAtIndex(int i) {
		return rayCastOrigins.topLeft + (Vector2.right * i * verticalRaySpacing);
	}
    
    #endregion

    #region [Protected Methods]
    
	protected void UpdateRayCastOrigins(Vector2 transformPosition) {
		rayCastOrigins.bottomLeft = transformPosition + new Vector2(-width + SKIN_WIDTH, -height + SKIN_WIDTH);
		rayCastOrigins.bottomRight = transformPosition + new Vector2(width - SKIN_WIDTH, -height + SKIN_WIDTH);
		rayCastOrigins.topLeft = transformPosition + new Vector2(-width + SKIN_WIDTH, height - SKIN_WIDTH);
		rayCastOrigins.topRight = transformPosition + new Vector2(width - SKIN_WIDTH, height - SKIN_WIDTH);
	}
    
    #endregion

    #region [Private Methods]

	void CalculateRaySpacing() {
		float heightWithoutSkin = (2 * height) - (2 * SKIN_WIDTH);
		float widthWithoutSkin = (2 * width) - (2 * SKIN_WIDTH);
		horizontalRaySpacing = heightWithoutSkin / (HORIZONTAL_RAY_COUNT - 1);
		verticalRaySpacing = widthWithoutSkin / (VERTICAL_RAY_COUNT - 1);
	}
    
    #endregion
}
