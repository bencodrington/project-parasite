using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class ObjectManager : MonoBehaviourPun {

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

	#region [Private Variables]

	GameObject elevatorPrefab;
	
	List<Elevator> elevators;
	
	#endregion

	#region [Public Methods]
	
	public void OnRoundStart() {
		// Elevators only need to be spawned on one client and propagated to the rest
		if (!PhotonNetwork.IsMasterClient) { return; }
		elevatorPrefab = Resources.Load("Elevator") as GameObject;
		SpawnElevators();
	}

	public void OnRoundEnd() {
		if (!PhotonNetwork.IsMasterClient) { return; }
		DestroyElevators();
		Destroy(gameObject);
	}

	public void PhysicsUpdate() {
		// Note: each object that needs to be updated every physics update needs to be
		// 	added (via Rpc) to the client ObjectManager's list of objects to update
		if (elevators == null) { return; }
		foreach (Elevator elevator in elevators) {
			elevator.PhysicsUpdate();
		}
	}
	
	#endregion

	#region [MonoBehaviour Callbacks]
	
	void Awake() {
		// Initialize lists of elevators
		elevators = new List<Elevator>();
	}
	
	#endregion

	#region [Private Methods]

	void SpawnElevators() {
		int elevatorViewId;
		// This should only ever be called on the Master Client
		foreach (ElevatorData elevatorData in elevatorDataArray) {
			elevatorViewId = SpawnElevator(elevatorData);
			photonView.RPC("RpcStoreElevator", RpcTarget.All, elevatorViewId);
		}
	}

	int SpawnElevator(ElevatorData elevatorData) {
		// Instantiate GameObject
		GameObject elevatorGameObject = PhotonNetwork.Instantiate(
											elevatorPrefab.name,
											GetElevatorSpawnCoordinates(elevatorData),
											Quaternion.identity);
		Elevator elevator = elevatorGameObject.GetComponent<Elevator>();
		float[] yCoordinates = new float[elevatorData.stops.Length];
		bool[] isOnRightSideValues = new bool[yCoordinates.Length];
		for (int i = 0; i < yCoordinates.Length; i++) {
			yCoordinates[i] = elevatorData.stops[i].yCoordinate;
			isOnRightSideValues[i] = elevatorData.stops[i].isOnRightSide;
		}
		// Let all copies of the elevator know what their stops are
		elevator.photonView.RPC("RpcSetStopData", RpcTarget.All, yCoordinates, isOnRightSideValues);
		return elevator.photonView.ViewID;
	}

	Vector2 GetElevatorSpawnCoordinates(ElevatorData elevator) {
		Vector2 range = elevator.GetVerticalRange();
		float yCoordinate = Random.Range(range.x, range.y);
		return new Vector2(elevator.xCoordinate, yCoordinate);
	}

	void DestroyElevators() {
		foreach(Elevator elevator in elevators) {
			PhotonNetwork.Destroy(elevator.gameObject);
		}
	}

	#endregion

	[PunRPC]
	void RpcStoreElevator(int elevatorViewId) {
		// This function is used to cache references to elevators that have been spawned
		// 	on the server. Otherwise the client ObjectManager will not know which elevators to update
		// 	each PhysicsUpdate.
		Elevator elevator = PhotonView.Find(elevatorViewId).GetComponentInChildren<Elevator>();
		// CLEANUP: ensure that elevators list has been instantiated before adding to it
		if (elevators == null) {
			elevators = new List<Elevator>();
		}
		elevators.Add(elevator);
	}
}
