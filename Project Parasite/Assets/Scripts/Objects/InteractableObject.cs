using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class InteractableObject : MonoBehaviour {
    // Whether the object is in range of being interacted with
    bool _isInRange = false;

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

    public void SetIsInRange(bool isInRange) {
        _isInRange = isInRange;
        if (isInRange) {
            OnIsInRange();
        } else {
            OnIsOutOfRange();
        }
    }

    protected virtual void OnIsInRange() {
        ShowControlKey();
    }
    protected virtual void OnIsOutOfRange() {
        HideControlKey();
    }

    void ShowControlKey() {
        controlKey.SetActive(true);
    }

    void HideControlKey() {
        controlKey.SetActive(false);
    }

    public abstract void OnInteract();

}
