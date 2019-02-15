using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

public class Elevator : MonoBehaviourPun {
	
	const float LAG_LERP_FACTOR = 0.4f;
	const float MOVEMENT_SPEED = 8f;
	const float BUTTON_OFFSET = 0.6f;

	public float[] stops;
	public Vector2 SIZE = new Vector2(2, 3);

	public GameObject buttonPrefab;

	int targetStop;
	Vector3 serverPosition;
	bool isMoving = false;
	
	private Collider2D[] passengers;
	private ElevatorButton[] buttons;

	private KinematicPhysicsEntity[] kinematicPhysicsEntities;

	void Start() {
		kinematicPhysicsEntities = GetComponentsInChildren<KinematicPhysicsEntity>();
	}

	void InitializeButtons() {
		ElevatorButton button;
		buttons = new ElevatorButton[stops.Length];

		Vector2 spawnPos = new Vector2(transform.position.x,
								transform.position.y +	// Base vertical position of the center of the elevator
								(SIZE.y / 2) +			// Get to the top of the elevator
								BUTTON_OFFSET / 2 );	// Add some padding before start of first button
		// Spawn button prefabs based on # of stops
		for (int i = 0; i < stops.Length; i++) {
			button = Instantiate(buttonPrefab, spawnPos, Quaternion.identity, transform).GetComponentInChildren<ElevatorButton>();
			button.gameObject.GetComponentInChildren<Text>().text = (i + 1).ToString();
			button.stopIndex = i;
			// TODO:
			// button.elevatorId = this.netId;
			buttons[i] = button;
			spawnPos.y += BUTTON_OFFSET;
		}

	}
	
	public void PhysicsUpdate() {
		if (PhotonNetwork.IsMasterClient) {
			if (isMoving) {
				MoveToTargetStop();
			} else {
				// TODO: this probably doesn't need to run every single physics update
				// Check for entity within borders
				Vector2 halfSize = SIZE / 2;
				passengers = Physics2D.OverlapAreaAll((Vector2)transform.position - halfSize,
												(Vector2)transform.position + halfSize,
												Utility.GetLayerMask("character"));
				Debug.DrawLine((Vector2)transform.position - halfSize, (Vector2)transform.position + halfSize);
				if (passengers.Length > 0) {
					// Show buttons on client
					RpcSetButtonActive(true);
				} else {
					// Hide buttons on client
					RpcSetButtonActive(false);
				}
			}
			RpcUpdateServerPosition(transform.position);
		} else {
			transform.position = Vector3.Lerp(transform.position, serverPosition, LAG_LERP_FACTOR);
		}
		// Update each kinematicPhysicsEntity in this component's children (floor/ceiling)
		foreach(KinematicPhysicsEntity entity in kinematicPhysicsEntities) {
			entity.PhysicsUpdate();
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
				// Disable this floor's button
				RpcDisableButton(targetStop);
			} else {
				transform.position = Vector3.MoveTowards(transform.position, targetPosition, potentialMovement);
			}

	}

	void SetButtonActive(bool isActive) {
		for (int i = 0; i < buttons.Length; i++) {
			buttons[i].gameObject.SetActive(isActive);
		}
	}

	// Commands
	public void CmdCallToStop(int indexOfStop) {
		RpcEnableButton(targetStop);
		targetStop = indexOfStop;
		isMoving = true;
		SetButtonActive(false);
		RpcSetButtonActive(false);
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

	public void RpcSetStopCoordinates(float[] stops) {
		this.stops = stops;
		// TODO:
		// InitializeButtons();
	}

	void RpcDisableButton(int index) {
		buttons[index].isDisabled = true;
	}

	void RpcEnableButton(int index) {
		buttons[index].isDisabled = false;
	}
}
