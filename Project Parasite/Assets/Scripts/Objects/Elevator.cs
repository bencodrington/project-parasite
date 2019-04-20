using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

public class Elevator : MonoBehaviourPun {

	#region [Public Variables]
	
	public GameObject buttonPrefab;
	public GameObject elevatorCallFieldPrefab;
	
	#endregion

	#region [Private Variables]

	// How far from the elevator's center should the callfield center be
	const float STOP_X_OFFSET = 2f;
	// The higher this value is, the snappier lag correction will be
	const float LAG_LERP_FACTOR = 0.1f;
	const float MOVEMENT_SPEED = 8f;
	// The vertical distance between elevator buttons
	const float BUTTON_OFFSET = 0.6f;
	Vector2 SIZE = new Vector2(2, 3);
	float[] stopYCoordinates;
	int targetStop;
	Vector3 estimatedServerPosition;
	bool isMoving = false;
	
	Collider2D[] passengers;
	ElevatorButton[] buttons;
	List<ElevatorCallField> callFields;

	KinematicPhysicsEntity[] kinematicPhysicsEntities;
	
	#endregion


	#region [Public Methods]
	
	public void PhysicsUpdate() {
		HandlePassengers();
		if (PhotonNetwork.IsMasterClient && isMoving) {
			transform.position = GetPositionAfterOneMovementFrame(transform.position);
			photonView.RPC("RpcUpdateServerPosition", RpcTarget.All, transform.position);
		} else if (isMoving) {
			// Remote client, so update our estimated position
			estimatedServerPosition = GetPositionAfterOneMovementFrame(estimatedServerPosition);
			transform.position = Vector3.Lerp(transform.position, estimatedServerPosition, 1);
		}
		// Update each kinematicPhysicsEntity in this component's children (floor/ceiling)
		foreach(KinematicPhysicsEntity entity in kinematicPhysicsEntities) {
			entity.PhysicsUpdate();
		}
		// Update each callfield (a.k.a. stop) that belongs to this elevator
		foreach(ElevatorCallField callField in callFields) {
			callField.PhysicsUpdate();
		}
	}

	public void CallToStop(int stopIndex) {
		photonView.RPC("RpcCallToStop", RpcTarget.All, stopIndex);
	}
	
	#endregion

	#region [MonoBehaviour Callbacks]
	
	void Awake() {
		kinematicPhysicsEntities = GetComponentsInChildren<KinematicPhysicsEntity>();
		callFields = new List<ElevatorCallField>();
	}

	void OnDestroy() {
		foreach (ElevatorCallField callField in callFields) {
			Destroy(callField.gameObject);
		}
	}
	
	#endregion

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
		// CLEANUP: this can be cleaner
		xCoordinate += isOnRightSide ? STOP_X_OFFSET - 1f : -STOP_X_OFFSET - 1f;
		// CLEANUP: replace magic number, half of elevator height
		return new Vector2(xCoordinate, yCoordinate - 1.5f);
	}

	void InitializeButtons(int buttonCount) {
		ElevatorButton button;
		buttons = new ElevatorButton[buttonCount];

		Vector2 spawnPos = new Vector2(transform.position.x,
								transform.position.y +	// Base vertical position of the center of the elevator
								(SIZE.y / 2) +			// Get to the top of the elevator
								BUTTON_OFFSET / 2 );	// Add some padding before start of first button
		// Spawn button prefabs based on # of stops
		for (int i = 0; i < buttonCount; i++) {
			button = Instantiate(buttonPrefab, spawnPos, Quaternion.identity, transform).GetComponentInChildren<ElevatorButton>();
			button.gameObject.GetComponentInChildren<Text>().text = (i + 1).ToString();
			button.stopIndex = i;
			button.elevator = this;
			buttons[i] = button;
			spawnPos.y += BUTTON_OFFSET;
		}
	}

	void DisableButton(int index) {
		buttons[index].isDisabled = true;
	}

	void EnableButton(int index) {
		buttons[index].isDisabled = false;
	}

	void HandlePassengers() {
		// Only show buttons when a character enters a halted elevator
		if (isMoving) { return; }
		// Check for entity within borders
		Vector2 halfSize = SIZE / 2;
		passengers = Physics2D.OverlapAreaAll((Vector2)transform.position - halfSize,
										(Vector2)transform.position + halfSize,
										Utility.GetLayerMask("character"));
		// Only show elevator buttons if the client's character is in the elevator
		Character character;
		foreach (Collider2D collider in passengers) {
			character = Utility.GetCharacterFromCollider(collider);
			if (character.photonView.IsMine && !character.IsUninfectedNpc()) {
				// Show buttons on client
				SetAllButtonsActive(true);
				return;
			} 
		}
		// Client's character is not a passenger, so hide buttons
		SetAllButtonsActive(false);
	}

	Vector2 GetPositionAfterOneMovementFrame(Vector2 currentPosition) {
		Vector2 targetPosition;
		float potentialMovement = MOVEMENT_SPEED * Time.deltaTime;
		targetPosition = new Vector2(transform.position.x, stopYCoordinates[targetStop]);
		if (Vector3.Distance(transform.position, targetPosition) < potentialMovement) {
			isMoving = false;
			// Disable this floor's button
			DisableButton(targetStop);
			// Destination reached
			return targetPosition;
		} else {
			return Vector3.MoveTowards(transform.position, targetPosition, potentialMovement);
		}
	}

	void SetAllButtonsActive(bool isActive) {
		for (int i = 0; i < buttons.Length; i++) {
			buttons[i].gameObject.SetActive(isActive);
		}
	}
	
	#endregion

	[PunRPC]
	public void RpcSetStopData(float[] yCoordinates, bool[] isOnRightSideValues) {
		SpawnStops(yCoordinates, isOnRightSideValues);
		InitializeButtons(yCoordinates.Length);
		stopYCoordinates = yCoordinates;
	}

	[PunRPC]
	public void RpcCallToStop(int indexOfStop) {
		EnableButton(targetStop);
		targetStop = indexOfStop;
		isMoving = true;
		SetAllButtonsActive(false);
	}

	[PunRPC]
	void RpcUpdateServerPosition(Vector3 newPosition) {
		if (PhotonNetwork.IsMasterClient) { return; }
		// Else, on a client machine, so update our record of the elevator's true position
		estimatedServerPosition = newPosition;
	}
}
