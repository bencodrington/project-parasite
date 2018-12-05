using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour {

	ObjectManager objectManager;

	void Start() {
		objectManager = gameObject.GetComponent<ObjectManager>();
	}

	void FixedUpdate() {
		objectManager.PhysicsUpdate();
		// TODO: optimize
		foreach(Character character in FindObjectsOfType<Character>()) {
			character.PhysicsUpdate();
		}
	}
}
