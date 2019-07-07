using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Orb : MonoBehaviour {

	#region [Private Variables]
	
	const float FORCE = 35f;
	const float RADIUS = 4f;
	const float FULL_FORCE_CUTOFF_RADIUS = 2f;
	const float BURST_FADE_TIME = .1f;
	bool isActive;

	#endregion

	public SpriteRenderer burstSprite;

	OrbBeam beam;

	#region [MonoBehaviour Callbacks]
	
	void Start() {
		isActive = true;
	}

	void FixedUpdate() {
		if (!isActive) { return; }
		Collider2D[] hunterColliders = Physics2D.OverlapCircleAll(transform.position, RADIUS, Utility.GetLayerMask("energyCenter"));
		Vector2 forceDirection;
		foreach (Collider2D hunterCollider in hunterColliders) {
			Hunter hunter = hunterCollider.transform.parent.GetComponent<Hunter>();
			if (hunter.photonView.IsMine) {
				forceDirection = hunterCollider.transform.position - transform.position;
				hunter.Repel(forceDirection, CalculateForce(hunterCollider.transform.position));
			}
		}
		// Only run on the first fixedupdate
		isActive = false;
	}

	void OnDestroy() {
		if (beam != null) {
			Destroy(beam.gameObject);
		}
	}
	
	#endregion

	#region [Public Methods]
	
	public void AttachBeam(OrbBeam beam) {
		this.beam = beam;
	}
	
	#endregion

	#region [Private Methods]
	
	float CalculateForce(Vector2 hunterPosition) {
		float distance = Vector2.Distance(transform.position, hunterPosition);
		// The maximum distance from the orb that the force recipient will receive full force
		// 	after this point, the force starts to fall off
		if (distance < FULL_FORCE_CUTOFF_RADIUS) {
			return FORCE;
		}
		// POINT LABELS: orb  FULL_FORCE_CUTOFF_RADIUS      RADIUS
		//                |----------------|----------------|
		// FORCE OUTPUT:   ^--FULL FORCE--^			       0
		float t = (distance - FULL_FORCE_CUTOFF_RADIUS) / (RADIUS - FULL_FORCE_CUTOFF_RADIUS);
		return Mathf.Lerp(FORCE, 0, t);
	}
	
	#endregion

	

	
}
