using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpriteTransform : MonoBehaviour
{
    float lastX;
    float lastY;
    Utility.Directions direction = Utility.Directions.Down;

    #region [Public Methods]
    
    public void SetRotateDirection(Utility.Directions newDirection) {
        direction = newDirection;
        transform.eulerAngles = new Vector3(0, transform.eulerAngles.y, Utility.DirectionToAngle(newDirection));
    }
    
    #endregion

    void Start() {
        lastX = transform.position.x;
        lastY = transform.position.y;
    }

    void Update() {
        float newX = transform.position.x;
        float newY = transform.position.y;
        if (newX > lastX) {
            // Moving right
            transform.eulerAngles = Vector3.zero;
        } else if (lastX > newX) {
            // Moving left
            transform.eulerAngles = new Vector3(0, -180, transform.eulerAngles.z);
        }
        lastX = newX;
        lastY = newY;
    }


}
