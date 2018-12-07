using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrbUiManager : MonoBehaviour {

	public GameObject orbSpritePrefab;
	Stack<GameObject> orbSprites;

	int maxOrbCount;
	public void setMaxOrbCount(int maxOrbCount) {
		this.maxOrbCount = maxOrbCount;
		orbSprites = new Stack<GameObject>();
		UpdateOrbSpriteCount(maxOrbCount);
	}
	public void OnOrbCountChange(int newCount) {
		UpdateOrbSpriteCount(newCount);
	}

	void UpdateOrbSpriteCount(int newCount) {
		// TODO: optimize by disabling/enabling instead of spawning & destroying
		int orbsRemaining = maxOrbCount - newCount;
		GameObject orbSprite;
		while (orbSprites.Count < orbsRemaining) {
			// Add orb sprite
			orbSprite = Instantiate(orbSpritePrefab);
			orbSprite.transform.SetParent(this.transform);
			orbSprite.GetComponent<RectTransform>().anchoredPosition = getNewOrbSpritePosition();
			orbSprites.Push(orbSprite);
		}
		while (orbSprites.Count > orbsRemaining) {
			// Remove orb sprite
			Destroy(orbSprites.Pop());
		}
	}

	Vector2 getNewOrbSpritePosition() {
		// Note: at this point, orbSprites.Count does not include the new orbSprite
		// 	therefore, it will be in the range [0..maxOrbCount - 1]
		float x = (-60 * orbSprites.Count) - 45;
		return new Vector2(x, 0);
	}
}
