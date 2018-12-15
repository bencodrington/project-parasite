using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Parasite : Character {

	private float jumpVelocity = 30f;
	private const float MAX_POUNCE_VELOCITY = 45f;
	// Whether the directional keys were being pressed last frame
	private bool oldUp = false;
	private bool oldRight = false;
	private bool oldLeft = false;
	// Whether the action1 key was being pressed last frame
	private bool oldAction1 = false;

	// How many seconds the parasite has been charging to pounce
	private float timeSpentCharging = 0f;
	// The pounce speed will be capped off after this many seconds
	private const float MAX_CHARGE_TIME = 2f;
	// The angle at which the parasite is currently set to pounce, UP by default
	private float pounceAngle = 90f;
	// The number of degrees by which to modify the angle each key press
	private const float POUNCE_ANGLE_INCREMENT = 15f;
	private bool IsChargingPounce() {
		return timeSpentCharging > 0f;
	}

	protected override void HandleInput()  {
		// Movement
		bool right = Input.GetKey(KeyCode.D);
		bool left = Input.GetKey(KeyCode.A);
		isMovingLeft = false;
		isMovingRight = false;
		if (IsChargingPounce()) {
			if (right && !oldRight) {
				// Tilt angle to the right
				pounceAngle -= POUNCE_ANGLE_INCREMENT;
			} else if (left && !oldLeft) {
				// Tilt angle to the left
				pounceAngle += POUNCE_ANGLE_INCREMENT;
			}
		} else {
			if (right && !left) {
				physicsEntity.applyGravity = !physicsEntity.IsOnRightWall();
				isMovingRight = true;
			} else if (left && !right) {
				physicsEntity.applyGravity = !physicsEntity.IsOnLeftWall();
				isMovingLeft = true;
			} else {
				physicsEntity.applyGravity = true;
			}
		}
		oldRight = right;
		oldLeft = left;

		bool isStuckToCeiling = false;
		bool up = Input.GetKey(KeyCode.W);
		bool down = Input.GetKey(KeyCode.S);
		isMovingUp = false;
		isMovingDown = false;
		if (up && !oldUp && physicsEntity.IsOnGround()) {
			// Jump
			physicsEntity.AddVelocity(0, jumpVelocity);
		} else if (up && physicsEntity.IsOnCeiling()) {
			// Stick to ceiling
			isStuckToCeiling = true;
		} else if (up && physicsEntity.IsOnWall()) {
			// Climb Up
			isMovingUp = true;
		} else if (down && physicsEntity.IsOnWall()) {
			// Climb Down
			isMovingDown = true;
		}
		oldUp = up;
		physicsEntity.SetIsStuckToCeiling(isStuckToCeiling);

		bool action1 = Input.GetKey(KeyCode.J);
		if (action1) {
			// Action key is down
			// Charge leap
			timeSpentCharging += Time.deltaTime;
		} else if (oldAction1 && !action1 && physicsEntity.IsOnGround()) {
			// On action button release
			// Pounce
			physicsEntity.AddVelocity(CalculatePounceVelocity());
		}
		if (!action1) {
			ResetPounceVariables();
		}
		oldAction1 = action1;

		// Infect
		if (Input.GetMouseButtonDown(0)) {
			Collider2D npc = Physics2D.OverlapPoint(Camera.main.ScreenToWorldPoint(Input.mousePosition), Utility.GetLayerMask(CharacterType.NPC));
			if (npc != null) {
				CmdInfectNpc(npc.transform.parent.GetComponent<NetworkIdentity>().netId);
				CmdDestroyParasite();
			}
		}
	}

	Vector2 CalculatePounceVelocity() {
		float speed = Mathf.Lerp(0, MAX_POUNCE_VELOCITY, timeSpentCharging / MAX_CHARGE_TIME);
		Debug.Log(speed);
		float pounceAngleRads = Mathf.Deg2Rad * pounceAngle;
		Vector2 velocity = new Vector2(Mathf.Cos(pounceAngleRads), Mathf.Sin(pounceAngleRads));
		Debug.Log(velocity);
		velocity *= speed;
		return velocity;
	}

	void ResetPounceVariables() {
		timeSpentCharging = 0f;
		pounceAngle = 90f;
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
