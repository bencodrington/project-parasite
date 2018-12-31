using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkButton : MonoBehaviour {

	public enum NetworkEntityType {
		Host,
		Client
	}

	public NetworkEntityType networkEntityType;

	public void OnClick() {
		MatchManager matchManager = FindObjectOfType<MatchManager>();
		if (matchManager == null) {
			Debug.LogError("NetworkButton: OnClick(): MatchManager not found. NetworkEntityType: " + networkEntityType);
			return;
		}
		switch (networkEntityType) {
			case NetworkEntityType.Host: matchManager.CreateRoom(); break;
			case NetworkEntityType.Client: matchManager.ListMatches(); break;
		}
	}
}
