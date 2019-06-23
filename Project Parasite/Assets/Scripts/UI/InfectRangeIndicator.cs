using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InfectRangeIndicator : RangeIndicator
{
    #region [Private Variables]
    
    float infectRadius;
    bool isInRange;
    Collider2D npcCollider;
    
    #endregion

    protected override int IndicatorCount => 3;

    #region [Public Variables]
    
    public void SetInfectRadius(float _infectRadius) {
        infectRadius = _infectRadius;
    }

    public Collider2D GetNpcCollider() { return npcCollider; }
    
    #endregion

    protected override bool ShouldShowIndicators() {
        npcCollider = Physics2D.OverlapCircle(transform.position, infectRadius, Utility.GetLayerMask(CharacterType.NPC));
        if (npcCollider != null) {
            SetTargetPosition(npcCollider.transform.position);
            return true;
        }
        return false;
    }
}
