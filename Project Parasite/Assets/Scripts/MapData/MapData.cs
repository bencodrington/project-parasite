using UnityEngine;

[CreateAssetMenu (menuName = "Scriptable Objects/MapData")]
public class MapData : ScriptableObject {

    [System.Serializable]
	public struct StopData {
        public string name;
		public float yCoordinate;
        // Which side of the platform the stop should be spawned
		public bool isOnRightSide;
	}

    [System.Serializable]
    public struct PlatformData {
        public float xCoordinate;
        public StopData[] stops;

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

    // Represents the origin point of the map, where any other coordinates are
    //  relative to
    public Vector2 mapOrigin;
	// Where players should be spawned in debug mode, relative to the origin
	public Vector2 debugSpawnCoordinates;
    // An array of each of the platforms on this map and their associated stop data
    public PlatformData[] platforms;

}
