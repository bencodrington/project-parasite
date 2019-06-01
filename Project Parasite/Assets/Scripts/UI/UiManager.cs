using System;
using System.Collections;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;

public class UiManager : MonoBehaviour, IOnEventCallback
{

    #region [Public Variables]
    
    public static UiManager Instance;
	public GameObject GameOverScreenPrefab;
	public GameObject GameOverScreenServerPrefab;
    
	public GameObject ParasiteControlsPrefab;
	public GameObject HunterControlsPrefab;
	public GameObject NpcCountPrefab;
    
    #endregion

    #region [Private Variables]

	Text npcCountText;
	GameObject controlsObject;
    
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

	GameObject titleScreen;
	GameObject mainMenu;
	GameObject lobby;
	GameObject startGameButton;
    GameObject readyToggleButton;
    GameObject randomParasiteToggleButton;
	GameObject selectParasiteButton;
	GameObject selectHunterButton;
	GameObject returnToMenuPanel;

	// CLEANUP: this should be extracted to its own file
	// Used for cycling parasite health colours
	Dictionary<Color, Color> parasiteHealthColourMap;
	Coroutine parasiteHealthColourFade;
	bool parasiteTakingDamage = false;
	Color parasiteHealthStartingColour;
	Color parasiteHealthCurrentColour;
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

	public void UpdateHealthObject(int newValue, bool isTakingDamage = false) {
		topRightUiText.text = newValue.ToString();
		parasiteTakingDamage = isTakingDamage;
		if (!isTakingDamage && parasiteHealthColourFade != null) {
			// (Re)setting initial health, so cancel fade
			StopCoroutine(parasiteHealthColourFade);
			topRightUiText.color = parasiteHealthStartingColour;
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
		foreach (CharacterSelectButton selectButton in FindObjectsOfType<CharacterSelectButton>()) {
			if (selectButton.characterType == CharacterType.Parasite) {
				selectParasiteButton = selectButton.gameObject;
			} else if (selectButton.characterType == CharacterType.Hunter) {
				selectHunterButton = selectButton.gameObject;
			}
		}
		mainMenu.SetActive(false);
		lobby.SetActive(false);
		startGameButton.SetActive(false);
        readyToggleButton.SetActive(false);
		randomParasiteToggleButton.SetActive(false);
		returnToMenuPanel.SetActive(false);

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
		parasiteHealthColourMap = new Dictionary<Color, Color>();
		parasiteHealthColourMap.Add(Color.red, Color.cyan);
		parasiteHealthColourMap.Add(Color.cyan, Color.yellow);
		parasiteHealthColourMap.Add(Color.yellow, Color.red);
		parasiteHealthCurrentColour = Color.red;
	}

	Color GetParasiteHealthColour(Color currentColour) {
		// Switch to next colour
		parasiteHealthColourMap.TryGetValue(currentColour, out currentColour);
		return currentColour;
	}

	void OnTakingDamage() {
		if (parasiteHealthColourFade != null) {
			StopCoroutine(parasiteHealthColourFade);
		}
		parasiteHealthColourFade = StartCoroutine(FadeParasiteHealthColour());
	}

	IEnumerator FadeParasiteHealthColour() {
		float timeElapsed = 0f;
		float progress = 0f;
		// Switch to next colour in the map
		parasiteHealthCurrentColour = GetParasiteHealthColour(parasiteHealthCurrentColour);
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
    			controlsObject = Instantiate(HunterControlsPrefab, Vector3.zero, Quaternion.identity, canvas);
    			break;
    		case CharacterType.Parasite:
    			topRightUiText.enabled = true;
    			UpdateHealthObject(ParasiteData.STARTING_HEALTH);
    			controlsObject = Instantiate(ParasiteControlsPrefab, Vector3.zero, Quaternion.identity, canvas);
    			break;
    		default:
    			topRightUiText.enabled = false;
    			break;
    	}
    	controlsObject.GetComponentInChildren<RectTransform>().anchoredPosition = new Vector2(UI_PADDING_DISTANCE, UI_PADDING_DISTANCE);
    	// Display NPC count
    	GameObject npcCountObject = Instantiate(NpcCountPrefab, Vector3.zero, Quaternion.identity, canvas);
    	npcCountObject.GetComponentInChildren<RectTransform>().anchoredPosition = new Vector2(UI_PADDING_DISTANCE, -UI_PADDING_DISTANCE);
        npcCountText = npcCountObject.GetComponentInChildren<Text>();
        // The original SetNpcCount event has likely already happened, so request
        //  the value be resent
        EventCodes.RaiseEventAll(EventCodes.RequestNpcCount, null);
    }

    void RemoveHud() {
    	if (topRightUiText != null) {
    		topRightUiText.enabled = false;
    	}
    	if (npcCountText != null) {
    		Destroy(npcCountText.gameObject);
    	}
    	if (controlsObject != null) {
    		Destroy(controlsObject);
    	}
    }
    
    #endregion
}
