using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class ObjectManager : NetworkBehaviour {

	public GameObject elevatorPrefab;
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

	public void OnRoundStart() {
		SpawnElevators();
	}

	void SpawnElevators() {
		foreach (ElevatorData elevatorData in elevatorDataArray) {
			SpawnElevator(elevatorData);
		}
	}

	void SpawnElevator(ElevatorData elevatorData) {
		// TODO: spawn as children
		GameObject elevatorGameObject = GameObject.Instantiate(
											elevatorPrefab,
											GetElevatorSpawnCoordinates(elevatorData),
											Quaternion.identity);
		NetworkServer.Spawn(elevatorGameObject);
		Elevator elevator = elevatorGameObject.GetComponent<Elevator>();
		float[] yCoordinates = new float[elevatorData.stops.Length];
		for (int i = 0; i < yCoordinates.Length; i++) {
			yCoordinates[i] = elevatorData.stops[i].yCoordinate;
		}
		// TODO: RPC set stops & initialize buttons
		elevator.stops = yCoordinates;
		elevator.size = new Vector2(2, 3); // TODO: extract magic numbers
		elevator.InitializeButtons();

		// TODO: RPC set elevator
		SpawnStops(elevatorData, elevator);
	}

	Vector2 GetElevatorSpawnCoordinates(ElevatorData elevator) {
		Vector2 range = elevator.GetVerticalRange();
		float yCoordinate = Random.Range(range.x, range.y);
		return new Vector2(elevator.xCoordinate, yCoordinate);
	}

	void SpawnStops(ElevatorData elevatorData, Elevator elevator) {
		for (int i = 0; i < elevatorData.stops.Length; i++) {
			SpawnStop(elevatorData.stops[i], i, elevatorData.xCoordinate, elevator);
		}
	}

	void SpawnStop(StopData stop, int index, float xCoordinate, Elevator elevator) {
		GameObject callFieldGameObject = GameObject.Instantiate(
								elevatorCallFieldPrefab,
								GetStopSpawnCoordinates(stop, xCoordinate),
								Quaternion.identity);
		NetworkServer.Spawn(callFieldGameObject);
		ElevatorCallField callField = callFieldGameObject.GetComponent<ElevatorCallField>();
		// TODO: extract magic numbers
		callField.size = new Vector2(2, 4);
		callField.elevator = elevator;
		callField.stopIndex = index;
	}

	Vector2 GetStopSpawnCoordinates(StopData stop, float xCoordinate) {
		// -1f is because call fields are positioned by their bottom left corner, not the middle
		// TODO: that can be cleaner
		xCoordinate += stop.isOnRightSide ? STOP_X_OFFSET - 1f : -STOP_X_OFFSET - 1f;
		// TODO: replace magic number, half of elevator height
		return new Vector2(xCoordinate, stop.yCoordinate -= 1.5f);
	}

	public void OnRoundEnd() {
		// TODO: destroy elevators
		// TODO: destroy callfields (done on elevator destroy?)
	}
}
