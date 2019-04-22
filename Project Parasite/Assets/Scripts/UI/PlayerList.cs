using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

public class PlayerList : MonoBehaviour {

	List<string> playerNames;
	string oldPlayerNames;

	public GameObject PlayerListEntryPrefab;

	List<GameObject> children;

	#region [MonoBehaviour Callbacks]

	void Start() {
		oldPlayerNames = "";
	}
	
	void Update() {
		// OPTIMIZE: this almost certainly doesn't need to run every frame
		// OPTIMIZE: run Onplayerconnect and Onplayerdisconnect
		playerNames = GetPlayerNames();
		// OPTIMIZE: then this can be removed
		if (ListHasBeenModified()) {
			UpdateChildren();
			oldPlayerNames = SerializeList(playerNames);
		}
	}

	#endregion

	#region [Private Methods]

	List<string> GetPlayerNames() {
		List<string> playerNamesList = new List<string>();
		Dictionary<int, CharacterType> characterSelections = MatchManager.Instance.characterSelectionManager.GetCharacterSelections();
		
		foreach (Player player in PhotonNetwork.PlayerList) {
			playerNamesList.Add(GetPlayerName(player, characterSelections));
		}
		return playerNamesList;
	}

	string GetPlayerName(Player player, Dictionary<int, CharacterType> characterSelections) {
		if (characterSelections == null || !characterSelections.ContainsKey(player.ActorNumber)) {
			// Currently on random character assignment mode,
			// 	or the player doesn't currently have a character selected
			// 	just show the name
			return player.NickName;
		}
		// Append selected character type to player's name
		return player.NickName + " - " + characterSelections[player.ActorNumber].ToString();
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

	#endregion
}