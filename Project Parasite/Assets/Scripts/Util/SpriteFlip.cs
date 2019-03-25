using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpriteFlip : MonoBehaviour
{
    float lastX;

    void Start() {
        lastX = transform.position.x;
    }

    void Update() {
        float newX = transform.position.x;
        if (newX > lastX) {
            // Moving right
            transform.eulerAngles = Vector3.zero;
        } else if (lastX > newX) {
            // Moving left
            transform.eulerAngles = new Vector3(0, -180, 0);
        }
        lastX = newX;
    }
}
