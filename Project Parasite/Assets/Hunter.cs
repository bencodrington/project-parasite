using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Hunter : PlayerCharacter {

	protected override void HandleInput()  {
		// Movement
		float movementX = Input.GetAxis("Horizontal") * movementSpeed * Time.deltaTime;
		// Has authority, so translate immediately
		transform.Translate(movementX, 0, 0);
	}
	
	public override void ImportStats() {
		height = .5f;
		movementSpeed = 10f;
		type = "HUNTER";
	}
}
