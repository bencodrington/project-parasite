using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HunterAiInputSource : InputSource
{
    bool orbsHaveBeenPlaced = false;

    #region [Public Methods]

    public override void UpdateInputState() {
        base.UpdateInputState();
        if (Random.Range(0, 200) < 1) {
            ToggleOrbPlacement();
        }
    }

    #endregion

    #region [Private Methods]

    void ToggleOrbPlacement() {
        if (orbsHaveBeenPlaced) {
            RecallOrbs();
        } else {
            PlaceOrbs();
        }
        orbsHaveBeenPlaced = !orbsHaveBeenPlaced;
    }

    void RecallOrbs() {
        Debug.Log("RECALL");
        // TODO:
    }

    void PlaceOrbs() {
        Debug.Log("PLACE");
        // TODO:
    }
    
    #endregion
}
