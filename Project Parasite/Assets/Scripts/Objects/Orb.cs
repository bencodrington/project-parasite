using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Orb : MonoBehaviour {

	const float RESTING_ENERGY_FORCE = 1f;
	const float BURST_ENERGY_FORCE = 20f;
	const float RESTING_ENERGY_RADIUS = 2f;
	const float BURST_ENERGY_RADIUS = 4f;
	const float BURST_FADE_TIME = .1f;
	float energyRadius = 2f;
	float energyForce = BURST_ENERGY_FORCE;

	public SpriteRenderer burstSprite;

	Coroutine intensityCoroutine;

	OrbBeam beam;

	#region [MonoBehaviour Callbacks]
	
	
	void Start() {
		intensityCoroutine = StartCoroutine(FadeIntensity());
	}

	void FixedUpdate() {
		Collider2D[] hunterColliders = Physics2D.OverlapCircleAll(transform.position, energyRadius, Utility.GetLayerMask("energyCenter"));
		Vector2 forceDirection;
		foreach (Collider2D hunterCollider in hunterColliders) {
			Hunter hunter = hunterCollider.transform.parent.GetComponent<Hunter>();
			if (hunter.photonView.IsMine) {
				forceDirection = hunterCollider.transform.position - transform.position;
				hunter.Repel(forceDirection, CalculateForce(hunterCollider.transform.position));
			}
		}
	}

	void OnDestroy() {
		if (beam != null) {
			Destroy(beam.gameObject);
		}
		if (intensityCoroutine != null) {
			StopCoroutine(intensityCoroutine);
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
		float fullForceCutoff = energyRadius * (3 / 4);
		if (distance < fullForceCutoff) {
			return energyForce;
		}
		// POINT LABELS: orb       fullForceCutoff    energyRadius
		//                |----------------|----------------|
		// FORCE OUTPUT:   ^--FULL FORCE--^			       0
		float t = (distance - fullForceCutoff) / (energyRadius - fullForceCutoff);
		return Mathf.Lerp(energyForce, 0, t);
	}

	IEnumerator FadeIntensity() {
		float remainingFadeTime = BURST_FADE_TIME;
		while (remainingFadeTime > 0) {
			yield return null;
			remainingFadeTime -= Time.deltaTime;
			energyForce = Mathf.Lerp(RESTING_ENERGY_FORCE, BURST_ENERGY_FORCE, remainingFadeTime / BURST_FADE_TIME);
			energyRadius = Mathf.Lerp(RESTING_ENERGY_RADIUS, BURST_ENERGY_RADIUS, remainingFadeTime / BURST_FADE_TIME);
			burstSprite.transform.localScale = Vector2.Lerp(Vector2.zero, new Vector2(3, 3), remainingFadeTime / BURST_FADE_TIME);
			burstSprite.color = new Color(burstSprite.color.r, burstSprite.color.g, burstSprite.color.b, Random.Range(0.25f, 0.5f));
		}
		energyForce = RESTING_ENERGY_FORCE;
		energyRadius = RESTING_ENERGY_RADIUS;
		intensityCoroutine = null;
		Destroy(burstSprite.gameObject);
	}
	
	#endregion

	

	
}
