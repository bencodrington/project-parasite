using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HunterAiInputSource : InputSource
{

    #region [Private Variables]
    // How long to wait (in seconds) between placing/recalling each orb
    const float TIME_BETWEEN_ORBS = 0.3f;
    // The box size (in game units) centered around the hunter, in which it
    //  can detect a parasite
    Vector2 AWARENESS_ZONE_SIZE = new Vector2(16, 1);
    int PARASITE_LAYER_MASK = Utility.GetLayerMask(CharacterType.Parasite);
    
    // Used to determine if we are placing/recalling orbs at all, or just waiting
    bool transitioningState = false;
    // Used to determine whether we are placing orbs or recalling them
    bool isPlacingOrbs = false;
    // Whether we're in the cooldown period after placing or recalling an orb
    bool isWaiting = false;
    bool canSeeParasite = false;
    OrbSetData orbData;
    int numOrbsInData;
    int numOrbsPlaced = 0;

    // Used for the hack that ensures we are always facing left
    Transform spriteTransform;
    
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
        FaceLeft();
        LookForParasite();
        if (!isWaiting) {
            OnReadyToPerformNextAction();
        }
    }

    #endregion

    #region [Private Methods]

    void StartRecallingOrbs() {
        transitioningState = true;
        isPlacingOrbs = false;
        TryRecallingOrb();
    }

    void TryRecallingOrb() {
        RecallOrb();
        numOrbsPlaced--;
        if (numOrbsPlaced == 0) {
            transitioningState = false;
        }
        Wait();
    }

    void RecallOrb() {
        state.keyState[Key.action2] = true;
    }

    void StartPlacingOrbs() {
        transitioningState = true;
        isPlacingOrbs = true;
        TryPlacingOrb();
    }

    void TryPlacingOrb() {
        PlaceOrb(numOrbsPlaced);
        numOrbsPlaced++;
        if (numOrbsPlaced == numOrbsInData) {
            transitioningState = false;
        }
        Wait();
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
                    return;
                }
                i++;
            }
        }
    }

    void LookForParasite() {
        // Don't interrupt placing/recalling orbs with a change of state
        if (isWaiting || (numOrbsPlaced != numOrbsInData && numOrbsPlaced != 0)) { return; }
        bool oldCanSeeParasite = canSeeParasite;
        canSeeParasite = (bool)Physics2D.OverlapBox(owner.transform.position, AWARENESS_ZONE_SIZE, 0, PARASITE_LAYER_MASK);
        if (canSeeParasite && !oldCanSeeParasite) {
            StartPlacingOrbs();
        } else if (!canSeeParasite && oldCanSeeParasite) {
            StartRecallingOrbs();
        }
    }

    void Wait() {
        isWaiting = true;
        MatchManager.Instance.StartCoroutine(Utility.WaitXSeconds(TIME_BETWEEN_ORBS, () => { isWaiting = false; }));
    }

    void OnReadyToPerformNextAction() {
        // transitioningState is set to false when all orbs are placed or all are recalled
        if (transitioningState) {
            if (isPlacingOrbs) {
                TryPlacingOrb();
            } else {
                TryRecallingOrb();
            }
        }
    }

    // CLEANUP: this is a relatively hacky way of making sure the only hunter AI currently in the game
    //  is always facing left
    void FaceLeft() {
        spriteTransform = owner.transform.GetComponentInChildren<SpriteTransform>().transform;
        if (spriteTransform.eulerAngles.y == 0) {
            state.keyState[Key.left] = true;
        }
    }
    
    #endregion
}
