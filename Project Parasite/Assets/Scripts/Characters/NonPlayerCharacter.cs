using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

// TODO: rename playercharacter?
public class NonPlayerCharacter : PlayerCharacter {

	public bool isInfected;

	private float validDistanceFromTarget = .5f;
	private Vector3 target;
	private float minTimeUntilNewPath = 2f;
	private float maxTimeUntilNewPath = 5f;
	private bool hasTarget = false;
	private float maxTargetDistance = 5f;
	private float minTargetDistance = 2f;
	private const float PARASITE_LAUNCH_VELOCITY = 0.75f;

	// TODO: update to use fixedupdate for physics
	public override void Update() {
		if (isInfected && hasAuthority) {
			// NPC is infected and this client is the Parasite player's client
			HandleInput();
		} else if (!isInfected && isServer && physicsEntity != null) {
			TraversePath();
		} else {
			// This is a cloned representation of the authoritative NPC
			// 	So just verify current position is up to date with server position
			transform.position = Vector3.Lerp(transform.position, serverPosition, 0.8f);
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

	void TraversePath() {
		if (!hasTarget) { return; }
		if (Vector3.Distance(this.transform.position, target) < validDistanceFromTarget) {
			// Reached target
			StartCoroutine(Idle());
			physicsEntity.velocityX = 0;
			hasTarget = false;
		} else {
			// Still moving
			if (target.x >= transform.position.x) {
				physicsEntity.velocityX = movementSpeed;
			} else {
				physicsEntity.velocityX = -movementSpeed;
			}
		}
	}

	void FindNewPath() {
		// TODO: While path target is not reachable
		// Choose a path
		float rangeDifference = maxTargetDistance - minTargetDistance;
		float offset = Random.Range(-rangeDifference, rangeDifference);
		offset += (offset >= 0) ? minTargetDistance : -minTargetDistance;
		target = new Vector3(transform.position.x + offset, transform.position.y, 0);
		hasTarget = true;
	}

	public IEnumerator Idle() {
		yield return new WaitForSeconds(Random.Range(minTimeUntilNewPath, maxTimeUntilNewPath));
		// Check that we are still uninfected
		if (!isInfected) { FindNewPath(); }
		
	}

	// COMMANDS

	[Command]
	void CmdDespawnSelf() {
		// Spawn new Parasite Object
		playerObject.CmdSpawnPlayerCharacter("PARASITE", transform.position, new Vector2(0, PARASITE_LAUNCH_VELOCITY));
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
