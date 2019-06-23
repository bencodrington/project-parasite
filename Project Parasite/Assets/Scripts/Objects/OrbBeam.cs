using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrbBeam : MonoBehaviour {

	float energyRadius = 2f;
	float energyForce = 10f;

	Vector2 startPoint;
	Vector2 endPoint;

	Vector2 normal;
	Ray2D hitboxRay;
	Vector2 hitboxSize;
	float hitboxAngle;
	Color currentColour = Color.red;

	SpriteRenderer spriteRenderer;

	public void Initialize(Vector2 startPoint, Vector2 endPoint) {
		this.startPoint = startPoint;
		this.endPoint = endPoint;
		normal = Vector2.Perpendicular(endPoint - startPoint).normalized;
		hitboxSize = new Vector2( Vector2.Distance(startPoint, endPoint), energyRadius);
		hitboxAngle = Vector2.SignedAngle(Vector2.up, normal);
		hitboxRay = new Ray2D(startPoint, endPoint - startPoint);
		// Stretch sprite between connecting orbs
		spriteRenderer = GetComponentInChildren<SpriteRenderer>();
		Vector3 spriteScale = new Vector3(
			spriteRenderer.transform.localScale.x * hitboxSize.x,
			spriteRenderer.transform.localScale.y,
			spriteRenderer.transform.localScale.z
		);
		spriteRenderer.transform.localScale = spriteScale;
		spriteRenderer.transform.Rotate(new Vector3(0, 0, hitboxAngle));
	}

	#region [MonoBehaviour Callbacks]

	void FixedUpdate() {
		Fry();
	}

	void Update() {
		// Flash colours
		// Used for cycling colours
		Dictionary<Color, Color> nextColour = new Dictionary<Color, Color>();
		nextColour.Add(Color.red, Color.cyan);
		nextColour.Add(Color.cyan, Color.yellow);
		nextColour.Add(Color.yellow, Color.red);
		// Switch to next colour
		nextColour.TryGetValue(currentColour, out currentColour);
		// Update spriterenderer
		SetColour(currentColour);
	}
	
	#endregion

	#region [Private Methods]
	
	void Fry() {
		// Get the colliders of all NPCs that fall on the line
		RaycastHit2D[] hits = Physics2D.LinecastAll(startPoint, endPoint, Utility.GetLayerMask(CharacterType.NPC));
		NonPlayerCharacter npc;
		Parasite parasite;
		// Fry each uninfected NPC on the server
		foreach (RaycastHit2D hit in hits) {
			npc = hit.transform.parent.GetComponent<NonPlayerCharacter>();
			if (npc.photonView.IsMine) {
				npc.OnGotFried();
			}
		}
		
		// Deal damage to parasite if it falls on the line (currently 1 per physics update)
		RaycastHit2D parasiteHit = Physics2D.Linecast(startPoint, endPoint, Utility.GetLayerMask(CharacterType.Parasite));
		if (parasiteHit != false) {
			parasite = parasiteHit.transform.parent.GetComponent<Parasite>();
			parasite.TakeDamage(1);
		}
	}

	Vector2 CalculateForceDirection(Vector2 hunterPosition, Vector2 projectedPosition) {
		return hunterPosition - projectedPosition;
	}

	float CalculateForce(float distance) {
		// The maximum distance from the beam that the force recipient will receive full force
		// 	after this point, the force starts to fall off
		float fullForceCutoff = energyRadius / 2;
		if (distance < fullForceCutoff) {
			return energyForce;
		}
		// POINT LABELS: projectedPosition  fullForceCutoff   energyRadius
		//              	  |----------------|----------------|
		// FORCE OUTPUT:	   ^--FULL FORCE--^			       0
		float t = (distance - fullForceCutoff) / (energyRadius - fullForceCutoff);
		return Mathf.Lerp(energyForce, 0, t);
	}

	void SetColour(Color colour) {
		currentColour = colour;
		// If hunter is blocking beam, fade the beam slightly
		spriteRenderer.color = new Color(colour.r, colour.g, colour.b, 1);
	}
	
	#endregion

}
