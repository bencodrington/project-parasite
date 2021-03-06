﻿using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class ElevatorButton : MonoBehaviour {

	#region [Public Variables]

	// The width and height of this button's "clickbox"
	public Vector2 size;
	// The index of the stop this button should call the elevator to
	public int stopIndex;
	// The elevator this button belongs to
	public Elevator elevator;
	public bool isDisabled = false;
	
	public Color hoverColour = new Color(0.4f, 0.7f, 0.9f, 1);
	public Color disabledColour = new Color(0.1f, 0.2f, 0.3f, .5f);
	
	#endregion

	#region [Private Variables]
	
	// Bounds of the button in world space
	Vector2 bottomLeft, topRight;

	SpriteRenderer spriteRenderer;
	Color defaultColour;
	
	#endregion

	#region [MonoBehaviour Callbacks]
	
	void Start() {
		spriteRenderer = GetComponentInChildren<SpriteRenderer>();
		defaultColour = spriteRenderer.color;
	}
	
	void Update() {
		// OPTIMIZE: can be optimized with proper inputManager
		Vector2 halfSize = size / 2;
		bottomLeft = (Vector2)transform.position - halfSize;
		topRight = (Vector2)transform.position + halfSize;
		bool isMouseOver = Utility.MouseIsWithinBounds(bottomLeft, topRight);
		if (isDisabled) {
			spriteRenderer.color = disabledColour;
		} else if (isMouseOver) {
			// Highlight
			spriteRenderer.color = hoverColour;
		} else {
			spriteRenderer.color = defaultColour;
		}
		// Upon mouse click
		if (Input.GetMouseButtonDown(0)) {
			// Check if click is within bounds and the button is enabled
			if (!isDisabled && isMouseOver) {
				CallElevator();
			}
		}
	}
	
	#endregion

	#region [Private Methods]
	
	void CallElevator() {
		elevator.CallToStop(stopIndex);
	}
	
	#endregion
}
