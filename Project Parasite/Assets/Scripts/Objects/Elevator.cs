using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

public class Elevator : MonoBehaviourPun {

	#region [Public Variables]
	
	public GameObject buttonPrefab;
	public GameObject elevatorCallFieldPrefab;
	public AudioClip elevatorArrivedSound;
	
	#endregion

	#region [Private Variables]

	// How far from the elevator's center should the callfield center be
	const float STOP_X_OFFSET = 2f;
	// The higher this value is, the snappier lag correction will be
	const float LAG_LERP_FACTOR = 0.1f;
	const float MOVEMENT_SPEED = 8f;
	// The vertical distance from the center of the platform gameobject to the
	// 	bottom of the first button
	const float BUTTON_OFFSET = 1.5f;
	// The vertical distance between elevator buttons
	const float BUTTON_SPACING = 0.6f;
	Vector2 SIZE = new Vector2(2, 3);
	float[] stopYCoordinates;
	int targetStop;
	Vector3 estimatedServerPosition;
	bool isMoving = false;
	
	Collider2D[] passengers;
	ElevatorButton[] buttons;
	List<ElevatorCallField> callFields;
    
    PlatformPhysicsEntity physicsEntity;

	AudioSource elevatorArrivedSource;
	
	#endregion


	#region [Public Methods]
	
	public void PhysicsUpdate() {
		float velocityY;
		HandlePassengers();
		if (isMoving) {//FIXME: deterministic physics (PhotonNetwork.IsMasterClient && isMoving) {
			// Calculate how much we should move this frame
			velocityY = GetPositionAfterOneMovementFrame(transform.position).y - transform.position.y;
			// Move physics entity and move passengers
			physicsEntity.Update(velocityY);
			// Update gameObject to match position
        	transform.Translate(Vector2.up * velocityY);
			// PlatformPhysicsEntity needs to move some passengers after the platform gameObject's transform has been moved
			physicsEntity.AfterUpdate();
			// Notify remote clients
			photonView.RPC("RpcUpdateServerPosition", RpcTarget.All, transform.position);
		} else if (isMoving) {
			// Remote client, so update our estimated position
			velocityY = GetPositionAfterOneMovementFrame(estimatedServerPosition).y - transform.position.y;
			// Move physics entity and move passengers
			physicsEntity.Update(velocityY);
			// Calculate where the gameObject should be
			estimatedServerPosition += Vector3.up * velocityY;
			// Lerp actual gameObject position towards estimated server position
			// TODO: this is currently snapping the platform to the estimated server position
			transform.position = Vector3.Lerp(transform.position, estimatedServerPosition, 1);
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
		callFields = new List<ElevatorCallField>();
		elevatorArrivedSource = Utility.AddAudioSource(gameObject, elevatorArrivedSound);
		Transform floorTransform = GetComponentInChildren<BoxCollider2D>().transform;
        physicsEntity = new PlatformPhysicsEntity(floorTransform, .05f, 1f);
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
		// Get the X coordinate on either the left or right of the elevator
		xCoordinate += isOnRightSide ? STOP_X_OFFSET : -STOP_X_OFFSET;
		// -1 is because call fields are positioned by their bottom left corner, not the middle
		xCoordinate -= 1;
		return new Vector2(xCoordinate, yCoordinate);
	}

	void InitializeButtons(int buttonCount) {
		ElevatorButton button;
		buttons = new ElevatorButton[buttonCount];

		Vector2 spawnPos = new Vector2(transform.position.x,
								transform.position.y +	// Base vertical position of the center of the elevator
								BUTTON_OFFSET );	// Add some padding before start of first button
		// Spawn button prefabs based on # of stops
		for (int i = 0; i < buttonCount; i++) {
			button = Instantiate(buttonPrefab, spawnPos, Quaternion.identity, transform).GetComponentInChildren<ElevatorButton>();
			button.gameObject.GetComponentInChildren<Text>().text = (i + 1).ToString();
			button.stopIndex = i;
			button.elevator = this;
			buttons[i] = button;
			spawnPos.y += BUTTON_SPACING;
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
		// The transform's position is measured from halfway up its height, so add half of its height
		// 	to make the platform line up with the floor
		targetPosition.y += SIZE.y / 2;
		if (Vector3.Distance(transform.position, targetPosition) < potentialMovement) {
			// Destination reached
			isMoving = false;
			elevatorArrivedSource.Play();
			// Disable this floor's button
			DisableButton(targetStop);
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
