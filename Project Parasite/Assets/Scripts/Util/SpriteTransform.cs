using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpriteTransform : MonoBehaviour
{
    // The coordinates we were at last frame
    float lastX;
    float lastY;
    // Keep track of which direction we're rotated to face
    //  e.g. as a parasite, which direction the wall is we're clinging to
    Utility.Directions direction = Utility.Directions.Null;
    // Whether our sprite is currently flipped
    bool isFlipped = false;
    // Used to prevent flipping when 'rubber-banding'
    bool canFlipThisFrame = true;
    // A buffer to stop float rounding from making us flip wildly
    float minDistanceBeforeFlipping = 0.01f;

    // The sprites local position relative to its parent object at the start of the game
    //  i.e. what's defined in the inspector/prefab
    Vector2 localOffset;

    PhysicsEntity physicsEntity;

    #region [Public Methods]

    public void SetTargetPhysicsEntity(PhysicsEntity newPhysicsEntity) {
        physicsEntity = newPhysicsEntity;
        UpdateCoords();
    }
    
    public void SetRotateDirection(Utility.Directions newDirection) {
        if (direction == newDirection) { return; }
        direction = newDirection;
        // The origin of rotation is not at the center of the object, but rather the
        //  center of the sprite, so we need to account for that by changing its offset
        SetOffset();
        transform.eulerAngles = new Vector3(0, 0, Utility.DirectionToAngle(newDirection));
        isFlipped = false;
    }

    public void DontFlipThisFrame() {
        canFlipThisFrame = false;
    }
    
    #endregion

    #region [MonoBehaviour Callbacks]

    void Start() {
        localOffset = transform.localPosition;
    }

    void Update() {
        if (physicsEntity == null) { return; }
        if (canFlipThisFrame && ShouldFlipSpriteHorizontally()) { FlipSpriteOnYAxis(); }
        if (canFlipThisFrame && ShouldFlipSpriteVertically()) { FlipSpriteOnXAxis(); }
        UpdateCoords();
    }
    
    #endregion

    #region [Private Methods]
    
    bool ShouldFlipSpriteHorizontally() {
        float newX = physicsEntity.transformPosition.x;
        switch (direction) {
            case Utility.Directions.Up:
                return ((!isFlipped && isSignificantlyMoreThan(newX, lastX))
                    || (isFlipped && isSignificantlyLessThan(newX, lastX)));
            case Utility.Directions.Down:
            case Utility.Directions.Null:
                return ((!isFlipped && isSignificantlyLessThan(newX, lastX))
                    || (isFlipped && isSignificantlyMoreThan(newX, lastX)));
        }
        return false;
    }
    
    bool ShouldFlipSpriteVertically() {
        float newY = physicsEntity.transformPosition.y;
        switch (direction) {
            case Utility.Directions.Right:
                return ((!isFlipped && isSignificantlyLessThan(newY,lastY))
                    || (isFlipped && isSignificantlyMoreThan(newY, lastY)));
            case Utility.Directions.Left:
                return ((!isFlipped && isSignificantlyMoreThan(newY, lastY))
                    || (isFlipped && isSignificantlyLessThan(newY, lastY)));
        }
        return false;
    }

    // Return true iff coord1 is less than coord2 by at least minDistanceBeforeFlipping
    bool isSignificantlyLessThan(float coord1, float coord2) {
        return (coord1 < coord2 - minDistanceBeforeFlipping);
    }
    bool isSignificantlyMoreThan(float coord1, float coord2) {
        return (coord1 > coord2 + minDistanceBeforeFlipping);
    }

    void FlipSpriteOnYAxis() {
        float newYRotation = isFlipped ? 0 : 180;
        transform.eulerAngles = new Vector3(0, newYRotation, Utility.DirectionToAngle(direction));
        isFlipped = !isFlipped;
    }

    void FlipSpriteOnXAxis() {
        float newXRotation = isFlipped ? 0 : 180;
        transform.eulerAngles = new Vector3(newXRotation, 0, Utility.DirectionToAngle(direction));
        isFlipped = !isFlipped;
    }

    void UpdateCoords() {
        lastX = physicsEntity.transformPosition.x;
        lastY = physicsEntity.transformPosition.y;
        // Frame is over, if we couldn't flip this frame we can again
        canFlipThisFrame = true;
    }

    void SetOffset() {
        switch (direction) {
            case Utility.Directions.Left:
                // CLEANUP: Magic number on next line is (Parasite width - height) / 2 
                transform.localPosition = new Vector2(localOffset.y - 0.25f, localOffset.x);
                break;
            case Utility.Directions.Right:
                // CLEANUP: Magic number on next line is (Parasite width - height) / 2 
                transform.localPosition = new Vector2(-localOffset.y + 0.25f, localOffset.x);
                break;
            case Utility.Directions.Up:
                transform.localPosition = new Vector2(localOffset.x, -localOffset.y);
                break;
            default:
                transform.localPosition = localOffset;
                break;
        }
    }
    
    #endregion

}
