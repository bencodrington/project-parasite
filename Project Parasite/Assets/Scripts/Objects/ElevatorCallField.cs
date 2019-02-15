using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class ElevatorCallField : InteractableObject {

	public Elevator elevator;
	public GameObject platformCalledAlertPrefab;
	public int stopIndex;

	// How far from the origin (bottom left of this object)
	// 	should the 'platform called' alert be spawned
	Vector2 PLATFORM_CALLED_ALERT_OFFSET = new Vector2(1, 3);

	AudioSource audioSource;

	public override void Start() {
		base.Start();
		audioSource = GetComponent<AudioSource>();
	} 

	void CallElevator() {
		elevator.photonView.RPC("RpcCallToStop", RpcTarget.All, stopIndex);
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
		CallElevator();
	}
}
