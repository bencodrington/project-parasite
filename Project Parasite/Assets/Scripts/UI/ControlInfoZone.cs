using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControlInfoZone : TriggerZone
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
        RECALL_ORB,
        WALL_CLING
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
        { ControlType.RECALL_ORB,   2 },
        { ControlType.WALL_CLING,   3 }
    };
    
    #endregion

    protected override void OnTrigger() {
        UiManager.Instance.ActivateControlAtIndex(typeToGameObjectIndexMap[controlType], true);
    }

}
