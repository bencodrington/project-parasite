using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class InteractableObject : MonoBehaviour {

	Vector2 SIZE = new Vector2(2, 4);
    
	private Collider2D[] charactersInRange = new Collider2D[0];
	private Collider2D[] oldCharactersInRange = new Collider2D[0];

    // The 'E' key indicator that is shown when in range
    public GameObject controlKeyPrefab;
    GameObject controlKey;

    // How far from this root's transform the key icon should be displayed
    protected Vector2 controlKeyOffset = new Vector2(1, 2);

    public virtual void Start() {
        // Create and hide the key icon
        controlKey = Instantiate(controlKeyPrefab, transform.position + (Vector3)controlKeyOffset, Quaternion.identity);
        controlKey.transform.parent = transform;
        controlKey.SetActive(false);
    }

    public void PhysicsUpdate() {
		// OPTIMIZE: this probably doesn't need to run every single physics update
		// Check for entity within borders
		charactersInRange = Physics2D.OverlapAreaAll(transform.position,
										transform.position + new Vector3(SIZE.x, SIZE.y, 0),
										Utility.GetLayerMask("character"));
		ResolveCharacterListDiff(oldCharactersInRange, charactersInRange);
		oldCharactersInRange = charactersInRange;
	}

    #region [Private Methods]
    
	void ResolveCharacterListDiff(Collider2D[] oldCallers, Collider2D[] callers) {
        Character character;
		// OPTIMIZE: move this logic to the local character because they only need to maintain a list of overlapped objects
		// OPTIMIZE:		at one point rather than a whole area.
		// OPTIMIZE: then just verify calls on the server
		// It's okay to mess up the oldCallers array,
		// 	but callers should be preserved outside this method
		callers = (Collider2D[])callers.Clone();
		// Loop through each array
		for (int i=0; i < oldCallers.Length; i++) {
			for (int j=0; j < callers.Length; j++) {
				// If the current new caller matches this old caller
				if (callers[j] != null && callers[j] == oldCallers[i]) {
					// Cross them both off, this collider has not changed
					oldCallers[i] = null;
					callers[j] = null;
					// Move on to next old caller
					break;
				}
			}
		}
		// Now, the only values that arent null are:
		// 	- those that have left the field in the oldCallers array
		//	- those that have just entered the field in the callers array
        // FIXME: this whole thing can do with a LOT of optimization
		foreach (Collider2D oldCaller in oldCallers) {
			if (oldCaller != null) {
				try {
					// Character is leaving the field
					character = Utility.GetCharacterFromCollider(oldCaller);
					if (character.photonView.IsMine) {
						character.UnregisterInteractableObject(this);
						HideControlKey();
					}
				} catch { /* Parasite infected an NPC while in range */ }
			}
		}
		foreach (Collider2D caller in callers) {
			if (caller != null) {
				// Character is entering the field
				character = Utility.GetCharacterFromCollider(caller);
                if (character.photonView.IsMine) {
                    character.RegisterInteractableObject(this);
					if (!character.IsUninfectedNpc()) {
						ShowControlKey();
					}
                }
			}
		}
	}

    void ShowControlKey() {
        controlKey.SetActive(true);
    }

    void HideControlKey() {
        controlKey.SetActive(false);
    }
    
    #endregion

    public abstract void OnInteract();

}
