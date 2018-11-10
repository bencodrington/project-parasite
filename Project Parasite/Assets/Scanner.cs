using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Scanner : NetworkBehaviour {

	public SpriteRenderer spriteRenderer;
    private Vector2 ceilingPoint;
    private float MAX_DISTANCE = 24;
    private int parasiteLayerMask;
    private int npcLayerMask;

    public Color detectingParasiteColour;
    public Color restingColour;

	void Start () {
        parasiteLayerMask = 1 << LayerMask.NameToLayer("Parasites");
        npcLayerMask = 1 << LayerMask.NameToLayer("NPCs");
        if (isServer) {
            int obstacleLayerMask = 1 << LayerMask.NameToLayer("Obstacles");
            // Get coordinate of ceiling above scanner
            RaycastHit2D ceiling = Physics2D.Raycast((Vector2)transform.position + new Vector2(0, 0.1f), Vector2.up, MAX_DISTANCE, obstacleLayerMask);
            ceilingPoint = ceiling.point;
            float distanceToCeiling = ceilingPoint.y - transform.position.y;
            // Set beam sprite to extend to the maximum range
            RpcSetBeamSpriteScale(distanceToCeiling);
        }
	}

	void FixedUpdate () {
        if (!isServer) { return; }
		// Check area between scanner and ceiling above
        Collider2D parasite = Physics2D.OverlapArea(transform.position, ceilingPoint, parasiteLayerMask);
        if (parasite != null) {
            IsDetectingParasite(true);
            RpcIsDetectingParasite(true);
            return;
        }
        // Check for infected NPCs
        Collider2D[] npcColliders = Physics2D.OverlapAreaAll(transform.position, ceilingPoint, npcLayerMask);
        NonPlayerCharacter npc;
        foreach (Collider2D npcCollider in npcColliders) {
            npc = npcCollider.transform.parent.GetComponentInChildren<NonPlayerCharacter>();
            if (npc.isInfected) {
                IsDetectingParasite(true);
                RpcIsDetectingParasite(true);
                return;
            }
        }
        IsDetectingParasite(false);
        RpcIsDetectingParasite(false);
	}

    void IsDetectingParasite(bool isAlerted) {
        if (isAlerted) {
            spriteRenderer.color = detectingParasiteColour;
        } else {
            spriteRenderer.color = Color.Lerp(spriteRenderer.color, restingColour, 0.002f);
        }
    }

    // ClientRpc
    [ClientRpc]
    void RpcIsDetectingParasite(bool isAlerted) {
        if (isServer) { return; }
        IsDetectingParasite(isAlerted);
    }

    [ClientRpc]
    void RpcSetBeamSpriteScale(float distanceToCeiling) {
        Debug.Log(distanceToCeiling);
        spriteRenderer.transform.localScale = new Vector2(spriteRenderer.transform.localScale.x, distanceToCeiling);
        spriteRenderer.transform.position = new Vector2(spriteRenderer.transform.position.x, transform.position.y + distanceToCeiling / 2);
    }
}
