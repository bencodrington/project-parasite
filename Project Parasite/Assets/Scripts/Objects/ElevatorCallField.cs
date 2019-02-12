using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class ElevatorCallField : InteractableObject {

	public Elevator elevator;
	public GameObject platformCalledAlertPrefab;
	public int stopIndex;

	Vector2 SIZE = new Vector2(2, 4);
	// How far from the origin (bottom left of this object)
	// 	should the 'platform called' alert be spawned
	Vector2 PLATFORM_CALLED_ALERT_OFFSET = new Vector2(1, 3);

	private Collider2D[] callers = new Collider2D[0];
	private Collider2D[] oldCallers = new Collider2D[0];

	AudioSource audioSource;

	public override void Start() {
		base.Start();
		audioSource = GetComponent<AudioSource>();
	} 

	public void PhysicsUpdate() {
		if (!isServer) { return; }
		// TODO: this probably doesn't need to run every single physics update
		// Check for entity within borders
		callers = Physics2D.OverlapAreaAll(transform.position,
										transform.position + new Vector3(SIZE.x, SIZE.y, 0),
										Utility.GetLayerMask("character"));
		Debug.DrawLine(transform.position, transform.position + new Vector3(SIZE.x, SIZE.y, 0));
		ResolveCallerListDiff(oldCallers, callers);
		oldCallers = callers;
	}

	void ResolveCallerListDiff(Collider2D[] oldCallers, Collider2D[] callers) {
		// TODO: move this logic to the local character because they only need to maintain a list of overlapped objects
		// TODO:		at one point rather than a whole area.
		// TODO: then just verify calls on the server
		// It's okay to mess up the oldCallers array,
		// 	but callers should be preserved outside this method
		callers = (Collider2D[])callers.Clone();
		// Loop through each array
		for (int i=0; i < oldCallers.Length; i++) {
			for (int j=0; j < callers.Length; j++) {
				// If the current new caller matches this old caller
				if (callers[j] != null && callers[j] == oldCallers[i]) {
					// Cross them both off, this collider has not changed
					oldCallers[i] = null;
					callers[j] = null;
					// Move on to next old caller
					break;
				}
			}
		}
		// Now, the only values that arent null are:
		// 	- those that have left the field in the oldCallers array
		//	- those that have just entered the field in the callers array
		foreach (Collider2D oldCaller in oldCallers) {
			if (oldCaller != null) {
				// Character is leaving the field
				// TODO:
				// GetCharacterFromCollider(oldCaller).RpcUnregisterObject(netId);
			}
		}
		foreach (Collider2D caller in callers) {
			if (caller != null) {
				// Character is entering the field
				// TODO:
				// GetCharacterFromCollider(caller).RpcRegisterObject(netId);
			}
		}
	}

	Character GetCharacterFromCollider(Collider2D collider) {
		return collider.GetComponentInParent<Character>();
	}

	void CallElevator() {
		elevator.CmdCallToStop(stopIndex);
		// Play 'ding' sound
		audioSource.Play();
		// Show 'platform called!' alert
		Instantiate(platformCalledAlertPrefab,
					(Vector2)transform.position + PLATFORM_CALLED_ALERT_OFFSET,
					Quaternion.identity)
			// And set this call field as it's parent in the hierarchy
			.transform.SetParent(transform);
	}

	public override void OnInteract() {
		CmdCallElevator();
	}

	// Commands
	[Command]
	void CmdCallElevator() {
		CallElevator();
	}
}
