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
    
    #endregion

    public void OnEvent(EventData photonEvent)
    {
        switch (photonEvent.Code) {
            case EventCodes.StartGame:
                DestroyTitleScreen();
                HideMenu();
				DestroyGameOverScreen();
				RemoveHud();
                break;
			case EventCodes.AssignPlayerType:
                UpdateHud();
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
    }

    void Start() {
		topRightUiText = GameObject.FindGameObjectWithTag("TopRightUI").GetComponent<Text>();
        canvas = FindObjectOfType<Canvas>().transform;
    }

    void OnDisable() {
        PhotonNetwork.RemoveCallbackTarget(this);
    }
    
    #endregion

    #region [Private Methods]

	public void UpdateHealthObject(int newValue) {
		topRightUiText.text = newValue.ToString();
	}

    void DestroyTitleScreen() {
        Destroy(GameObject.FindWithTag("TitleScreen"));
    }

    void HideMenu() {
        Menu menu = FindObjectOfType<Menu>();
        if (menu == null) {
            Debug.LogError("UiManager: HideMenu: Menu not found");
            return;
        }
        menu.DeleteMenuItems();
    }

    void ShowGameOverScreen(CharacterType victorType) {
		// Don't spawn another gameover screen if one already exists
		if (gameOverScreen != null) { return; }
		gameOverScreen = PhotonNetwork.IsMasterClient ? Instantiate(GameOverScreenServerPrefab) : Instantiate(GameOverScreenPrefab);
		gameOverScreen.transform.SetParent(canvas);
		RectTransform rect = gameOverScreen.GetComponent<RectTransform>();
		// Position gameOverScreen;
		rect.anchoredPosition = new Vector2(0.5f, 0.5f);
		rect.offsetMax = Vector2.zero;
		rect.offsetMin = Vector2.zero;

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
    			controlsObject = Instantiate(HunterControlsPrefab, Vector3.zero, Quaternion.identity, FindObjectOfType<Canvas>().transform);
    			break;
    		case CharacterType.Parasite:
    			topRightUiText.enabled = true;
    			UpdateHealthObject(PlayerObject.STARTING_PARASITE_HEALTH);
    			controlsObject = Instantiate(ParasiteControlsPrefab, Vector3.zero, Quaternion.identity, FindObjectOfType<Canvas>().transform);
    			break;
    		default:
    			topRightUiText.enabled = false;
    			break;
    	}
    	controlsObject.GetComponentInChildren<RectTransform>().anchoredPosition = new Vector2(UI_PADDING_DISTANCE, UI_PADDING_DISTANCE);
    	// Display NPC count 
    	GameObject npcCountObject = Instantiate(NpcCountPrefab, Vector3.zero, Quaternion.identity, FindObjectOfType<Canvas>().transform);
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
