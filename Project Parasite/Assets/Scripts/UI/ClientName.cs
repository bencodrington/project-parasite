using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class ClientName : MonoBehaviour {
	void Start () {
		if (PhotonNetwork.NickName != null) {
			GetComponent<Text>().text = "Name: " + PhotonNetwork.NickName;
		}
	}
}
