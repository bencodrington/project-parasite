﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Parasite : PlayerCharacter {

	private float jumpVelocity = .25f;

	protected override void HandleInput()  {
		// Movement
		float movementX = Input.GetAxis("Horizontal") * movementSpeed * Time.deltaTime;
		// TODO: check if on ground
		if (Input.GetKeyDown(KeyCode.W)) {
			physicsEntity.AddVelocity(0, jumpVelocity);
		}
		// Has authority, so translate immediately
		transform.Translate(movementX, 0, 0);
	}
	
	public override void ImportStats() {
		// TODO: get stats like this from imported files
		height = .25f;
		movementSpeed = 15f;
		type = "PARASITE";
	}
}
