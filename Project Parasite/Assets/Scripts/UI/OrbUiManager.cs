using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrbUiManager : MonoBehaviour {

	public GameObject orbSpritePrefab;
	Stack<GameObject> orbSprites;

	const float DISTANCE_BETWEEN_ORB_SPRITES = 10;
	const float ORB_SPRITE_WIDTH = 50;
	// Space to the edge of the canvas
	Vector2 CANVAS_PADDING = new Vector2(-420, 20);

	int maxOrbCount;

	void Start() {
		GetComponent<RectTransform>().anchoredPosition = CANVAS_PADDING;
	}

	public void setMaxOrbCount(int maxOrbCount) {
		this.maxOrbCount = maxOrbCount;
		orbSprites = new Stack<GameObject>();
		UpdateOrbSpriteCount(0);
	}
	public void OnOrbCountChange(int numberOfOrbsPlaced) {
		UpdateOrbSpriteCount(numberOfOrbsPlaced);
	}

	void UpdateOrbSpriteCount(int numberOfOrbsPlaced) {
		// TODO: optimize by disabling/enabling instead of spawning & destroying
		int orbsRemaining = maxOrbCount - numberOfOrbsPlaced;
		GameObject orbSprite;
		while (orbSprites.Count < orbsRemaining) {
			// Add orb sprite
			orbSprite = Instantiate(orbSpritePrefab);
			orbSprite.transform.SetParent(transform);
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
		float x = (-(DISTANCE_BETWEEN_ORB_SPRITES + ORB_SPRITE_WIDTH) * orbSprites.Count) - ORB_SPRITE_WIDTH;
		return new Vector2(x, 0);
	}
}
