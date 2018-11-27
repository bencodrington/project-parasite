using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour {

	private Transform target;
	private Vector3 offset = new Vector3(0, 2, -10);
	// Raise this for camera to be snappier, lower it to be smoother
	private const float SMOOTH_TIME = 10;

	void FixedUpdate() {
		if (target == null) { return; }
		Vector3 targetPosition = target.position + offset;
		transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * SMOOTH_TIME);
	}

	public void SetTarget(Transform newTarget) {
		target = newTarget;
	}

}
