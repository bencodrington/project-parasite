using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OrbUiManager : MonoBehaviour {

	public GameObject orbSpritePrefab;
	GameObject[] orbSprites;
	// The sprites that show even when the orb sprites are hidden
	public GameObject orbSpritePlaceholderPrefab;
	// Only need to store images, not entire gameobject, as we will only
	// 	modify the colour, never the actual gameobject
	Image[] orbSpritePlaceholders;
	Color placeholderDefaultColour = new Color(0, 1, 1, 0.25f);

	const float DISTANCE_BETWEEN_ORB_SPRITES = 10;
	const float ORB_SPRITE_WIDTH = 50;
	// Space to the edge of the canvas
	Vector2 CANVAS_PADDING = new Vector2(-420, 20);

	int maxOrbCount;
	int numOrbSpritesEnabled;

	void Start() {
		GetComponent<RectTransform>().anchoredPosition = CANVAS_PADDING;
	}

	public void setMaxOrbCount(int maxOrbCount) {
		this.maxOrbCount = maxOrbCount;
		orbSprites = new GameObject[maxOrbCount];
		orbSpritePlaceholders = new Image[maxOrbCount];
		SpawnOrbSprites();
	}

	void SpawnOrbSprites() {
		for(int i = 0; i < maxOrbCount; i++) {
			// NOTE: the placeholder must be spawn first to be drawn below the sprite itself
			orbSpritePlaceholders[i] = SpawnOrbSpritePlaceholder(i);
			orbSprites[i] = SpawnOrbSprite(i);
		}
		numOrbSpritesEnabled = maxOrbCount;
		UpdateOrbSpriteCount(0);
	}

	GameObject SpawnOrbSprite(int index) {
		GameObject orbSprite = Instantiate(orbSpritePrefab);
		orbSprite.transform.SetParent(transform);
		orbSprite.GetComponent<RectTransform>().anchoredPosition = getNewOrbSpritePosition(index);
		return orbSprite;
	}

	Image SpawnOrbSpritePlaceholder(int index) {
		Image orbSpritePlaceholder = Instantiate(orbSpritePlaceholderPrefab).GetComponent<Image>();
		orbSpritePlaceholder.transform.SetParent(transform);
		orbSpritePlaceholder.GetComponent<RectTransform>().anchoredPosition = getNewOrbSpritePosition(index);
		orbSpritePlaceholder.color = placeholderDefaultColour;
		return orbSpritePlaceholder;
	}

	public void OnOrbCountChange(int numberOfOrbsPlaced) {
		UpdateOrbSpriteCount(numberOfOrbsPlaced);
	}

	void UpdateOrbSpriteCount(int numberOfOrbsPlaced) {
		int numOrbsRemaining = maxOrbCount - numberOfOrbsPlaced;
		while (numOrbSpritesEnabled < numOrbsRemaining) {
			// Enable sprite
			EnableOrbSprite();
			// If we added any sprites, we can return early
			return;
		}
		Debug.Log("Enabled: " + numOrbSpritesEnabled + ", should be: " + numOrbsRemaining);
		while (numOrbSpritesEnabled > numOrbsRemaining) {
			// Disable sprite
			DisableOrbSprite();
		}
	}
	
	void EnableOrbSprite() {
		orbSprites[numOrbSpritesEnabled].SetActive(true);
		numOrbSpritesEnabled++;
	}

	void DisableOrbSprite() {
		orbSprites[numOrbSpritesEnabled - 1].SetActive(false);
		numOrbSpritesEnabled--;
	}

	Vector2 getNewOrbSpritePosition(int index) {
		// index should be in the range [0..maxOrbCount - 1]
		float x = (-(DISTANCE_BETWEEN_ORB_SPRITES + ORB_SPRITE_WIDTH) * index) - ORB_SPRITE_WIDTH;
		return new Vector2(x, 0);
	}
}
