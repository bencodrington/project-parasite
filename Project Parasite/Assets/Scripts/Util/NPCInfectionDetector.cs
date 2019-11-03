using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCInfectionDetector : MonoBehaviour {

    #region [Public Variables]
    
    public InfoScreen infoScreen;
    
    #endregion

    #region [Private Variables]
    
    Vector2 SIZE = new Vector2(3, 3);
    NonPlayerCharacter npc;
    
    #endregion

    #region [Public Methods]
    
    public void Reset() {
        npc = null;
        ScanForNPCs();
    }
    
    public void ScanForNPCs() {
        Collider2D coll = Physics2D.OverlapArea(transform.position,
										transform.position + new Vector3(SIZE.x, SIZE.y, 0),
										Utility.GetLayerMask(CharacterType.NPC));
        if (coll != null) {
            npc = (NonPlayerCharacter)Utility.GetCharacterFromCollider(coll);
        }
        if (coll == null || npc == null) {
            Debug.LogError("NPCInfectionDetector: ScanForNPCS: no NPCs found.");
        }
    }
    
    #endregion

    #region [MonoBehaviour Callbacks]

    void Update() {
        if (npc != null && npc.isInfected) {
            // NPC just got infected, let the associated info screen know
            infoScreen.StartPrinting();
            npc = null;
        }
    }
    
    #endregion
}
