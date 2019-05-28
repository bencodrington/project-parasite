using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HunterAiInputSource : InputSource
{

    #region [Private Variables]
    
    bool transitioningState = false;
    bool isPlacingOrbs = false;
    bool modifiedOrbLastFrame = false;
    OrbSetData orbData;
    int numOrbsInData;
    int numOrbsPlaced = 0;
    
    #endregion

    #region [Public Methods]

    public HunterAiInputSource() : base() {
        orbData = MatchManager.Instance.hunterAiOrbData;
        numOrbsInData = 0;
        foreach (OrbSetData.OrbChain chain in orbData.orbChains) {
            numOrbsInData += chain.positions.Length;
        }
    }

    public override void UpdateInputState() {
        base.UpdateInputState();
        if (!transitioningState && Random.Range(0, 200) < 1) {
            ToggleOrbPlacement();
        } else if (transitioningState) {
            if (isPlacingOrbs) {
                TryPlacingOrb();
            } else {
                TryRecallingOrb();
            }
        }
    }

    #endregion

    #region [Private Methods]

    void ToggleOrbPlacement() {
        // This should only be called when orbs are fully placed or fully recalled
        if (numOrbsPlaced == numOrbsInData) {
            StartRecallingOrbs();
        } else {
            StartPlacingOrbs();
        }
    }

    void StartRecallingOrbs() {
        transitioningState = true;
        isPlacingOrbs = false;
        modifiedOrbLastFrame = false;
        TryRecallingOrb();
    }

    void TryRecallingOrb() {
        // Alternate clicks every two frames so that it's
        //  not interpreted as holding the mouse down
        if (modifiedOrbLastFrame) { 
            modifiedOrbLastFrame = false;
            return;
        }
        RecallOrb();
        numOrbsPlaced--;
        if (numOrbsPlaced == 0) {
            transitioningState = false;
        } 
    }

    void RecallOrb() {
        state.keyState[Key.action2] = true;
        modifiedOrbLastFrame = true;
    }

    void StartPlacingOrbs() {
        transitioningState = true;
        isPlacingOrbs = true;
        modifiedOrbLastFrame = false;
        TryPlacingOrb();
    }

    void TryPlacingOrb() {
        // Alternate clicks every two frames so that it's
        //  not interpreted as holding the mouse down
        if (modifiedOrbLastFrame) { 
            modifiedOrbLastFrame = false;
            return;
        }
        PlaceOrb(numOrbsPlaced);
        numOrbsPlaced++;
        if (numOrbsPlaced == numOrbsInData) {
            transitioningState = false;
        } 
    }

    void PlaceOrb(int orbIndex) {
        int i = 0;
        foreach (OrbSetData.OrbChain chain in orbData.orbChains) {
            foreach (Vector2 position in chain.positions) {
                if (i == orbIndex) {
                    // Point AI's 'mouse' at the orb position
                    state.mousePosition = position;
                    // 'Click'
                    state.keyState[Key.action1] = true;
                    modifiedOrbLastFrame = true;
                    return;
                }
                i++;
            }
        }
    }
    
    #endregion
}
