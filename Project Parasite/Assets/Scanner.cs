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
    private bool isBeingTriggered = false;

    private Hunter owner;

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
            IsDetectingParasite();
            return;
        }
        // Check for infected NPCs
        Collider2D[] npcColliders = Physics2D.OverlapAreaAll(transform.position, ceilingPoint, npcLayerMask);
        NonPlayerCharacter npc;
        foreach (Collider2D npcCollider in npcColliders) {
            npc = npcCollider.transform.parent.GetComponentInChildren<NonPlayerCharacter>();
            if (npc.isInfected) {
                IsDetectingParasite();
                return;
            }
        }
        // If we reach this point, we're not currently detecting a parasite, so reset
        isBeingTriggered = false;
	}

    void Update() {
        // Fade back to resting colour
        spriteRenderer.color = Color.Lerp(spriteRenderer.color, restingColour, 0.120f * Time.deltaTime);
    }

    void IsDetectingParasite() {
        if (!isServer) { return; }
        // If this is not a new trigger, exit early
        if (isBeingTriggered) { return; }
        // First frame this has occured, notify clients
        isBeingTriggered = true;
        owner.RpcOnScannerTriggered(transform.position);
        RpcIsDetectingParasite();
    }

    public void SetOwner(Hunter owner) {
        this.owner = owner;
    }

    // ClientRpc
    [ClientRpc]
    void RpcIsDetectingParasite() {
        spriteRenderer.color = detectingParasiteColour;
    }

    [ClientRpc]
    void RpcSetBeamSpriteScale(float distanceToCeiling) {
        spriteRenderer.transform.localScale = new Vector2(spriteRenderer.transform.localScale.x, distanceToCeiling);
        spriteRenderer.transform.position = new Vector2(spriteRenderer.transform.position.x, transform.position.y + distanceToCeiling / 2);
    }
}
