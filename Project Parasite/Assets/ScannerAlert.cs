using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScannerAlert : MonoBehaviour {

	const float FADE_LENGTH = 20f;
	CanvasRenderer[] renderers;
	Vector2 scannerPosition;
	bool scannerIsInView;

	void Start() {
		renderers = GetComponentsInChildren<CanvasRenderer>();
		StartCoroutine(FadeOut());
	}

	void Update() {
		float distToScreenEdge;
		Vector2 screenCenter, displacement;
		Camera cam = Camera.main;
		Vector2 scannerScreenPosition = cam.WorldToScreenPoint(scannerPosition);
		scannerIsInView = cam.pixelRect.Contains(scannerScreenPosition);
		if (!scannerIsInView) {
			// Calculate angle between center and scanner
			screenCenter = cam.pixelRect.center;
			displacement = scannerScreenPosition - screenCenter;
			// Keep alert on screen
			distToScreenEdge = cam.pixelWidth > cam.pixelHeight ? cam.pixelHeight / 2 : cam.pixelWidth / 2;
			// TODO: probably want to add some padding here
			displacement = Vector2.ClampMagnitude(displacement, distToScreenEdge);
			// Update position to reflect;
			transform.position = screenCenter + displacement;
		}
	}

	IEnumerator FadeOut() {
		float timeRemaining = FADE_LENGTH;
		Color c;
		while (timeRemaining > 0) {
			timeRemaining -= Time.deltaTime;
			c = renderers[0].GetColor();
			if (scannerIsInView) {
				c.a = 0;
			} else {
				c.a = timeRemaining / FADE_LENGTH;
			}
			foreach (CanvasRenderer renderer in renderers) {
				renderer.SetColor(c);
			}
			yield return null;
		}
		Destroy(gameObject);
	}

    public void SetScannerPosition(Vector2 scannerPosition) {
        this.scannerPosition = scannerPosition;
    }
}
