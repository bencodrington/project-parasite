using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class ElevatorCallField : NetworkBehaviour {

	public Elevator elevator;
	public int stopIndex;

	Vector2 SIZE = new Vector2(2, 4);

	private Collider2D caller;

	public void PhysicsUpdate() {
		if (!isServer) { return; }
		// TODO: this probably doesn't need to run every single physics update
		// Check for entity within borders
		caller = Physics2D.OverlapArea(transform.position,
										transform.position + new Vector3(SIZE.x, SIZE.y, 0),
										Utility.GetLayerMask("character"));
		Debug.DrawLine(transform.position, transform.position + new Vector3(SIZE.x, SIZE.y, 0));
		if (caller != null) {
			CallElevator();
		}
	}

	void CallElevator() {
		elevator.CmdCallToStop(stopIndex);
	}
}
