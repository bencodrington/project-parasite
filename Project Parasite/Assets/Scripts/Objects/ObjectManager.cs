using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class ObjectManager : MonoBehaviourPun {

	GameObject elevatorPrefab;
	public GameObject elevatorCallFieldPrefab;

	// How far from the elevator's center should the callfield center be
	const float STOP_X_OFFSET = 2f;

	class StopData {
		public float yCoordinate;
		public bool isOnRightSide;
		public StopData(float yCoordinate, bool isOnRightSide){
			this.yCoordinate = yCoordinate;
			this.isOnRightSide = isOnRightSide;
		}
	}
	class ElevatorData {
		public float xCoordinate;
		public StopData[] stops;
		public ElevatorData(float xCoordinate, StopData[] stops) {
			this.xCoordinate = xCoordinate;
			this.stops = stops;
		}

		public Vector2 GetVerticalRange() {
			if (stops.Length == 0) { return Vector2.zero; }
			// Initialize range min and max to first stop's yCoordinate
			Vector2 range = new Vector2(stops[0].yCoordinate, stops[0].yCoordinate);
			// For each of the remaining stops
			for (int i = 1; i < stops.Length; i++) {
				// Update range to include current stop's yCoordinate
				if (stops[i].yCoordinate < range.x) {
					range.x = stops[i].yCoordinate;
				} else if (stops[i].yCoordinate > range.y) {
					range.y = stops[i].yCoordinate;
				}
			}
			return range;
		}

	}
	ElevatorData[] elevatorDataArray = {
		new ElevatorData(-15f, new StopData[] {
			new StopData(-8.5f, false),
			new StopData(-0.5f, true),
			new StopData(11.5f, true),
			new StopData(23.5f, true),
			new StopData(35.5f, false),
		}),
		new ElevatorData(15f, new StopData[] {
			new StopData(-8.5f, true),
			new StopData(-0.5f, false),
			new StopData(11.5f, false),
			new StopData(23.5f, false),
			new StopData(35.5f, true),
		})
	};

	List<Elevator> elevators;
	List<ElevatorCallField> callFields;

	#region [Public Methods]
	
	public void OnRoundStart() {
		// Elevators only need to be spawned on one client and propagated to the rest
		if (!PhotonNetwork.IsMasterClient) { return; }
		elevatorPrefab = Resources.Load("Elevator") as GameObject;
		SpawnElevators();
	}

	public void OnRoundEnd() {
		if (!PhotonNetwork.IsMasterClient) { return; }
		// DestroyElevators();
		// DestroyCallFields();
	}

	public void PhysicsUpdate() {
	// 	// Note: each object that needs to be updated every physics update needs to be
	// 	// 	added (via Rpc) to the client ObjectManager's list of objects to update
	// 	if (elevators == null) { return; }
	// 	foreach (Elevator elevator in elevators) {
	// 		elevator.PhysicsUpdate();
	// 	}
	// 	// Call fields are only updated every physics update on the server, and are
	// 	// 	not stored on the client ObjectManager
	// 	if (!isServer) { return; }
	// 	foreach (ElevatorCallField callField in callFields) {
	// 		callField.PhysicsUpdate();
	// 	}
	}
	
	#endregion

	#region [MonoBehaviour Callbacks]
	
	void Awake() {
		// Initialize lists of elevators & callfields(a.k.a. stops)
		elevators = new List<Elevator>();
		callFields = new List<ElevatorCallField>();
	}
	
	#endregion

	#region [Private Methods]

	void SpawnElevators() {
		foreach (ElevatorData elevatorData in elevatorDataArray) {
			SpawnElevator(elevatorData);
			// TODO:
			// RpcStoreElevator(elevatorNetId);
		}
	}

	void SpawnElevator(ElevatorData elevatorData) {
		// Instantiate GameObject
		GameObject elevatorGameObject = PhotonNetwork.Instantiate(
											elevatorPrefab.name,
											GetElevatorSpawnCoordinates(elevatorData),
											Quaternion.identity);
		Elevator elevator = elevatorGameObject.GetComponent<Elevator>();
		float[] yCoordinates = new float[elevatorData.stops.Length];
		for (int i = 0; i < yCoordinates.Length; i++) {
			yCoordinates[i] = elevatorData.stops[i].yCoordinate;
		}
		// Let all copies of the elevator know what their stops are
		elevator.RpcSetStopCoordinates(yCoordinates);
		// TODO:
	// 	// Add to server master list of elevators
	// 	elevators.Add(elevator);
	// 	// Spawn the stops that belong to it
	// 	SpawnStops(elevatorData, elevator);
	}

	Vector2 GetElevatorSpawnCoordinates(ElevatorData elevator) {
		Vector2 range = elevator.GetVerticalRange();
		float yCoordinate = Random.Range(range.x, range.y);
		return new Vector2(elevator.xCoordinate, yCoordinate);
	}

	#endregion


	

	// void SpawnStops(ElevatorData elevatorData, Elevator elevator) {
	// 	for (int i = 0; i < elevatorData.stops.Length; i++) {
	// 		SpawnStop(elevatorData.stops[i], i, elevatorData.xCoordinate, elevator);
	// 	}
	// }

	// void SpawnStop(StopData stop, int index, float xCoordinate, Elevator elevator) {
	// 	// Instantiate GameObject
	// 	GameObject callFieldGameObject = GameObject.Instantiate(
	// 							elevatorCallFieldPrefab,
	// 							GetStopSpawnCoordinates(stop, xCoordinate),
	// 							Quaternion.identity);
	// 	// Share with clients
	// 	NetworkServer.Spawn(callFieldGameObject);
	// 	ElevatorCallField callField = callFieldGameObject.GetComponent<ElevatorCallField>();
	// 	// Client callfields don't need to know their elevator nor stopIndex
	// 	// 	because all checking for callers and calling is done server-side
	// 	callField.elevator = elevator;
	// 	callField.stopIndex = index;
	// 	callFields.Add(callField);
	// }

	// Vector2 GetStopSpawnCoordinates(StopData stop, float xCoordinate) {
	// 	// -1f is because call fields are positioned by their bottom left corner, not the middle
	// 	// TODO: that can be cleaner
	// 	xCoordinate += stop.isOnRightSide ? STOP_X_OFFSET - 1f : -STOP_X_OFFSET - 1f;
	// 	// TODO: replace magic number, half of elevator height
	// 	return new Vector2(xCoordinate, stop.yCoordinate - 1.5f);
	// }

	// void DestroyElevators() {
	// 	foreach(Elevator elevator in elevators) {
	// 		NetworkServer.Destroy(elevator.gameObject);
	// 	}
	// }

	// void DestroyCallFields() {
	// 	foreach(ElevatorCallField callField in callFields) {
	// 		NetworkServer.Destroy(callField.gameObject);
	// 	}
	// }

	// // ClientRpc
	// [ClientRpc]
	// void RpcStoreElevator(NetworkInstanceId elevatorNetId) {
	// 	// This function is used to cache references (on the client) to elevators that have been spawned
	// 	// 	on the server. Otherwise the client ObjectManager will not know which elevators to update
	// 	// 	each PhysicsUpdate.
	// 	if (isServer) { return; }
	// 	Elevator elevator = Utility.GetLocalObject(elevatorNetId, isServer).GetComponentInChildren<Elevator>();
	// 	// TODO: cleanup: ensure that elevators list has been instantiated before adding to it
	// 	if (elevators == null) {
	// 		elevators = new List<Elevator>();
	// 	}
	// 	elevators.Add(elevator);
	// }
}
