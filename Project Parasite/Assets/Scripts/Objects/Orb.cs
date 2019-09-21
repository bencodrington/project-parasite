using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Orb : MonoBehaviour {

	#region [Public Variables]
	
	public GameObject orbBeamPrefab;
	
	#endregion

	#region [Private Variables]
	
	const float FORCE = 35f;
	const float RADIUS = 4f;
	const float FULL_FORCE_CUTOFF_RADIUS = 2f;
	bool isActive;

	// The beam connecting this orb to the next
	// 	this orb will destroy this beam when it is recalled
	OrbBeam beam;
	// The beam connecting the previous orb to this one
	//	- stored so that this orb can activate it when this orb is activated
	OrbBeam previousBeam;

	#endregion

	#region [MonoBehaviour Callbacks]

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

	public void SpawnBeamToPreviousOrb(Orb previousOrb) {
		Vector2 beamSpawnPosition;
		// Spawn beam halfway between orbs
		beamSpawnPosition = Vector2.Lerp(previousOrb.transform.position, transform.position, 0.5f);
		previousBeam = Instantiate(orbBeamPrefab, beamSpawnPosition, Quaternion.identity).GetComponent<OrbBeam>();
		// Store beam in most recent orb so when the orb is destroyed it can take the beam with it
		previousOrb.AttachBeam(previousBeam);
		previousBeam.Initialize(previousOrb.transform.position, transform.position);
		// Deactivate previous beam
		previousBeam.gameObject.SetActive(false);
	}
	
	public void AttachBeam(OrbBeam beam) {
		this.beam = beam;
	}

	public void SetActive() {
		isActive = true;
		// Activate previous beam if it exists
		if (previousBeam != null) {
			previousBeam.gameObject.SetActive(true);
		}
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
