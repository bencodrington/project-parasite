using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StartButton : MonoBehaviour {

	public void StartGame() {
		// TODO: clean up
		RestartGame();
	}

	public void RestartGame() {
		PlayerGrid.Instance.GetLocalPlayerObject().CmdStartGame();
	}
}
