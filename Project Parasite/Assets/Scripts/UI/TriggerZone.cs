using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class TriggerZone : MonoBehaviour
{

    #region [Private Variables]

    Vector2 bottomLeft;
    Vector2 topRight;

    bool hasBeenTriggered = false;
    
    #endregion

    #region [Public Methods]

    public void OnUpdate(Vector2 characterPosition) {
        if (hasBeenTriggered) { return; }
        bool within = isPositionWithinBounds(characterPosition);
        if (within) {
            hasBeenTriggered = true;
            OnTrigger();
        }
    }

    public void Reset() {
        hasBeenTriggered = false;
    }
    
    #endregion

    #region [MonoBehaviour Callbacks]
    
    void Awake() {
        bottomLeft = transform.position - transform.localScale / 2;
        topRight = transform.position + transform.localScale / 2;
    }
    
    #endregion

    protected abstract void OnTrigger();

    #region [Private Methods]
    
    bool isPositionWithinBounds(Vector2 position) {
        return bottomLeft.x < position.x
            && position.x < topRight.x
            && bottomLeft.y < position.y
            && position.y < topRight.y;
    }
    
    #endregion

}
