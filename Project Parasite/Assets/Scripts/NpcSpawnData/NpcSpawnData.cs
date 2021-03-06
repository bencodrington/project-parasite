﻿using UnityEngine;

[CreateAssetMenu (menuName = "Scriptable Objects/NpcSpawnData")]
public class NpcSpawnData : ScriptableObject {
    [System.Serializable]
	public struct spawnPoint {
        public bool isStationary;
        public bool isInfected;
        public Vector2 coordinates;
        public spawnPoint(bool _isStationary, Vector2 _coordinates, bool _isInfected = false) {
            isStationary = _isStationary;
            coordinates = _coordinates;
            isInfected = _isInfected;
        }
    }
    [System.Serializable]
	public struct playableCharacterSpawnPoint {
        public bool isParasite;
        public Vector2 coordinates;
        public playableCharacterSpawnPoint(bool _isParasite, Vector2 _coordinates) {
            isParasite = _isParasite;
            coordinates = _coordinates;
        }
    }

    public bool shouldSpawnClusters; 
    public spawnPoint[] spawnPoints;
    public playableCharacterSpawnPoint[] playableCharacterSpawnPoints;
}
