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
	Color PLACEHOLDER_DEFAULT_COLOUR = new Color(0, 1, 1, 0.25f);
	Color PLACEHOLDER_FLASH_COLOUR = Color.red;

	const float DISTANCE_BETWEEN_ORB_SPRITES = 10;
	const float ORB_SPRITE_WIDTH = 100;
	const float ORB_SPRITE_HEIGHT = 100;
	// Space to the edge of the canvas
	Vector2 CANVAS_PADDING = new Vector2(-10, 10);

	int maxOrbCount;
	int numOrbSpritesEnabled;

	// Used when the placeholder sprites flash
	// 	- triggered when the hunter tries to place an orb but has none left
	Coroutine flashing;
	// How long each colour lasts in the flashing animation
	const float FLASH_LENGTH = .1f;
	// How many times the flashing animation repeats
	const int NUM_FLASHES = 4;

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
		orbSpritePlaceholder.color = PLACEHOLDER_DEFAULT_COLOUR;
		return orbSpritePlaceholder;
	}

	void UpdateOrbSpriteCount(int numberOfOrbsPlaced) {
		int numOrbsRemaining = maxOrbCount - numberOfOrbsPlaced;
		if (flashing != null) {
			// Must have recalled an orb while flashing placeholders
			StopCoroutine(flashing);
		}
		while (numOrbSpritesEnabled < numOrbsRemaining) {
			// Enable sprite
			EnableOrbSprite();
			// If we added any sprites, we can return early
			return;
		}
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
		float x = (-(DISTANCE_BETWEEN_ORB_SPRITES + ORB_SPRITE_WIDTH) * index) - ORB_SPRITE_WIDTH / 2;
		return new Vector2(x, ORB_SPRITE_HEIGHT / 2);
	}
	
	#region [Public Methods]

	public void FlashPlaceholders() {
		if (flashing != null) {
			StopCoroutine(flashing);
		}
		flashing = StartCoroutine(Flash());
	}

	public void OnOrbCountChange(int numberOfOrbsPlaced) {
		UpdateOrbSpriteCount(numberOfOrbsPlaced);
	}
	
	#endregion

	IEnumerator Flash() {
		int numFlashesCompleted = 0;
		while (numFlashesCompleted < NUM_FLASHES) {
			SetPlaceholderColour(PLACEHOLDER_FLASH_COLOUR);
			yield return new WaitForSeconds(FLASH_LENGTH);
			SetPlaceholderColour(PLACEHOLDER_DEFAULT_COLOUR);
			yield return new WaitForSeconds(FLASH_LENGTH);
			numFlashesCompleted++;
		}
	}

	void SetPlaceholderColour(Color newColour) {
		for (int i = 0; i < maxOrbCount; i++) {
			orbSpritePlaceholders[i].color = newColour;
		}
	}
}
