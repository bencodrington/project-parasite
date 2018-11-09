using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Elevator : NetworkBehaviour {
	
	const float LAG_LERP_FACTOR = 0.4f;
	const float MOVEMENT_SPEED = 4f;
	const float BUTTON_OFFSET = 0.5f;

	public Vector2[] stops;
	public Vector2 size;

	public GameObject buttonPrefab;

	int targetStop;
	Vector3 serverPosition;
	bool isMoving = false;
	
	private Collider2D[] passengers;
	private int passengerLayerMask;

	void Start() {
		int hunterMask = 1 << LayerMask.NameToLayer("Hunters");
		int npcMask = 1 << LayerMask.NameToLayer("NPCs");
		int parasiteMask = 1 << LayerMask.NameToLayer("Parasites");
		passengerLayerMask = hunterMask + npcMask + parasiteMask;	

		Vector2 spawnPos = new Vector2(transform.position.x, transform.position.y + (size.y / 2));
		// Spawn button prefabs based on # of stops
		for (int i = 0; i < stops.Length; i++) {
			spawnPos.y += BUTTON_OFFSET;
			ElevatorButton button = Instantiate(buttonPrefab, spawnPos, Quaternion.identity, transform).GetComponentInChildren<ElevatorButton>();
			button.stopIndex = i;
			button.elevatorId = this.netId;
		}
	}

	void Update() {
		if (isServer) { return; }
		// We're on a client machine, so
		// 	Make sure we're in line with where the server says we should be
		transform.position = Vector3.Lerp(transform.position, serverPosition, LAG_LERP_FACTOR);
	}
	
	void FixedUpdate() {
		if (isServer) {
			if (isMoving) {
				MoveToTargetStop();
			} else {
				// TODO: this probably doesn't need to run every single physics update
				// Check for entity within borders
				passengers = Physics2D.OverlapAreaAll(transform.position,
												transform.position + new Vector3(size.x, size.y, 0),
												passengerLayerMask);
				Debug.DrawLine(transform.position, transform.position + new Vector3(size.x, size.y, 0));
				if (passengers.Length > 0) {
					// TODO: Show Buttons
				}
			}
			RpcUpdateServerPosition(transform.position);
		}
		
	}

	void MoveToTargetStop() {
			Vector2 targetPosition;
			float potentialMovement = MOVEMENT_SPEED * Time.deltaTime;
				targetPosition = stops[targetStop];
				if (Vector3.Distance(transform.position, targetPosition) < potentialMovement) {
					// Destination reached
					transform.position = targetPosition;
					isMoving = false;
				} else {
					transform.position = Vector3.MoveTowards(transform.position, targetPosition, potentialMovement);
				}

	}

	// Commands

	[Command]
	public void CmdCallToStop(int indexOfStop) {
		targetStop = indexOfStop;
		isMoving = true;
	}

	// ClientRpc

	[ClientRpc]
	void RpcUpdateServerPosition(Vector3 newPosition) {
		if (isServer) { return; }
		// Else, on a client machine, so update our record of the elevator's true position
		serverPosition = newPosition;
	}


}
