using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ClientName : MonoBehaviour {
	void Start () {
		ClientInformation cI = FindObjectOfType<ClientInformation>();
		if (cI == null) {
			Debug.Log("ClientName:Start: cI is null");
			return;
		}
		if (cI.clientName != null) {
			GetComponent<Text>().text = "Name: " + cI.clientName;
		}
	}
}
