using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class ElevatorCallField : NetworkBehaviour {

	public Elevator elevator;
	public int stopIndex;

	public Vector2 size;

	private int callerLayerMask;
	private Collider2D caller;
	
	void Start() {
		int hunterMask = 1 << LayerMask.NameToLayer("Hunters");
		int npcMask = 1 << LayerMask.NameToLayer("NPCs");
		int parasiteMask = 1 << LayerMask.NameToLayer("Parasites");
		callerLayerMask = hunterMask + npcMask + parasiteMask;
							
	} 

	void FixedUpdate() {
		if (!isServer) { return; }
		// TODO: this probably doesn't need to run every single physics update
		caller = Physics2D.OverlapArea(transform.position,
										transform.position + new Vector3(size.x, size.y, 0),
										callerLayerMask);
		Debug.DrawLine(transform.position, transform.position + new Vector3(size.x, size.y, 0));
		if (caller != null) {
			CallElevator();
		}
	}

	void CallElevator() {
		elevator.CmdCallToStop(stopIndex);
	}
}
