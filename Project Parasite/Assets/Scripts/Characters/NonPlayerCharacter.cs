using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class NonPlayerCharacter : Character {

	public bool isInfected = false;

	private const float PARASITE_LAUNCH_VELOCITY = 30f;

	// Pathfinding
	private float validDistanceFromTarget = .5f;
	private Vector3 target;
	private float minTimeUntilNewPath = 2f;
	private float maxTimeUntilNewPath = 5f;
	private bool hasTarget = false;
	private float maxTargetDistance = 5f;
	private float minTargetDistance = 2f;
	
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

    protected override void HandleInput()
    {
		// This function is only called when this NPC is infected,
		// 	and is only called on the Parasite player's client
		// Movement
		HandleHorizontalMovement();
		// Self Destruct
		if (Input.GetMouseButtonDown(1)) {
			// Destroy this NPC
			CmdDespawnSelf();
		}
    }

	void TraversePath() {
		isMovingLeft = false;
		isMovingRight = false;
		if (!hasTarget) { return; }
		if (Vector3.Distance(this.transform.position, target) < validDistanceFromTarget) {
			// Reached target
			StartCoroutine(Idle());
			// Stop traversing path
			hasTarget = false;
		} else {
			// Still moving
			if (target.x >= transform.position.x) {
				isMovingRight = true;
			} else {
				isMovingLeft = true;
			}
		}
	}

	void FindNewPath() {
		// Randomly select offset that is +/-[minTargetDistance, maxTargetDistance]
		float rangeDifference = maxTargetDistance - minTargetDistance;
		float offset = Random.Range(-rangeDifference, rangeDifference);
		offset += (offset >= 0) ? minTargetDistance : -minTargetDistance;
		// Set target relative to current location
		target = new Vector3(transform.position.x + offset, transform.position.y, 0);
		// If there is a wall/ beam in the way, don't move to new target
		target = ModifyTargetToAvoidObstacles(target);
		// Begin traversing
		hasTarget = true;
	}

	Vector2 ModifyTargetToAvoidObstacles(Vector2 target) {
		Vector2 pathHitboxSize = new Vector2(target.x - transform.position.x, spriteRenderer.transform.localScale.y);
		// TODO: the below can cause npcs to walk into beams at head height,
		//  but also stops the hitbox from being triggered by the floor
		pathHitboxSize.y -= 0.1f;
		// Calculate corners of hitbox
		Vector2 pathHitboxTopStart = new Vector2(transform.position.x, transform.position.y + pathHitboxSize.y / 2);
		Vector2 pathHitboxBottomEnd = new Vector2(target.x, target.y - pathHitboxSize.y / 2);
		bool isObstacleInTheWay = Physics2D.OverlapArea(pathHitboxTopStart, pathHitboxBottomEnd, Utility.GetLayerMask("npcPathObstacle"));
		if (isObstacleInTheWay) {
			target = transform.position;
		}
		return target;
	}

	public IEnumerator Idle() {
		yield return new WaitForSeconds(Random.Range(minTimeUntilNewPath, maxTimeUntilNewPath));
		// Check that we are still uninfected and still exist
		if (this != null && !isInfected) { FindNewPath(); }
		
	}

	// Commands

	[Command]
	public void CmdDespawnSelf() {
		// Spawn new Parasite Object
		PlayerObject.CmdSpawnPlayerCharacter(CharacterType.Parasite, transform.position, new Vector2(0, PARASITE_LAUNCH_VELOCITY));
		// Despawn this NPC object
		FindObjectOfType<NpcManager>().DespawnNpc(netId);
	}

	// ClientRpc

	[ClientRpc]
	public void RpcSetLocalPlayerAuthority(bool newValue) {
		GetComponentInChildren<NetworkIdentity>().localPlayerAuthority = newValue;
	}

	[ClientRpc]
	public void RpcInfect() {
		isInfected = true;
		if (hasAuthority) {
			// Only update sprite if on the Parasite player's client
			spriteRenderer.color = Color.magenta;
		}
	}
}
