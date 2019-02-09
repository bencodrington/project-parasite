using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkButton : MonoBehaviour {
	public void OnClick() {
		MatchManager matchManager = FindObjectOfType<MatchManager>();
		if (matchManager == null) {
			Debug.LogError("NetworkButton: OnClick(): MatchManager not found.");
			return;
		}
		Debug.Log("NetworkButton:OnClick()");
		matchManager.Connect();
	}
}
