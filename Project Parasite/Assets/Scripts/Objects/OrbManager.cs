using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrbManager : MonoBehaviour
{
    #region [Public Variables]
    
    public GameObject orbPrefab;
    public GameObject orbBeamPrefab;
    public OrbSetData orbSetData;
    
    #endregion

    #region [Private Variables]
    
    Orb mostRecentOrb;
    List<Orb> orbs;
    
    #endregion

    #region [Public Methods]
    
    public void DestroyOrbs() {
        foreach(Orb orb in orbs) {
            Destroy(orb.gameObject);
        }
        Destroy(gameObject);
    }
    
    #endregion

    #region [MonoBehaviour Callbacks]

    void Awake() {
        orbs = new List<Orb>();
        SpawnOrbSet();
    }

    #endregion

    #region [Private Methods]
    
    void SpawnOrbSet() {
        bool shouldConnectToPrevious;
        foreach (OrbSetData.OrbChain chain in orbSetData.orbChains) {
            // The first orb in each chain has nothing to connect to
            shouldConnectToPrevious = false;
            foreach (Vector2 position in chain.positions) {
                SpawnOrb(position, shouldConnectToPrevious);
                shouldConnectToPrevious = true;
            }
        }
    }

    void SpawnOrb(Vector2 atPosition, bool shouldConnectToPrevious) {
		Vector2 beamSpawnPosition;
		// Create orb game object
		GameObject orbGameObject = Instantiate(orbPrefab, atPosition, Quaternion.identity);
		Orb orb = orbGameObject.GetComponent<Orb>();
		if (shouldConnectToPrevious && mostRecentOrb != null) {
			// Spawn beam halfway between orbs
			beamSpawnPosition = Vector2.Lerp(mostRecentOrb.transform.position, atPosition, 0.5f);
			OrbBeam orbBeam = Instantiate(orbBeamPrefab, beamSpawnPosition, Quaternion.identity).GetComponent<OrbBeam>();
			// Store beam in most recent orb so when the orb is destroyed it can take the beam with it
			mostRecentOrb.AttachBeam(orbBeam);
			orbBeam.Initialize(mostRecentOrb.transform.position, atPosition);
		}
		// Add to list
		orbs.Add(orb);
		// Update reference to most recent orb for connecting the next orb
		mostRecentOrb = orb;
    }
    
    #endregion

}
