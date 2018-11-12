using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkButton : MonoBehaviour {

	public GameObject networkIdentityPrefab;

	public void OnClick() {
		Instantiate(networkIdentityPrefab);
	}
}
