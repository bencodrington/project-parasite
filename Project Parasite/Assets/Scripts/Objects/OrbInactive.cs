using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrbInactive : MonoBehaviour
{
    #region [Private Variables]

    Vector2 destination;
    // Used when the orb is heading towards a moving target
    Transform destinationTransform;
    float remainingLifetime;
    bool isMoving = false;
    
    #endregion

    #region [Public Methods]
    
    public void SetDestinationAndStartMoving(Vector2 newDestination, float lifetime) {
        destination = newDestination;
        StartMoving(lifetime);
    }

    public void SetDestinationAndStartMoving(Transform newDestinationTransform, float lifetime) {
        destinationTransform = newDestinationTransform;
        StartMoving(lifetime);
    }
    
    #endregion

    #region [MonoBehaviour Callbacks]
    
    void Update() {
        if (remainingLifetime <= 0) {
            // Destroy self
            Destroy(gameObject);
            return;
        }
        remainingLifetime -= Time.deltaTime;
        Vector2 dest = destinationTransform != null ? (Vector2)destinationTransform.position : destination;
        if (isMoving) {
            transform.position = Vector2.Lerp(transform.position, dest, 0.5f);
        }
    }
    
    #endregion

    #region [Private Methods]
    
    void StartMoving(float lifetime) {
        remainingLifetime = lifetime;
        // Start moving that way
        isMoving = true;
        if (lifetime > 0) {
            float playbackSpeed = 1 / lifetime;
            GetComponent<Animator>().speed = playbackSpeed;
        }
    }
    
    #endregion
}
