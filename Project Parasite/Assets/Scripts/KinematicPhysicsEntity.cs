using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KinematicPhysicsEntity : MonoBehaviour {

    Vector2 positionLastFrame;
    Vector2 displacementSinceLastFrame;

    public void PhysicsUpdate() {
        displacementSinceLastFrame = (Vector2)transform.position - positionLastFrame;
        // if (displacementSinceLastFrame != Vector2.zero) {
        //     Debug.Log(displacementSinceLastFrame.y);
        // }
        positionLastFrame = transform.position;
    }

    public Vector2 GetVelocity() {
        return displacementSinceLastFrame;
    }

}
