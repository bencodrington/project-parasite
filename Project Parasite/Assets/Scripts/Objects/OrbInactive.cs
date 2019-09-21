using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrbInactive : MonoBehaviour
{
    #region [Private Variables]

    Vector2 destination;
    float remainingLifetime;
    bool isMoving = false;
    
    #endregion

    #region [Public Methods]
    
    public void StartMoving(Vector2 newDestination, float lifetime) {
        // Set it
        destination = newDestination;
        remainingLifetime = lifetime;
        // Start moving that way
        isMoving = true;
        if (lifetime > 0) {
            float playbackSpeed = 1 / lifetime;
            GetComponent<Animator>().speed = playbackSpeed;
        }
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
        if (isMoving) {
            transform.position = Vector2.Lerp(transform.position, destination, 0.5f);
        }
    }
    
    #endregion
}
