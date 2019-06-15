using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformPhysicsEntity : RaycastController
{
    struct PassengerMovement {
        public PassengerMovement(Character passenger, float pushY, bool shouldMoveBeforePlatform) {
            this.passenger = passenger;
            this.pushY = pushY;
            this.shouldMoveBeforePlatform = shouldMoveBeforePlatform;
        }
        public Character passenger;
        public float pushY;
        public bool shouldMoveBeforePlatform;
    }

    #region [Private Variables]
    
    // Used to identify which objects to move
    LayerMask passengerMask = Utility.GetLayerMask("character");
    // A list of passengers and how far they should be moved this frame
    List<PassengerMovement> passengerMovements;
    
    #endregion

    #region [Public Methods]

    public PlatformPhysicsEntity(Transform transform, float height, float width) : base(transform, height, width) {}

    public void Update(float velocityY) {
        if (velocityY == 0) { return; }
        UpdateRayCastOrigins(transformPosition);
        passengerMovements = CalculatePassengerMovements(velocityY);
        MovePassengers(passengerMovements, true);
        // Move the platform
        transformPosition += velocityY * Vector2.up;
    }

    // Needs to be called after the platform gameObject's transform has moved
    public void AfterUpdate() {
        // Cast rays to identify passengers from the platform's new position
        UpdateRayCastOrigins(transformPosition);
        // Find passengers and move them
        MovePassengers(passengerMovements, false);
    }
    
    #endregion

    #region [Private Methods]
    
    List<PassengerMovement> CalculatePassengerMovements(float velocityY) {
        // Keep track of passengers that have been moved this frame, so we don't move them
        //  once for each ray that hits them
        HashSet<Transform> movedPassengers = new HashSet<Transform>();
        List<PassengerMovement> passengerMovements = new List<PassengerMovement>();
        float directionY = Mathf.Sign(velocityY);
        // Rays are cast from SKIN_WIDTH within the entity
        float rayLength = Mathf.Abs(velocityY) + SKIN_WIDTH;
        Vector2 rayOrigin;
        RaycastHit2D[] hits;
        // Player on top, platform moving upwards
        if (velocityY != 0 && directionY == 1) {
            for (int i = 0; i < VERTICAL_RAY_COUNT; i++) {
                // Always check above, spread the rays out along the width of the entity
                rayOrigin = rayCastOrigins.topLeft + Vector2.right * (i * verticalRaySpacing);
                // Cast each ray
                hits = Physics2D.RaycastAll(rayOrigin, Vector2.up, rayLength, passengerMask);
                // Draw visuals for debugging
                if (MatchManager.Instance.GetDebugMode()) {
                    // Draw ray origin
                    Debug.DrawLine(rayOrigin + Vector2.right * -0.01f, rayOrigin + Vector2.right * 0.01f, Color.blue);
                    // Draw ray we're actually firing
                    Debug.DrawRay(rayOrigin, Vector2.up * rayLength, Color.red);
                }
                foreach (RaycastHit2D hit in hits) {
                    if (!movedPassengers.Contains(hit.transform)) {
                        float pushY = velocityY - (hit.distance - SKIN_WIDTH);
                        passengerMovements.Add(GetPassengerMovement(hit.transform, pushY));
                        movedPassengers.Add(hit.transform);
                    }
                }
            }
        }
        // Player on top, platform moving downwards
        else if (directionY == -1) {
            // Just look a very tiny amount above the platform
            rayLength = 2 * SKIN_WIDTH;
            for (int i = 0; i < VERTICAL_RAY_COUNT; i++) {
                // Always check up, spread the rays out along the width of the entity
                rayOrigin = rayCastOrigins.topLeft + Vector2.right * (i * verticalRaySpacing);
                // Cast each ray upwards
                hits = Physics2D.RaycastAll(rayOrigin, Vector2.up, rayLength, passengerMask);
                // Draw visuals for debugging
                if (MatchManager.Instance.GetDebugMode()) {
                    // Draw ray origin
                    Debug.DrawLine(rayOrigin + Vector2.right * -0.01f, rayOrigin + Vector2.right * 0.01f, Color.blue);
                    // Draw ray we're actually firing
                    Debug.DrawRay(rayOrigin, Vector2.up * directionY * rayLength, Color.red);
                }
                foreach (RaycastHit2D hit in hits) {
                    if (!movedPassengers.Contains(hit.transform)) {
                        float pushY = velocityY + (hit.distance - SKIN_WIDTH);
                        passengerMovements.Add(GetPassengerMovement(hit.transform, pushY));
                        movedPassengers.Add(hit.transform);
                    }
                }
            }
        }
        return passengerMovements;
    }

    PassengerMovement GetPassengerMovement(Transform colliderTransform, float pushY) {
        Character passenger = colliderTransform.GetComponentInParent<Character>();
        // If pushY > 0, i.e. we're moving upwards, we need to move the passenger before the
        //  platform to ensure that the platform doesn't move above it, blocking the passenger
        //  from staying on top of it
        bool shouldMoveBeforePlatform = pushY > 0;
        return new PassengerMovement(passenger, pushY, shouldMoveBeforePlatform);
    }

    void MovePassengers(List<PassengerMovement> passengerMovements, bool isBeforePlatfromMoves) {
        foreach(PassengerMovement movement in passengerMovements) {
            if (movement.shouldMoveBeforePlatform == isBeforePlatfromMoves) {
                MovePassenger(movement);
            }
        }
    }

    void MovePassenger(PassengerMovement passengerMovement) {
        passengerMovement.passenger.Move(Vector2.up * passengerMovement.pushY);
    }
    
    #endregion

}
