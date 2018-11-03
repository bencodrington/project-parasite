using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

// TODO: rename playercharacter?
public class NonPlayerCharacter : PlayerCharacter {

	private bool isInfected;

	public override void Update() {
		if (isInfected && hasAuthority) {
			// NPC is infected and this client is the Parasite player's client
			HandleInput();
			physicsEntity.Update();
			CmdUpdatePosition(transform.position);
		} else if (!isInfected && isServer && physicsEntity != null) {
			// Not infected, so the server should handle physics and be the authority on the NPC's position
			physicsEntity.Update();
			CmdUpdatePosition(transform.position);
		} else {
			// This is a cloned representation of the authoritative NPC
			// 	So just verify current position is up to date with server position
			transform.position = Vector3.SmoothDamp(transform.position, serverPosition, ref serverPositionSmoothVelocity, 0.1f);
		}
	}

    public override void ImportStats()
    {
		height = .5f;
		movementSpeed = 8f;
		type = "NPC";
		isInfected = false;
    }

    protected override void HandleInput()
    {
		// This function is only called when this NPC is infected,
		// 	and is only called on the Parasite player's client

		// Movement
		float movementX = Input.GetAxis("Horizontal") * movementSpeed * Time.deltaTime;
		// Has authority, so translate immediately
		transform.Translate(movementX, 0, 0);

		// Self Destruct
		if (Input.GetMouseButtonDown(0)) {
			// Destroy this NPC
			CmdDespawnSelf();
		}
    }

	// COMMANDS

	[Command]
	void CmdDespawnSelf() {
		// Spawn new Parasite Object
		playerObject.SpawnPlayerCharacter("PARASITE");
		// Despawn this NPC object
		FindObjectOfType<NpcManager>().DespawnNpc(netId);
	}

	// CLIENTRPC

	[ClientRpc]
	public void RpcSetLocalPlayerAuthority(bool newValue) {
		GetComponentInChildren<NetworkIdentity>().localPlayerAuthority = newValue;
	}

	[ClientRpc]
	public void RpcInfect() {
		isInfected = true;
		if (hasAuthority) {
			// Only update sprite if on the Parasite player's client
			GetComponentInChildren<SpriteRenderer>().color = Color.magenta;
		}
	}
}
