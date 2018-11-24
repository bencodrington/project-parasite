using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ClientName : MonoBehaviour {
	void Start () {
		if (PlayerGrid.Instance.localPlayerName != null) {
			GetComponent<Text>().text = "Name: " + PlayerGrid.Instance.localPlayerName;
		}
	}
}
