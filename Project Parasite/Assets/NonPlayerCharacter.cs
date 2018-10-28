using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class NonPlayerCharacter : NetworkBehaviour {

	// COMMANDS
	// CLIENTRPC

	[ClientRpc]
	public void RpcSetColour() {
		GetComponentInChildren<SpriteRenderer>().color = Color.yellow;
	}
}
