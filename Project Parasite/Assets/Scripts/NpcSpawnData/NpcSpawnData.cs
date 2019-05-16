using UnityEngine;

[CreateAssetMenu (menuName = "Scriptable Objects/NpcSpawnData")]
public class NpcSpawnData : ScriptableObject {
    [System.Serializable]
	public struct spawnPoint {
        public bool isStationary;
        public Vector2 coordinates;
    }
    public bool shouldSpawnClusters; 
    public spawnPoint[] spawnPoints;
}
