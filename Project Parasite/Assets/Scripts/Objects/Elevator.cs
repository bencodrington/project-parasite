using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Elevator : NetworkBehaviour {
	
	const float LAG_LERP_FACTOR = 0.4f;
	const float MOVEMENT_SPEED = 4f;
	const float BUTTON_OFFSET = 0.5f;

	public float[] stops;
	public Vector2 size;

	public GameObject buttonPrefab;

	int targetStop;
	Vector3 serverPosition;
	bool isMoving = false;
	
	private Collider2D[] passengers;
	private ElevatorButton[] buttons;

	public void InitializeButtons() {
		ElevatorButton button;
		buttons = new ElevatorButton[stops.Length];

		Vector2 spawnPos = new Vector2(transform.position.x, transform.position.y + (size.y / 2));
		// Spawn button prefabs based on # of stops
		for (int i = 0; i < stops.Length; i++) {
			spawnPos.y += BUTTON_OFFSET;
			button = Instantiate(buttonPrefab, spawnPos, Quaternion.identity, transform).GetComponentInChildren<ElevatorButton>();
			button.stopIndex = i;
			button.elevatorId = this.netId;
			buttons[i] = button;
		}

	}
	
	void FixedUpdate() {
		if (isServer) {
			if (isMoving) {
				MoveToTargetStop();
			} else {
				// TODO: this probably doesn't need to run every single physics update
				// Check for entity within borders
				Vector2 halfSize = size / 2;
				passengers = Physics2D.OverlapAreaAll((Vector2)transform.position - halfSize,
												(Vector2)transform.position + halfSize,
												Utility.GetLayerMask("character"));
				Debug.DrawLine((Vector2)transform.position - halfSize, (Vector2)transform.position + halfSize);
				if (passengers.Length > 0) {
					// Show buttons on server
					SetButtonEnabled(true);
					// Show buttons on client
					RpcSetButtonEnabled(true);
				}
			}
			RpcUpdateServerPosition(transform.position);
		} else {
			transform.position = Vector3.Lerp(transform.position, serverPosition, LAG_LERP_FACTOR);
		}
	}

	void MoveToTargetStop() {
			Vector2 targetPosition;
			float potentialMovement = MOVEMENT_SPEED * Time.deltaTime;
				targetPosition = new Vector2(transform.position.x, stops[targetStop]);
				if (Vector3.Distance(transform.position, targetPosition) < potentialMovement) {
					// Destination reached
					transform.position = targetPosition;
					isMoving = false;
				} else {
					transform.position = Vector3.MoveTowards(transform.position, targetPosition, potentialMovement);
				}

	}

	void SetButtonEnabled(bool isEnabled) {
		for (int i = 0; i < buttons.Length; i++) {
			buttons[i].IsEnabled = isEnabled;
		}
	}

	// Commands

	[Command]
	public void CmdCallToStop(int indexOfStop) {
		targetStop = indexOfStop;
		isMoving = true;
		SetButtonEnabled(false);
		RpcSetButtonEnabled(false);
	}

	// ClientRpc

	[ClientRpc]
	void RpcUpdateServerPosition(Vector3 newPosition) {
		if (isServer) { return; }
		// Else, on a client machine, so update our record of the elevator's true position
		serverPosition = newPosition;
	}

	[ClientRpc]
	void RpcSetButtonEnabled(bool isEnabled) {
		if (!isServer) {
			SetButtonEnabled(isEnabled);
		}
	}


}
