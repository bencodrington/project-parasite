using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

// TODO: rename playercharacter?
public class NonPlayerCharacter : PlayerCharacter {

	public bool isInfected;

	// TODO: update to use fixedupdate for physics
	public override void Update() {
		if (isInfected && hasAuthority) {
			// NPC is infected and this client is the Parasite player's client
			HandleInput();
		} else if (!isInfected && isServer && physicsEntity != null) {
			// Not infected, so the server should handle physics and be the authority on the NPC's position
		} else {
			// This is a cloned representation of the authoritative NPC
			// 	So just verify current position is up to date with server position
			transform.position = Vector3.SmoothDamp(transform.position, serverPosition, ref serverPositionSmoothVelocity, 0.1f);
		}
	}

	public override void FixedUpdate() {
		if (isInfected && hasAuthority || (!isInfected && isServer && physicsEntity != null)) {
			physicsEntity.Update();
			CmdUpdatePosition(transform.position);
		}
	}

    public override void ImportStats()
    {
		height = .5f;
		width = .5f;
		movementSpeed = .06f;
		type = "NPC";
		isInfected = false;
    }

    protected override void HandleInput()
    {
		// This function is only called when this NPC is infected,
		// 	and is only called on the Parasite player's client

		// Movement
		// TODO: add possibility for being moved outside of input and reduce duplication
		bool right = Input.GetKey(KeyCode.D);
		bool left = Input.GetKey(KeyCode.A);
		if (right && !left) {
			physicsEntity.velocityX = movementSpeed;
		} else if (left && !right) {
			physicsEntity.velocityX = -movementSpeed;
		} else {
			physicsEntity.velocityX = 0;
		}

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
		// TODO: replace with var
		playerObject.CmdSpawnPlayerCharacter("PARASITE", transform.position, new Vector2(0, .75f));
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
		} else {
			// On all other clients, un-verify
			GetComponentInChildren<SpriteRenderer>().color = Color.white;
		}
	}

	[ClientRpc]
	public void RpcVerify() {
		bool isParasitePlayer = FindObjectOfType<ClientInformation>().clientType == "PARASITE";
		if (isParasitePlayer) { return;}
		if (isInfected) {
			// Turn Magenta
			GetComponentInChildren<SpriteRenderer>().color = Color.magenta;
		} else {
			// Turn Green
			GetComponentInChildren<SpriteRenderer>().color = Color.cyan;
		}
	}
}
