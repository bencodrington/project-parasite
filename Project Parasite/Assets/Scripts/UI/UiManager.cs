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
    public CharacterType characterType;
    
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
				characterType = (CharacterType)EventCodes.GetEventContentAtPosition(photonEvent, 1);
				if (actorNumber == PhotonNetwork.LocalPlayer.ActorNumber) {
					UpdateHud();
				}
                break;
            case EventCodes.GameOver: 
                // Deconstruct event
                CharacterType victorType = (CharacterType)EventCodes.GetFirstEventContent(photonEvent);
                ShowGameOverScreen(victorType);
                break;
        }
    }

    #region [Public Methods]

    public void SetRemainingNpcCount(int remainingNpcCount) {
        if (npcCountText != null) {
            npcCountText.text = remainingNpcCount.ToString();
        };
    }

	public Transform GetCanvas() {
		return canvas;
	}

	public void ShowMainMenu() {
		SetTitleScreenActive(true);
		mainMenu.SetActive(true);
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

	public void UpdateHealthObject(int newValue) {
		topRightUiText.text = newValue.ToString();
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
    }

    void Start() {
		topRightUiText = GameObject.FindGameObjectWithTag("TopRightUI").GetComponent<Text>();
		// Find main canvas before other canvases are created
        canvas = FindObjectOfType<Canvas>().transform;
    }

    void OnDisable() {
        PhotonNetwork.RemoveCallbackTarget(this);
    }
    
    #endregion

    #region [Private Methods]

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
