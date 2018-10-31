using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

// TODO: rename playercharacter?
public class NonPlayerCharacter : PlayerCharacter {


	public override void Update() {
		base.Update();
		if (isServer && physicsEntity != null) {
			physicsEntity.Update();
			serverPosition = transform.position;
		}
	}
	// COMMANDS
	// CLIENTRPC

    public override void ImportStats()
    {
		height = .5f;
		movementSpeed = 8f;
		type = "NPC";
    }

    protected override void HandleInput()
    {
        // TODO: if being controlled, take input
    }

	[ClientRpc]
	public void RpcSetColour() {
		GetComponentInChildren<SpriteRenderer>().color = Color.yellow;
	}
}
