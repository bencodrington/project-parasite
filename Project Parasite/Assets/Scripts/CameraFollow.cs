using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour {

	#region [Private Variables]
	// Raise this for camera to be snappier, lower it to be smoother
	const float SMOOTH_TIME = 10;
	
	Transform target;
	Vector3 offset = new Vector3(0, 2, -10);

	Coroutine shakingScreen;
	float screenShakeOffsetDistance;
	float currentScreenShakeIntensity = 0;
	
	#endregion

	#region [Public Methods]

	public void SetTarget(Transform newTarget, bool forceSnapToTargetPosition = false) {
		target = newTarget;
		if (forceSnapToTargetPosition) {
			// Jump to new target's position immediately
			transform.position = target.position + offset;
		}
	}

	public void ShakeScreen(float intensity, float duration, Vector2 coordinates) {
		float distanceFromTarget = Vector2.Distance(target.position, coordinates);
		intensity = CalculateIntensity(intensity, distanceFromTarget);
		if (shakingScreen != null) {
			if (intensity < currentScreenShakeIntensity) {
				// Don't overwrite screenshakes with less intense ones
				return;
			}
			StopCoroutine(shakingScreen);
		}
		shakingScreen = StartCoroutine(ShakingScreen(duration, intensity));
		currentScreenShakeIntensity = intensity;
	}
	
	#endregion

	#region [Private Methods]
	
	void FixedUpdate() {
		if (target == null) { return; }
		Vector3 targetPosition = target.position + offset;
		transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * SMOOTH_TIME);
		float[] screenShakeOffsets = new float[4];
		for (int i=0; i < 4; i++) {
			// Ensure that the random value is closer to the screenShakeOffsetDistance than to 0
			screenShakeOffsets[i] = Random.Range(screenShakeOffsetDistance / 2, screenShakeOffsetDistance);
		}
		transform.position += new Vector3(
			Random.Range(-screenShakeOffsets[0], screenShakeOffsets[1]),
			Random.Range(-screenShakeOffsets[2], screenShakeOffsets[3])
		);
	}

	IEnumerator ShakingScreen(float duration, float intensity) {
		float timeElapsed = 0f;
		float progress = 0f;
		while (timeElapsed <= duration) {
			// Increase time elapsed
			timeElapsed += Time.deltaTime;
			progress = timeElapsed / duration;
			// Decrease distance
			screenShakeOffsetDistance = (1 - progress) * intensity;
			yield return null;
		}
		screenShakeOffsetDistance = 0;
		shakingScreen = null;
	}

	float CalculateIntensity(float fullIntensity, float distance) {
		if (distance < 5) {
			return fullIntensity;
		}
		if (distance < 10) {
			return .8f * fullIntensity;
		}
		if (distance < 20) {
			return .6f * fullIntensity;
		}
		if (distance < 30) {
			return .4f * fullIntensity;
		}
		return 0;
	}
	
	#endregion


}
