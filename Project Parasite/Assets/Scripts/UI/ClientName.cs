using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class ClientName : MonoBehaviour {
	void OnEnable () {
		if (PhotonNetwork.LocalPlayer.NickName != null) {
			GetComponent<Text>().text = "Name: " + PhotonNetwork.LocalPlayer.NickName;
		}
	}
}
