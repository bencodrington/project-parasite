using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class ObjectManager : MonoBehaviourPun {

	#region [Private Variables]

	GameObject elevatorPrefab;
	
	List<Elevator> elevators;

	MapData mapData;
	
	#endregion

	#region [Public Methods]
	
	public void OnRoundStart() {
		// Elevators only need to be spawned on one client and propagated to the rest
		if (!PhotonNetwork.IsMasterClient) { return; }
		elevatorPrefab = Resources.Load("Elevator") as GameObject;
		SpawnPlatforms();
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

	public void SetMap(MapData _mapData) {
		mapData = _mapData;
	}
	
	#endregion

	#region [MonoBehaviour Callbacks]
	
	void Awake() {
		// Initialize lists of elevators
		elevators = new List<Elevator>();
	}
	
	#endregion

	#region [Private Methods]

	void SpawnPlatforms() {
		int elevatorViewId;
		if (mapData == null) {
			Debug.LogError("ObjectManager:SpawnPlatforms():mapData not set");
			return;
		}
		// This should only ever be called on the Master Client
		foreach (MapData.PlatformData platformData in mapData.platforms) {
			elevatorViewId = SpawnPlatform(platformData, mapData.mapOrigin);
			photonView.RPC("RpcStoreElevator", RpcTarget.All, elevatorViewId);
		}
	}

	int SpawnPlatform(MapData.PlatformData platformData, Vector2 mapOrigin) {
		// Instantiate GameObject
		GameObject platformGameObject = PhotonNetwork.Instantiate(
											elevatorPrefab.name,
											mapOrigin + GetPlatformSpawnCoordinates(platformData),
											Quaternion.identity);
		Elevator elevator = platformGameObject.GetComponent<Elevator>();
		float[] yCoordinates = new float[platformData.stops.Length];
		bool[] isOnRightSideValues = new bool[yCoordinates.Length];
		for (int i = 0; i < yCoordinates.Length; i++) {
			yCoordinates[i] = mapOrigin.y + platformData.stops[i].yCoordinate;
			isOnRightSideValues[i] = platformData.stops[i].isOnRightSide;
		}
		// Let all copies of the elevator know what their stops are
		elevator.photonView.RPC("RpcSetStopData", RpcTarget.All, yCoordinates, isOnRightSideValues);
		return elevator.photonView.ViewID;
	}

	Vector2 GetPlatformSpawnCoordinates(MapData.PlatformData platformData) {
		Vector2 range = platformData.GetVerticalRange();
		float yCoordinate = Random.Range(range.x, range.y);
		return new Vector2(platformData.xCoordinate, yCoordinate);
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
