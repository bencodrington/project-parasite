using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Parasite : Character {

	private float jumpVelocity = 30f;
	// Whether the "UP" key was being pressed last frame
	private bool oldUp = false;

	protected override void HandleInput()  {
		// Movement
		bool right = Input.GetKey(KeyCode.D);
		bool left = Input.GetKey(KeyCode.A);
		isMovingLeft = false;
		isMovingRight = false;
		if (right && !left) {
			physicsEntity.applyGravity = !physicsEntity.IsOnRightWall();
			isMovingRight = true;
		} else if (left && !right) {
			physicsEntity.applyGravity = !physicsEntity.IsOnLeftWall();
			isMovingLeft = true;
		} else {
			physicsEntity.applyGravity = true;
		}

		bool isOvercomingGravity = false;
		bool up = Input.GetKey(KeyCode.W);
		bool down = Input.GetKey(KeyCode.S);
		isMovingUp = false;
		isMovingDown = false;
		if (up && !oldUp && physicsEntity.IsOnGround()) {
			// Jump
			physicsEntity.AddVelocity(0, jumpVelocity);
		} else if (up && physicsEntity.IsOnCeiling()) {
			// Stick to ceiling
			isOvercomingGravity = true;
		} else if (up && physicsEntity.IsOnWall()) {
			// Climb Up
			isMovingUp = true;
		} else if (down && physicsEntity.IsOnWall()) {
			// Climb Down
			isMovingDown = true;
		}
		oldUp = up;
		physicsEntity.SetIsOvercomingGravity(isOvercomingGravity);

		// Infect
		if (Input.GetMouseButtonDown(0)) {
			Collider2D npc = Physics2D.OverlapPoint(Camera.main.ScreenToWorldPoint(Input.mousePosition), Utility.GetLayerMask(CharacterType.NPC));
			if (npc != null) {
				CmdInfectNpc(npc.transform.parent.GetComponent<NetworkIdentity>().netId);
				CmdDestroyParasite();
			}
		}
	}

	public void OnTakingDamage() {
		StartCoroutine(FlashColours());
	}

	IEnumerator FlashColours() {
		// How long to flash for
		float timeRemaining = 0.5f;
		Color currentColour = spriteRenderer.color;
		// Used for cycling colours
		Dictionary<Color, Color> nextColour = new Dictionary<Color, Color>();
		nextColour.Add(Color.red, Color.cyan);
		nextColour.Add(Color.cyan, Color.yellow);
		nextColour.Add(Color.yellow, Color.red);
		while (timeRemaining > 0) {
			timeRemaining -= Time.deltaTime;
			// Switch to next colour
			nextColour.TryGetValue(currentColour, out currentColour);
			// Update spriterenderer
			spriteRenderer.color = currentColour;
			yield return null;
		}
		// Return to default colour
		spriteRenderer.color = Color.red;

	}

	// Commands

	[Command]
	void CmdDestroyParasite() {
		NetworkServer.Destroy(gameObject);
	}

	[Command]
	void CmdInfectNpc(NetworkInstanceId npcNetId) {
		// Find NPC GameObject with matching NetId
		GameObject npcGameObject = NetworkServer.FindLocalObject(npcNetId);
		// Get NonPlayerCharacter script and NetworkIdentity component off of it
		NonPlayerCharacter npc = npcGameObject.GetComponentInChildren<NonPlayerCharacter>();
		NetworkIdentity networkIdentity = npcGameObject.GetComponentInChildren<NetworkIdentity>();
		// Let it know it will be player controlled from now on
		networkIdentity.localPlayerAuthority = true;
		npc.RpcSetLocalPlayerAuthority(true);
		// Store playerObject for eventual transfer back to parasite
		npc.PlayerObject = PlayerObject;
		// Give Parasite player authority over the NPC
		networkIdentity.AssignClientAuthority(PlayerObject.connectionToClient);
		// Delete current physics entity off the server for performance
		npc.CmdDeletePhysicsEntity();
		// TODO: transfer velocity from current physics entity?
		npc.RpcGeneratePhysicsEntity(Vector2.zero);
		// Set isInfected to true/update sprite on new authority's client
		npc.RpcInfect();
		// Update client's camera and render settings to reflect new character
		npc.RpcSetCameraFollow();
		npc.RpcSetRenderLayer();
		PlayerGrid.Instance.CmdSetCharacter(PlayerObject.netId, npc.netId);
	}
}
