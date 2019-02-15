using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Orb : MonoBehaviour {

	float energyRadius = 2f;
	float energyForce = 1f;

	OrbBeam beam;

	void FixedUpdate() {
		// TODO:
		// Collider2D[] hunterColliders = Physics2D.OverlapCircleAll(transform.position, energyRadius, Utility.GetLayerMask("energyCenter"));
		// Vector2 forceDirection;
		// foreach (Collider2D hunterCollider in hunterColliders) {
		// 	Hunter hunter = hunterCollider.transform.parent.GetComponent<Hunter>();
		// 	if ((Character)hunter == PlayerGrid.Instance.GetLocalCharacter()) {
		// 		forceDirection = hunterCollider.transform.position - transform.position;
		// 		hunter.Repel(forceDirection, CalculateForce(hunterCollider.transform.position));
		// 	}
		// }
	}

	float CalculateForce(Vector2 hunterPosition) {
		float distance = Vector2.Distance(transform.position, hunterPosition);
		// The maximum distance from the orb that the force recipient will receive full force
		// 	after this point, the force starts to fall off
		float fullForceCutoff = energyRadius / 2;
		if (distance < fullForceCutoff) {
			return energyForce;
		}
		// POINT LABELS: orb       fullForceCutoff    energyRadius
		//                |----------------|----------------|
		// FORCE OUTPUT:   ^--FULL FORCE--^			       0
		float t = (distance - fullForceCutoff) / (energyRadius - fullForceCutoff);
		return Mathf.Lerp(energyForce, 0, t);
	}

	public void AttachBeam(OrbBeam beam) {
		this.beam = beam;
	}

	void OnDestroy() {
		if (beam != null) {
			Destroy(beam.gameObject);
		}
	}
}
