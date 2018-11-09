using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Elevator : NetworkBehaviour {
	
	const float LAG_LERP_FACTOR = 0.4f;
	const float MOVEMENT_SPEED = 4f;

	public Vector2[] stops;
	int targetStop;
	Vector3 serverPosition;
	bool isMoving = false;

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
