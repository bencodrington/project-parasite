using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControlInfoZone : MonoBehaviour
{
    #region [Public Variables]
    
    public enum ControlType {
        CLIMB,
        JUMP,
        POUNCE,
        INFECT,
        EJECT,
        EXECUTE,
        HUNTER_JUMP,
        PLACE_ORB,
        RECALL_ORB
    }

    public ControlType controlType;
    
    #endregion

    #region [Private Variables]

    static Dictionary<ControlType, int> typeToGameObjectIndexMap = new Dictionary<ControlType, int>() {
        { ControlType.CLIMB,        0 },
        { ControlType.JUMP,         1 },
        { ControlType.POUNCE,       2 },
        { ControlType.INFECT,       3 },
        { ControlType.EJECT,        0 },
        { ControlType.EXECUTE,      1 },
        { ControlType.HUNTER_JUMP,  0 },
        { ControlType.PLACE_ORB,    1 },
        { ControlType.RECALL_ORB,   2 }
    };
    
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
            UiManager.Instance.ActivateControlAtIndex(typeToGameObjectIndexMap[controlType]);
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

    #region [Private Methods]
    
    bool isPositionWithinBounds(Vector2 position) {
        return bottomLeft.x < position.x
            && position.x < topRight.x
            && bottomLeft.y < position.y
            && position.y < topRight.y;
    }
    
    #endregion
}
