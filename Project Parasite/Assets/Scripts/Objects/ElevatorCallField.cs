using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class ElevatorCallField : NetworkBehaviour {

	public Elevator elevator;
	public int stopIndex;

	public Vector2 size;

	private Collider2D caller;

	void FixedUpdate() {
		if (!isServer) { return; }
		// TODO: this probably doesn't need to run every single physics update
		// Check for entity within borders
		caller = Physics2D.OverlapArea(transform.position,
										transform.position + new Vector3(size.x, size.y, 0),
										Utility.GetLayerMask("character"));
		Debug.DrawLine(transform.position, transform.position + new Vector3(size.x, size.y, 0));
		if (caller != null) {
			CallElevator();
		}
	}

	void CallElevator() {
		elevator.CmdCallToStop(stopIndex);
	}
}
