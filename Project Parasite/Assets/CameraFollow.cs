using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour {

	private Transform target;
	private Vector3 offset = new Vector3(0, 0, -10);

	void FixedUpdate() {
		if (target == null) { return; }
		transform.position = target.position + offset;
	}

	public void SetTarget(Transform newTarget) {
		target = newTarget;
	}

}
