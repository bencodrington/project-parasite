using UnityEngine;

[CreateAssetMenu (menuName = "Scriptable Objects/OrbSetData")]
public class OrbSetData : ScriptableObject {

    public OrbChain[] orbChains;
    
    [System.Serializable]
    public struct OrbChain {
        public Vector2[] positions;
    }
}
