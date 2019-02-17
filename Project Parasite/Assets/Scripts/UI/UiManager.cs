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
    
    #endregion

    #region [Private Variables]
    
	GameObject gameOverScreen;
	Text topRightUiText;
    Transform canvas;

	// The text shown on the game over screen
	const string HUNTERS_WIN = "HUNTERS WIN!";
	const string PARASITE_WINS = "PARASITE WINS!";
	// The colour of the text shown on the game over screen
	Color WIN_COLOUR = Color.green;
	Color LOSS_COLOUR = Color.red;
    
    #endregion

    public void OnEvent(EventData photonEvent)
    {
        switch (photonEvent.Code) {
            case EventCodes.StartGame: 
				DestroyGameOverScreen();
				// TODO: destroy hud
                break;
			case EventCodes.AssignPlayerType:
                // TODO: update hud
                break;
            case EventCodes.GameOver: 
                // Deconstruct event
                CharacterType victorType = (CharacterType)EventCodes.GetFirstEventContent(photonEvent);
                ShowGameOverScreen(victorType);
                break;
            
        }
    }

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
    
    #endregion
}
