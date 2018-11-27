using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Detonation : MonoBehaviour {

	public SpriteRenderer sR;
	public float lifeTime = 0.1f;

	void Update() {
		lifeTime -= Time.deltaTime;
		if (lifeTime <= 0) {
			Destroy(gameObject);
		}
		float alpha = Random.Range(0f, 1f);
		sR.color = new Color(sR.color.r, sR.color.g, sR.color.b, alpha);
	}


}
