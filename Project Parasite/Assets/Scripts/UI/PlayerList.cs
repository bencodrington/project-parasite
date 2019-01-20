using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerList : MonoBehaviour {

	List<string> playerNames;
	string oldPlayerNames;

	public GameObject PlayerListEntryPrefab;

	List<GameObject> children;

	void Start() {
		oldPlayerNames = "";
	}
	
	void Update() {
		// TODO: this almost certainly doesn't need to run every frame
		if (PlayerGrid.Instance == null) { return; }
		playerNames = PlayerGrid.Instance.GetPlayerNames();
		if (ListHasBeenModified()) {
			UpdateChildren();
			oldPlayerNames = SerializeList(playerNames);
		}
	}

	void UpdateChildren() {
		foreach(Transform child in transform) {
			Destroy(child.gameObject);
		}
		GameObject playerListEntry;
		for(int i = 0; i < playerNames.Count; i++) {
			playerListEntry = Instantiate(PlayerListEntryPrefab);
			// NOTE: the false on the next line is important for not messing with scaling
			// 	and is necessary because of the Canvas Scaler component
			playerListEntry.transform.SetParent(transform, false);
			playerListEntry.transform.position = (Vector2)transform.position + new Vector2(0, (i+1) * -30);
			playerListEntry.GetComponent<Text>().text = playerNames[i];
		}
	}

	bool ListHasBeenModified() {
		return SerializeList(playerNames) != oldPlayerNames;
	}

	string SerializeList(List<string> stringList) {
		string stringVersion = "";
		foreach (string entry in stringList) {
			stringVersion += entry;
		}
		return stringVersion;
	}
}
