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

	private SpriteRenderer spriteRenderer;
	private bool _isEnabled;
	public bool IsEnabled {
		get {
			return _isEnabled;
		} set {
			spriteRenderer.color = new Color(spriteRenderer.color.r,
											spriteRenderer.color.g,
											spriteRenderer.color.b,
											value ? 1 : 0);
			_isEnabled = value;
		}
	}

	void Start() {
		spriteRenderer = GetComponentInChildren<SpriteRenderer>();
		IsEnabled = false;
	}
	
	void Update () {
		if (!_isEnabled) { return; }
		Vector2 mousePosition, bottomLeft, topRight;
		Vector2 halfSize = size / 2;
		// Upon mouse click
		if (Input.GetMouseButtonDown(0)) {
			bottomLeft = (Vector2)transform.position - halfSize;
			topRight = (Vector2)transform.position + halfSize;
			mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			// Check if click is within bounds
			if (WithinBounds(bottomLeft, mousePosition, topRight)) {
				PlayerGrid.Instance.GetLocalPlayerObject().CmdCallElevatorToStop(elevatorId, stopIndex);
			}
		}
	}

	bool WithinBounds(Vector2 bottomLeft, Vector2 point, Vector2 topRight) {
		return (bottomLeft.x <= point.x && point.x <= topRight.x &&
				bottomLeft.y <= point.y && point.y <= topRight.y);
	}
}
