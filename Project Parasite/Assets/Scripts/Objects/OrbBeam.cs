using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class OrbBeam : NetworkBehaviour {

	float energyRadius = 2f;
	float energyForce = 10f;

	Vector2 startPoint;
	Vector2 endPoint;

	Vector2 normal;
	Ray2D hitboxRay;
	Vector2 hitboxSize;
	float hitboxAngle;

	SpriteRenderer spriteRenderer;

	void Initialize(Vector2 startPoint, Vector2 endPoint) {
		this.startPoint = startPoint;
		this.endPoint = endPoint;
		normal = Vector2.Perpendicular(endPoint - startPoint).normalized;
		hitboxSize = new Vector2( Vector2.Distance(startPoint, endPoint), energyRadius);
		hitboxAngle = Vector2.SignedAngle(Vector2.up, normal);
		hitboxRay = new Ray2D(startPoint, endPoint - startPoint);
		// Stretch sprite between connecting orbs
		spriteRenderer = GetComponentInChildren<SpriteRenderer>();
		Vector3 spriteScale = new Vector3(
			spriteRenderer.transform.localScale.x * hitboxSize.x,
			spriteRenderer.transform.localScale.y,
			spriteRenderer.transform.localScale.z
		);
		spriteRenderer.transform.localScale = spriteScale;
		spriteRenderer.transform.Rotate(new Vector3(0, 0, hitboxAngle));
	}

	void FixedUpdate() {
		Repel();
		if (isServer) {
			// See if this beam is overlapping any parasites
			Parasite[] parasites = Array.ConvertAll<Character, Parasite>(
				CheckOverlapping(CharacterType.Parasite),
				delegate (Character character) {
					return (Parasite)character;
				}
			);
			if (parasites.Length > 0) {
				// If so, turn red and notify the hunter that placed it
				RpcOnOverlapParasite();
				// Fry parasites
				Fry(parasites);
			}
			NonPlayerCharacter[] npcs = Array.ConvertAll<Character, NonPlayerCharacter>(
				CheckOverlapping(CharacterType.NPC),
				delegate (Character character) {
					return (NonPlayerCharacter)character;
				}
			);
			Fry(npcs);
		}
	}

	void Repel() {
		Vector2 projectionOntoOrbBeam, hunterPosition, forceDirection;
		float distanceToHunter;
		// Get the colliders of all hunters within range of the line
		Collider2D[] energyCenterColliders = Physics2D.OverlapBoxAll(transform.position, hitboxSize, hitboxAngle, Utility.GetLayerMask("energyCenter"));
		foreach (Collider2D energyCenterCollider in energyCenterColliders) {
			Hunter hunter = energyCenterCollider.transform.parent.GetComponent<Hunter>();
			if ((Character)hunter == PlayerGrid.Instance.GetLocalCharacter()) {
				hunterPosition = energyCenterCollider.transform.position;
				// Find the point on the orb beam line that is nearest to the hunter
				projectionOntoOrbBeam = Utility.ProjectOntoRay2D(hunterPosition, hitboxRay);
				distanceToHunter = Vector2.Distance(projectionOntoOrbBeam, hunterPosition);
				forceDirection = CalculateForceDirection(hunterPosition, projectionOntoOrbBeam);
				// Default to launching hunters up
				forceDirection = forceDirection == Vector2.zero ? Vector2.up : forceDirection;
				// Repel hunter away from projected point with a force that is greater if the hunter is
				//	close to the orb beam
				hunter.Repel(forceDirection, CalculateForce(distanceToHunter));
			}
		}
	}

	Character[] CheckOverlapping(CharacterType type) {
		RaycastHit2D[] hits = Physics2D.LinecastAll(startPoint, endPoint, Utility.GetLayerMask(type));
		return Array.ConvertAll<RaycastHit2D, Character>(
			hits,
			delegate (RaycastHit2D hit) {
				return hit.transform.parent.GetComponent<Character>();
			}
		);
	}

	void Fry(Character[] characters) {
		// TODO:
	}

	// void Fry() {
	// 	// Get the colliders of all NPCs that fall on the line
	// 	RaycastHit2D[] hits = Physics2D.LinecastAll(startPoint, endPoint, Utility.GetLayerMask(CharacterType.NPC));
	// 	NonPlayerCharacter npc;
	// 	Parasite parasite;
	// 	// Fry each uninfected NPC on the server
	// 	foreach (RaycastHit2D hit in hits) {
	// 		npc = hit.transform.parent.GetComponent<NonPlayerCharacter>();
	// 		if (isServer && !npc.isInfected) {
	// 			FindObjectOfType<NpcManager>().DespawnNpc(npc.netId);
	// 		} else if (npc.isInfected && PlayerGrid.Instance.GetLocalCharacter() == npc) {
	// 			npc.CmdDespawnSelf();
	// 		}
	// 	}
	// }

	Vector2 CalculateForceDirection(Vector2 hunterPosition, Vector2 projectedPosition) {
		return hunterPosition - projectedPosition;
	}

	float CalculateForce(float distance) {
		// The maximum distance from the beam that the force recipient will receive full force
		// 	after this point, the force starts to fall off
		float fullForceCutoff = energyRadius / 2;
		if (distance < fullForceCutoff) {
			return energyForce;
		}
		// POINT LABELS: projectedPosition  fullForceCutoff   energyRadius
		//              	  |----------------|----------------|
		// FORCE OUTPUT:	   ^--FULL FORCE--^			       0
		float t = (distance - fullForceCutoff) / (energyRadius - fullForceCutoff);
		return Mathf.Lerp(energyForce, 0, t);
	}

	// ClientRpc

	[ClientRpc]
	public void RpcInitialize(Vector2 startPoint, Vector2 endPoint) {
		Initialize(startPoint, endPoint);
	}

	[ClientRpc]
	void RpcOnOverlapParasite() {
		// TODO:
		Debug.Log("OVERLAPPING PARASITE");
	}
}
