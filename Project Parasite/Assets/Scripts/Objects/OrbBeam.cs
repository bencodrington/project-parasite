using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrbBeam : MonoBehaviour {

	int hunterLayerMask;

	float energyRadius = 2f;
	float energyForce = 10f;

	Vector2 startPoint;
	Vector2 endPoint;
	Vector2 normal;

	Ray2D hitboxRay;
	Vector2 hitboxSize;
	float hitboxAngle;

	void Start() {
		hunterLayerMask = 1 << LayerMask.NameToLayer("EnergyCenters");
	}

	public void Initialize(Vector2 startPoint, Vector2 endPoint) {
		this.startPoint = startPoint;
		this.endPoint = endPoint;
		normal = Vector2.Perpendicular(endPoint - startPoint).normalized;
		hitboxSize = new Vector2( Vector2.Distance(startPoint, endPoint), energyRadius);
		hitboxAngle = Vector2.Angle(Vector2.up, normal);
		hitboxRay = new Ray2D(startPoint, endPoint - startPoint);
		Debug.DrawLine(startPoint, endPoint, Color.cyan, 15f);
	}

	void FixedUpdate() {
		Vector2 projectionOntoOrbBeam;
		Vector2 hunterPosition;
		Vector2 forceDirection;
		float distanceToHunter;
		// Get the colliders of all hunters within range of the line
		Collider2D[] energyCenterColliders = Physics2D.OverlapBoxAll(transform.position, hitboxSize, hitboxAngle, hunterLayerMask);
		// TODO: remove;
		// Color lineColour = energyCenterColliders.Length == 0 ? Color.red : Color.green;
		// Debug.DrawLine(startPoint, endPoint, lineColour);
		// Debug.DrawLine(startPoint + normal, endPoint - normal, lineColour);
		foreach (Collider2D energyCenterCollider in energyCenterColliders) {
			Hunter hunter = energyCenterCollider.transform.parent.GetComponent<Hunter>();
			if ((Character)hunter == PlayerGrid.Instance.GetLocalCharacter()) {
				hunterPosition = energyCenterCollider.transform.position;
				// Find the point on the orb beam line that is nearest to the hunter
				projectionOntoOrbBeam = Utility.ProjectOntoRay2D(hunterPosition, hitboxRay);
				distanceToHunter = Vector2.Distance(projectionOntoOrbBeam, hunterPosition);
				// Repel hunter away from projected point with a force that is greater if the hunter is
				//	close to the orb beam
				forceDirection = CalculateForceDirection(hunterPosition, projectionOntoOrbBeam);
				// Default to launching hunters up
				forceDirection = forceDirection == Vector2.zero ? Vector2.up : forceDirection;
				hunter.Repel(forceDirection, CalculateForce(distanceToHunter));
			}
		}
	}

	Vector2 CalculateForceDirection(Vector2 hunterPosition, Vector2 projectedPosition) {
		return hunterPosition - projectedPosition;
	}

	float CalculateForce(float distance) {
		// The maximum distance from the orb that the force recipient will receive full force
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
}
