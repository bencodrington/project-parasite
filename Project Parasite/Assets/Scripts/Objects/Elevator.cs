using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

public class Elevator : MonoBehaviourPun {
	
	const float LAG_LERP_FACTOR = 0.4f;
	const float MOVEMENT_SPEED = 8f;
	const float BUTTON_OFFSET = 0.6f;
	// How far from the elevator's center should the callfield center be
	const float STOP_X_OFFSET = 2f;

	public Vector2 SIZE = new Vector2(2, 3);

	public GameObject buttonPrefab;
	public GameObject elevatorCallFieldPrefab;

	int targetStop;
	Vector3 serverPosition;
	bool isMoving = false;
	
	private Collider2D[] passengers;
	private ElevatorButton[] buttons;
	List<ElevatorCallField> callFields;

	private KinematicPhysicsEntity[] kinematicPhysicsEntities;

	#region [MonoBehaviour Callbacks]
	
	void Awake() {
		kinematicPhysicsEntities = GetComponentsInChildren<KinematicPhysicsEntity>();
		callFields = new List<ElevatorCallField>();
	}
	
	#endregion

	void InitializeButtons() {
		// ElevatorButton button;
		// buttons = new ElevatorButton[stops.Length];

		// Vector2 spawnPos = new Vector2(transform.position.x,
		// 						transform.position.y +	// Base vertical position of the center of the elevator
		// 						(SIZE.y / 2) +			// Get to the top of the elevator
		// 						BUTTON_OFFSET / 2 );	// Add some padding before start of first button
		// // Spawn button prefabs based on # of stops
		// for (int i = 0; i < stops.Length; i++) {
		// 	button = Instantiate(buttonPrefab, spawnPos, Quaternion.identity, transform).GetComponentInChildren<ElevatorButton>();
		// 	button.gameObject.GetComponentInChildren<Text>().text = (i + 1).ToString();
		// 	button.stopIndex = i;
		// 	// TODO:
		// 	// button.elevatorId = this.netId;
		// 	buttons[i] = button;
		// 	spawnPos.y += BUTTON_OFFSET;
		// }
	}
	
	public void PhysicsUpdate() {
		// if (PhotonNetwork.IsMasterClient) {
		// 	if (isMoving) {
		// 		MoveToTargetStop();
		// 	} else {
		// 		// TODO: this probably doesn't need to run every single physics update
		// 		// Check for entity within borders
		// 		Vector2 halfSize = SIZE / 2;
		// 		passengers = Physics2D.OverlapAreaAll((Vector2)transform.position - halfSize,
		// 										(Vector2)transform.position + halfSize,
		// 										Utility.GetLayerMask("character"));
		// 		Debug.DrawLine((Vector2)transform.position - halfSize, (Vector2)transform.position + halfSize);
		// 		if (passengers.Length > 0) {
		// 			// Show buttons on client
		// 			RpcSetButtonActive(true);
		// 		} else {
		// 			// Hide buttons on client
		// 			RpcSetButtonActive(false);
		// 		}
		// 	}
		// 	RpcUpdateServerPosition(transform.position);
		// } else {
		// 	transform.position = Vector3.Lerp(transform.position, serverPosition, LAG_LERP_FACTOR);
		// }
		// Update each kinematicPhysicsEntity in this component's children (floor/ceiling)
		foreach(KinematicPhysicsEntity entity in kinematicPhysicsEntities) {
			entity.PhysicsUpdate();
		}
		// Update each callfield (a.k.a. stop) that belongs to this elevator
		foreach(ElevatorCallField callField in callFields) {
			callField.PhysicsUpdate();
		}
	}

	void MoveToTargetStop() {
		// Vector2 targetPosition;
		// float potentialMovement = MOVEMENT_SPEED * Time.deltaTime;
		// targetPosition = new Vector2(transform.position.x, stops[targetStop]);
		// if (Vector3.Distance(transform.position, targetPosition) < potentialMovement) {
		// 	// Destination reached
		// 	transform.position = targetPosition;
		// 	isMoving = false;
		// 	// Disable this floor's button
		// 	RpcDisableButton(targetStop);
		// } else {
		// 	transform.position = Vector3.MoveTowards(transform.position, targetPosition, potentialMovement);
		// }
	}

	void SetButtonActive(bool isActive) {
		for (int i = 0; i < buttons.Length; i++) {
			buttons[i].gameObject.SetActive(isActive);
		}
	}

	#region [Private Methods]
	
	void SpawnStops(float[] yCoordinates, bool[] isOnRightSideValues) {
		for (int i = 0; i < yCoordinates.Length; i++) {
			SpawnStop(yCoordinates[i], isOnRightSideValues[i], i);
		}
	}

	void SpawnStop(float yCoordinate, bool isOnRightSide, int index) {
		// Instantiate GameObject
		GameObject callFieldGameObject = GameObject.Instantiate(
								elevatorCallFieldPrefab,
								GetStopSpawnCoordinates(transform.position.x, yCoordinate, isOnRightSide),
								Quaternion.identity);
		ElevatorCallField callField = callFieldGameObject.GetComponent<ElevatorCallField>();
		callField.elevator = this;
		callField.stopIndex = index;
		callFields.Add(callField);
	}

	Vector2 GetStopSpawnCoordinates(float xCoordinate, float yCoordinate, bool isOnRightSide) {
		// -1f is because call fields are positioned by their bottom left corner, not the middle
		// TODO: this can be cleaner
		xCoordinate += isOnRightSide ? STOP_X_OFFSET - 1f : -STOP_X_OFFSET - 1f;
		// TODO: replace magic number, half of elevator height
		return new Vector2(xCoordinate, yCoordinate - 1.5f);
	}
	
	#endregion

	[PunRPC]
	public void RpcSetStopData(float[] yCoordinates, bool[] isOnRightSideValues) {
		SpawnStops(yCoordinates, isOnRightSideValues);
		// TODO:
		// InitializeButtons();
	}

	[PunRPC]
	public void RpcCallToStop(int indexOfStop) {
		Debug.Log("Called to stop: " + indexOfStop);
		// RpcEnableButton(targetStop);
		// targetStop = indexOfStop;
		// isMoving = true;
		// SetButtonActive(false);
		// RpcSetButtonActive(false);
	}

	// ClientRpc
	void RpcUpdateServerPosition(Vector3 newPosition) {
		if (PhotonNetwork.IsMasterClient) { return; }
		// Else, on a client machine, so update our record of the elevator's true position
		serverPosition = newPosition;
	}

	void RpcSetButtonActive(bool isEnabled) {
		SetButtonActive(isEnabled);
	}

	void RpcDisableButton(int index) {
		buttons[index].isDisabled = true;
	}

	void RpcEnableButton(int index) {
		buttons[index].isDisabled = false;
	}
}
