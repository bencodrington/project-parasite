using System;
using System.Collections;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UiManager : MonoBehaviour, IOnEventCallback
{

    #region [Public Variables]
    
    public static UiManager Instance;
	public OrbUiManager orbUiManager {get; private set;}
	public GameObject GameOverScreenPrefab;
	public GameObject GameOverScreenServerPrefab;
    
	public GameObject NpcCountPrefab;

	public GameObject[] ParasiteControls;
	public GameObject[] NpcControls;
	public GameObject[] HunterControls;
	public GameObject[] ActiveMutations;

	public Color[] flashColours;
    
    #endregion

    #region [Private Variables]

	Text npcCountText;
    
	GameObject gameOverScreen;
	Text topRightUiText;
    Transform canvas;

	CharacterType characterType;

	// The text shown on the game over screen
	const string HUNTERS_WIN = "HUNTERS WIN!";
	const string PARASITE_WINS = "PARASITE WINS!";
	// The colour of the text shown on the game over screen
	Color WIN_COLOUR = Color.green;
	Color LOSS_COLOUR = Color.red;

	const int UI_PADDING_DISTANCE = 9;
	// How long should the highlight animation play for
	const float CONTROL_HIGHLIGHT_FADE_TIME = 2f;
	// How long between flipping the control alpha on and off
	const float CONTROL_HIGHLIGHT_FLASH_LENGTH = 0.1f;
	// The colour of the control text when highlighted
	Color CONTROL_HIGHLIGHT_COLOUR = Color.yellow;

	GameObject titleScreen;
	GameObject mainMenu;
	GameObject lobby;
	GameObject startGameButton;
    GameObject readyToggleButton;
    GameObject randomParasiteToggleButton;
	GameObject selectParasiteButton;
	GameObject selectHunterButton;
	GameObject returnToMenuPanel;
	GameObject someoneLeftPanel;

	// Used for cycling parasite health colours
	Coroutine parasiteHealthColourFade;
	bool parasiteTakingDamage = false;
	Color parasiteHealthStartingColour;
	ColourRotator parasiteHealthColourRotator;
	const float PARASITE_HEALTH_COLOUR_FADE_TIME = 1f;
    
    #endregion

    public void OnEvent(EventData photonEvent)
    {
        switch (photonEvent.Code) {
            case EventCodes.StartGame:
                HideMenu();
				DestroyGameOverScreen();
				RemoveHud();
                break;
			case EventCodes.AssignPlayerTypeAndSpawnPoint:
				int actorNumber = (int)EventCodes.GetEventContentAtPosition(photonEvent, 0);
				if (actorNumber == PhotonNetwork.LocalPlayer.ActorNumber) {
					SetCharacterType((CharacterType)EventCodes.GetEventContentAtPosition(photonEvent, 1));
				}
                break;
            case EventCodes.GameOver: 
                // Deconstruct event
                CharacterType victorType = (CharacterType)EventCodes.GetFirstEventContent(photonEvent);
                ShowGameOverScreen(victorType);
                break;
			case EventCodes.SetNpcCount:
				SetRemainingNpcCount((int)EventCodes.GetFirstEventContent(photonEvent));
				break;
        }
    }

    #region [Public Methods]

    public void SetRemainingNpcCount(int remainingNpcCount) {
        if (npcCountText != null) {
            npcCountText.text = "Living civilians: " + remainingNpcCount.ToString();
        }
    }

	public void SetCharacterType(CharacterType type) {
		characterType = type;
		UpdateHud();
	}

	public Transform GetCanvas() {
		return canvas;
	}

	public void ShowMainMenu() {
		SetTitleScreenActive(true);
		mainMenu.SetActive(true);
		lobby.SetActive(false);
		DestroyGameOverScreen();
		RemoveHud();
	}

	public void OnJoinedRoom() {
		// Show the lobby
		mainMenu.SetActive(false);
		lobby.SetActive(true);
		// Only show the Random Character Select toggle button if this is the Master Client
		randomParasiteToggleButton.SetActive(PhotonNetwork.IsMasterClient);
	}

	public void SetStartGameButtonActive(bool isActive) {
		startGameButton.SetActive(isActive);
	}

	public void UpdateHealthObject(int newValue, bool isTakingDamage = false, bool isRegainingHealth = false) {
		topRightUiText.text = newValue.ToString() + " HP";
		parasiteTakingDamage = isTakingDamage;
		if (parasiteHealthColourFade != null) {
			// Cancel fade
			StopCoroutine(parasiteHealthColourFade);
			topRightUiText.color = parasiteHealthStartingColour;
		}
		if (isRegainingHealth) {
			parasiteHealthColourFade = StartCoroutine(FadeParasiteHealthColour(Color.green));
		}
	}

	public void OnIsRandomParasiteChanged(bool isRandomParasite) {
        // Only need to show ready button if parasite is randomly selected
        readyToggleButton.SetActive(isRandomParasite);
		// Only need to show character select buttons if it isn't
		selectHunterButton.SetActive(!isRandomParasite);
		selectParasiteButton.SetActive(!isRandomParasite);
	}

    public void HideMenu() {
		SetTitleScreenActive(false);
		mainMenu.SetActive(false);
		lobby.SetActive(false);
    }

	public void SetReturnToMenuPanelActive(bool isActive) {
		returnToMenuPanel.SetActive(isActive);
	}

	public void DeactivateControls() {
		foreach (GameObject control in ParasiteControls) {
			control.SetActive(false);
		}
		foreach (GameObject control in NpcControls) {
			control.SetActive(false);
		}
		foreach (GameObject control in HunterControls) {
			control.SetActive(false);
		}
	}

	public void ActivateControlAtIndex(int index, bool shouldHighlight = false) {
		GameObject control = null;
		if (characterType == CharacterType.Parasite) {
			control = ParasiteControls[index];
		} else if (characterType == CharacterType.Hunter) {
			control = HunterControls[index];
		} else if (characterType == CharacterType.NPC) {
			control = NpcControls[index];
		}
		if (control != null) {
			control.SetActive(true);
			if (shouldHighlight) {
				StartCoroutine(HighlightControl(control));
			}
		}
	}

	public void ActivateControls(CharacterType characterType) {
		DeactivateControls();
		switch (characterType) {
			case CharacterType.Parasite: 	ActivateControls(ParasiteControls); break;
			case CharacterType.NPC: 		ActivateControls(NpcControls); 		break;
			case CharacterType.Hunter: 		ActivateControls(HunterControls); 	break;
		}
	}

    public void RemoveHud() {
    	if (topRightUiText != null) {
    		topRightUiText.enabled = false;
    	}
    	if (npcCountText != null) {
    		Destroy(npcCountText.gameObject);
    	}
		DeactivateControls();
		DeactivateMutations();
    }

	public void ActivateMutation(int index) {
		ActiveMutations[index].SetActive(true);
	}

	public void OnSomeoneLeft(string playername) {
		someoneLeftPanel.SetActive(true);
		someoneLeftPanel.GetComponentInChildren<TextMeshProUGUI>().text = playername + " left the game.";
	}
    
    #endregion

    #region [MonoBehaviour Callbacks]

    void OnEnable() {
        PhotonNetwork.AddCallbackTarget(this);
    }
    
    void Awake() {
        if (Instance != null) {
            Debug.LogError("UiManager:Awake: Attempting to make a second UiManager");
            return;
        }
        Instance = this;
		titleScreen = GameObject.FindWithTag("TitleScreen");
		mainMenu = GameObject.FindGameObjectWithTag("Main Menu");
		lobby = GameObject.FindGameObjectWithTag("Lobby");
		startGameButton = GameObject.FindObjectOfType<StartGameButton>().gameObject;
        readyToggleButton = FindObjectOfType<ReadyButton>().gameObject;
        randomParasiteToggleButton = FindObjectOfType<RandomParasiteButton>().gameObject;
		returnToMenuPanel = FindObjectOfType<ReturnToMenuButton>().transform.parent.gameObject;
		someoneLeftPanel = FindObjectOfType<SomeoneLeftButton>().transform.parent.gameObject;
		foreach (CharacterSelectButton selectButton in FindObjectsOfType<CharacterSelectButton>()) {
			if (selectButton.characterType == CharacterType.Parasite) {
				selectParasiteButton = selectButton.gameObject;
			} else if (selectButton.characterType == CharacterType.Hunter) {
				selectHunterButton = selectButton.gameObject;
			}
		}
		orbUiManager = FindObjectOfType<OrbUiManager>();
		mainMenu.SetActive(false);
		lobby.SetActive(false);
		startGameButton.SetActive(false);
        readyToggleButton.SetActive(false);
		randomParasiteToggleButton.SetActive(false);
		returnToMenuPanel.SetActive(false);
		someoneLeftPanel.SetActive(false);
		DeactivateControls();
		DeactivateMutations();

		InitializeColourMap();
    }

    void Start() {
		// Cache the health text object and its starting colour
		topRightUiText = GameObject.FindGameObjectWithTag("TopRightUI").GetComponent<Text>();
		parasiteHealthStartingColour = topRightUiText.color;
		// Find main canvas before other canvases are created
        canvas = FindObjectOfType<Canvas>().transform;
    }

	void Update() {
		if (parasiteTakingDamage) {
			OnTakingDamage();
			parasiteTakingDamage = false;
		}
	}

    void OnDisable() {
        PhotonNetwork.RemoveCallbackTarget(this);
    }
    
    #endregion

    #region [Private Methods]

	void InitializeColourMap() {
		// Used for cycling parasite health text colours
		parasiteHealthColourRotator = new ColourRotator(flashColours);
	}

	void OnTakingDamage() {
		if (parasiteHealthColourFade != null) {
			StopCoroutine(parasiteHealthColourFade);
		}
		parasiteHealthColourFade = StartCoroutine(FadeParasiteHealthColour(parasiteHealthColourRotator.GetNextColour()));
	}

	IEnumerator FadeParasiteHealthColour(Color newColour) {
		float timeElapsed = 0f;
		float progress = 0f;
		// Switch to next colour in the map
		Color parasiteHealthCurrentColour = newColour;
		// Fade back to starting colour over time
		while (timeElapsed < PARASITE_HEALTH_COLOUR_FADE_TIME) {
			timeElapsed += Time.deltaTime;
			progress = timeElapsed / PARASITE_HEALTH_COLOUR_FADE_TIME;
			topRightUiText.color = Color.Lerp(parasiteHealthCurrentColour, parasiteHealthStartingColour, progress);
			yield return null;
		}
		topRightUiText.color = parasiteHealthStartingColour;
	}

    void SetTitleScreenActive(bool isActive) {
        titleScreen.SetActive(isActive);
    }

    void ShowGameOverScreen(CharacterType victorType) {
		// Don't spawn another gameover screen if one already exists
		if (gameOverScreen != null) { return; }
		gameOverScreen = PhotonNetwork.IsMasterClient ?
			Instantiate(GameOverScreenServerPrefab, canvas.position, Quaternion.identity, canvas) :
			Instantiate(GameOverScreenPrefab, canvas.position, Quaternion.identity, canvas);

		Transform VictorText = gameOverScreen.transform.Find("Victor");
		if (VictorText == null) {
			Debug.LogError("PlayerObject:ShowGameOverScreen: Victor Text not found");
			return;
		}
		Text txt = VictorText.GetComponent<Text>();
		txt.text = victorType == CharacterType.Hunter ? HUNTERS_WIN : PARASITE_WINS;
		txt.color = victorType == characterType ? WIN_COLOUR : LOSS_COLOUR;
	}

	void DestroyGameOverScreen() {
		// gameOverScreen should be null when starting the game from the main menu
		// 	as opposed to when restarting after a round has been completed
		if (gameOverScreen == null) { return; }
		Destroy(gameOverScreen.gameObject);
	}

    void UpdateHud() {
    	switch (characterType) {
    		case CharacterType.Hunter:
    			topRightUiText.enabled = false;
				ActivateControls(HunterControls);
    			break;
    		case CharacterType.Parasite:
    			topRightUiText.enabled = true;
    			UpdateHealthObject(ParasiteData.STARTING_HEALTH);
				ActivateControls(ParasiteControls);
    			break;
    		default:
    			topRightUiText.enabled = false;
    			break;
    	}
    	// Display NPC count
    	GameObject npcCountObject = Instantiate(NpcCountPrefab, Vector3.zero, Quaternion.identity, canvas);
    	npcCountObject.GetComponentInChildren<RectTransform>().anchoredPosition = new Vector2(UI_PADDING_DISTANCE, -UI_PADDING_DISTANCE);
        npcCountText = npcCountObject.GetComponentInChildren<Text>();
        // The original SetNpcCount event has likely already happened, so request
        //  the value be resent
        EventCodes.RaiseEventAll(EventCodes.RequestNpcCount, null);
    }

	void ActivateControls(GameObject[] controlsGroup) {
		foreach (GameObject control in controlsGroup) {
			control.SetActive(true);
		}
	}

	IEnumerator HighlightControl(GameObject control) {
		TextMeshProUGUI textMesh = control.GetComponentInChildren<TextMeshProUGUI>();
		Color originalColour = textMesh.color;
		textMesh.color = CONTROL_HIGHLIGHT_COLOUR;
		float timeElapsed = 0;
		int alphaToggle = 0;
		while (timeElapsed < CONTROL_HIGHLIGHT_FADE_TIME) {
			yield return null;
			timeElapsed += Time.deltaTime;
			// Every CONTROL_HIGHLIGHT_FLASH_LENGTH, this switches between 1 & 0
			alphaToggle = ((int)(timeElapsed / CONTROL_HIGHLIGHT_FLASH_LENGTH) % 2);
			textMesh.color = alphaToggle == 0
				? Color.Lerp(CONTROL_HIGHLIGHT_COLOUR, originalColour, timeElapsed / CONTROL_HIGHLIGHT_FADE_TIME)
				: Color.clear;
		}
		textMesh.color = originalColour;
	}

	void DeactivateMutations() {
		foreach (GameObject activeMutation in ActiveMutations) {
			activeMutation.SetActive(false);
		}
	}
    
    #endregion
}
