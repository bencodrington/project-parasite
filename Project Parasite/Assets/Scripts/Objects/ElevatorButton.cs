using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class ElevatorButton : MonoBehaviour {

	// The width and height of this button's "clickbox"
	public Vector2 size;
	// The index of the stop this button should call the elevator to
	public int stopIndex;
	// The elevator this button belongs to
	public NetworkInstanceId elevatorId;
	// Bounds of the button in world space
	Vector2 bottomLeft, topRight;

	SpriteRenderer spriteRenderer;
	Color defaultColour;
	Color hoverColour = new Color(0.4f, 0.7f, 0.9f, 1);

	void Start() {
		spriteRenderer = GetComponentInChildren<SpriteRenderer>();
		defaultColour = spriteRenderer.color;
	}
	
	void Update() {
		// TODO: can be optimized with proper inputManager
		Vector2 halfSize = size / 2;
		bottomLeft = (Vector2)transform.position - halfSize;
		topRight = (Vector2)transform.position + halfSize;
		bool isMouseOver = Utility.MouseIsWithinBounds(bottomLeft, topRight);
		if (isMouseOver) {
			// Highlight
			spriteRenderer.color = hoverColour;
		} else {
			spriteRenderer.color = defaultColour;
		}
		// Upon mouse click
		if (Input.GetMouseButtonDown(0)) {
			// Check if click is within bounds
			if (isMouseOver) {
				PlayerGrid.Instance.GetLocalPlayerObject().CmdCallElevatorToStop(elevatorId, stopIndex);
			}
		}
	}
}
