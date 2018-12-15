using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PounceIndicator : MonoBehaviour {

	Vector2 minSize = new Vector2(1, 1);
	Vector2 maxSize = new Vector2(4f, 4f);

	SpriteRenderer spriteRenderer;
	void Start() {
		spriteRenderer = GetComponentInChildren<SpriteRenderer>();
		Hide();
	}

	public void Hide() {
		SetAlpha(0);
	}
	public void Show() {
		SetAlpha(1);
	}

	// Percentage should be a value in the range [0..1]
	public void SetPercentage(float percentage) {
		SetSize(percentage);
		SetAlpha(percentage);
	}
	void SetSize(float percentage) {
		spriteRenderer.transform.localScale = Vector2.Lerp(minSize, maxSize, percentage);
	}

	void SetAlpha(float a) {
		Color c = spriteRenderer.color;
		spriteRenderer.color = new Color(c.r, c.g, c.b, a);
	}

	// Angle should be in degrees
	public void SetAngle(float angle) {
		spriteRenderer.transform.eulerAngles = new Vector3(0, 0, 1) * angle;
	}
}
