using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrbBeamRangeManager : MonoBehaviour {

	// Number of indicators to display
	const int MARKER_COUNT = 6;
	// The maximum distance from the most recent orb that another can be placed
	//	to connect them
	const float ORB_BEAM_RANGE = 9f;

	public GameObject orbBeamRangeMarkerPrefab;

	// The orb that was most recently placed
	public Orb mostRecentOrb;

	// Set to false externally when there are no orbs remaining to be placed
	public bool shouldShowMarkers = true;
	
	GameObject[] markers;
	void Start() {
		// Spawn markers
		markers = new GameObject[MARKER_COUNT];
		for (int i = 0; i < MARKER_COUNT; i++) {
			markers[i] = Instantiate(orbBeamRangeMarkerPrefab);
			markers[i].transform.SetParent(transform);
		}
		HideMarkers();
	}

	void Update() {
		if (isInRange(Utility.GetMousePos()) && shouldShowMarkers) {
			ShowMarkers();
			PositionMarkers();
		} else {
			HideMarkers();
		}
	}

	public bool isInRange(Vector2 ofPosition) {
		if (mostRecentOrb == null) {
			return false;
		}
		return Vector2.Distance(mostRecentOrb.transform.position, ofPosition) <= ORB_BEAM_RANGE;
	}

	void ShowMarkers() {
		// Return early if the first marker is already active
		if (markers == null || markers[0].gameObject.activeInHierarchy) { return; }
		foreach(GameObject marker in markers) {
			marker.gameObject.SetActive(true);
		}
	}

	void HideMarkers() {
		// Return early if the first marker is already inactive
		if (markers == null || !markers[0].gameObject.activeInHierarchy) { return; }
		foreach(GameObject marker in markers) {
			marker.gameObject.SetActive(false);
		}
	}

	void PositionMarkers() {
		GameObject marker;
		Vector2 start = mostRecentOrb.transform.position;
		Vector2 end = Utility.GetMousePos();
		for (int i = 0; i < MARKER_COUNT; i++) {
			marker = markers[i];
			// Include an imaginary marker on either end of the calculations
			// 	to replace the first and last ones which will be hidden by the
			// 	orb and hunter, respectively
			// Otherwise it would be (float) i / (MARKER_COUNT)
			float percentageToHunterPosition = (i + 1f) / (MARKER_COUNT + 1);
			marker.transform.position = Vector2.Lerp(start, end, percentageToHunterPosition);
		}
	}
}
