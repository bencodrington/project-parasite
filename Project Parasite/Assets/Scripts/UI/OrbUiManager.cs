using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OrbUiManager : MonoBehaviour {

	#region [Public Variables]
	
	// The "Press Right Click to Recall Orb" alert that shows when
	// 	the player attempts to place an orb when they have none left
	public GameObject recallOrbAlertPrefab;

	public GameObject[] orbSprites;
	// The sprites that show even when the orb sprites are hidden
	// Only need to store images, not entire gameobject, as we will only
	// 	modify the colour, never the actual gameobject
	public Image[] orbSpritePlaceholders;
	
	#endregion

	#region [Private Variables]

	Color placeholderDefaultColour;
	Color PLACEHOLDER_FLASH_COLOUR = Color.red;

	int maxOrbCount;
	int numOrbSpritesEnabled;

	// Used when the placeholder sprites flash
	// 	- triggered when the hunter tries to place an orb but has none left
	Coroutine flashing;
	// How long each colour lasts in the flashing animation
	const float FLASH_LENGTH = .1f;
	// How many times the flashing animation repeats
	const int NUM_FLASHES = 4;

	#endregion

	#region [Public Methods]

	public void FlashPlaceholders() {
		if (flashing != null) {
			StopCoroutine(flashing);
		}
		flashing = StartCoroutine(Flash());
	}

	public void ShowRecallAlert() {
		// Spawn alert at mouse position
		Instantiate(recallOrbAlertPrefab,
					Utility.GetMousePos(),
					Quaternion.identity);
	}

	public void OnOrbCountChange(int numberOfOrbsPlaced) {
		UpdateOrbSpriteCount(numberOfOrbsPlaced);
	}

	public void Initialize() {
		// Reset state of orb UI
		UpdateOrbSpriteCount(0);
	}
	
	#endregion

	#region [MonoBehaviour Callbacks]
	
	void Start() {
		maxOrbCount = orbSprites.Length;
		numOrbSpritesEnabled = maxOrbCount;
		placeholderDefaultColour = orbSpritePlaceholders[0].color;
		Initialize();
		// Deactivate self
		gameObject.SetActive(false);
	}
	
	#endregion

	#region [Private Methods]
	
	IEnumerator Flash() {
		int numFlashesCompleted = 0;
		while (numFlashesCompleted < NUM_FLASHES) {
			SetPlaceholderColour(PLACEHOLDER_FLASH_COLOUR);
			yield return new WaitForSeconds(FLASH_LENGTH);
			SetPlaceholderColour(placeholderDefaultColour);
			yield return new WaitForSeconds(FLASH_LENGTH);
			numFlashesCompleted++;
		}
	}

	void SetPlaceholderColour(Color newColour) {
		for (int i = 0; i < maxOrbCount; i++) {
			orbSpritePlaceholders[i].color = newColour;
		}
	}

	void UpdateOrbSpriteCount(int numberOfOrbsPlaced) {
		int numOrbsRemaining = maxOrbCount - numberOfOrbsPlaced;
		if (flashing != null) {
			// Must have recalled an orb while flashing placeholders
			StopCoroutine(flashing);
			SetPlaceholderColour(placeholderDefaultColour);
		}
		while (numOrbSpritesEnabled < numOrbsRemaining) {
			// Enable sprite
			EnableOrbSprite();
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
	
	#endregion
}
