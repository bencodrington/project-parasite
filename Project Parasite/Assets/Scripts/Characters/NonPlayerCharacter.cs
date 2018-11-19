using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class NonPlayerCharacter : Character {

	public bool isInfected = false;

	private const float PARASITE_LAUNCH_VELOCITY = 0.75f;

	// Pathfinding
	private float validDistanceFromTarget = .5f;
	private Vector3 target;
	private float minTimeUntilNewPath = 2f;
	private float maxTimeUntilNewPath = 5f;
	private bool hasTarget = false;
	private float maxTargetDistance = 5f;
	private float minTargetDistance = 2f;

	// TODO: update to use fixedupdate for physics
	public override void Update() {
		if (isInfected && hasAuthority) {
			// NPC is infected and this client is the Parasite player's client
			HandleInput();
		} else if (!isInfected && isServer && physicsEntity != null) {
			// NPC still belongs to the server
			TraversePath();
		} else {
			// This is a cloned representation of the authoritative NPC
			// 	So just verify current position is up to date with server position
			transform.position = Vector3.Lerp(transform.position, serverPosition, 0.8f);
		}
	}

	public override void FixedUpdate() {
		// If on owner's client
		if (isInfected && hasAuthority || (!isInfected && isServer && physicsEntity != null)) {
			// Run physics update and notify server
			physicsEntity.Update();
			CmdUpdatePosition(transform.position, false);
		}
	}

    protected override void HandleInput()
    {
		// This function is only called when this NPC is infected,
		// 	and is only called on the Parasite player's client
		// Movement
		HandleHorizontalMovement();
		// Self Destruct
		if (Input.GetMouseButtonDown(0)) {
			// Destroy this NPC
			CmdDespawnSelf();
		}
    }

	void TraversePath() {
		if (!hasTarget) { return; }
		if (Vector3.Distance(this.transform.position, target) < validDistanceFromTarget) {
			// Reached target
			StartCoroutine(Idle());
			// Stop traversing path
			physicsEntity.velocityX = 0;
			hasTarget = false;
		} else {
			// Still moving
			if (target.x >= transform.position.x) {
				physicsEntity.velocityX = stats.movementSpeed;
			} else {
				physicsEntity.velocityX = -stats.movementSpeed;
			}
		}
	}

	void FindNewPath() {
		// TODO: While path target is not reachable
		// Randomly select offset that is +/-[minTargetDistance, maxTargetDistance]
		float rangeDifference = maxTargetDistance - minTargetDistance;
		float offset = Random.Range(-rangeDifference, rangeDifference);
		offset += (offset >= 0) ? minTargetDistance : -minTargetDistance;
		// Set target relative to current location
		target = new Vector3(transform.position.x + offset, transform.position.y, 0);
		// Begin traversing
		hasTarget = true;
	}

	public IEnumerator Idle() {
		yield return new WaitForSeconds(Random.Range(minTimeUntilNewPath, maxTimeUntilNewPath));
		// Check that we are still uninfected and still exist
		if (!isInfected && transform != null) { FindNewPath(); }
		
	}

	// COMMANDS

	[Command]
	void CmdDespawnSelf() {
		// Spawn new Parasite Object
		playerObject.CmdSpawnPlayerCharacter(CharacterType.Parasite, transform.position, new Vector2(0, PARASITE_LAUNCH_VELOCITY));
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
		// TODO: update PlayerGrid
		isInfected = true;
		if (hasAuthority) {
			// Only update sprite if on the Parasite player's client
			spriteRenderer.color = Color.magenta;
		}
	}
}
