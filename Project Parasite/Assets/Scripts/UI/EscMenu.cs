using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EscMenu : MonoBehaviour
{

    #region [Private Variables]

    bool isValid = false;
    Image bg;
    
    #endregion

    #region [Public Methods]
    
    public void SetValid(bool newValue) {
        // Should only be able to toggle this menu when the player is in game
        isValid = newValue;
        if (!isValid) {
            HideMenu();
        }
    }
    
    #endregion

    #region [MonoBehaviour Callbacks]

    void Start() {
        bg = GetComponent<Image>();
        HideMenu();
    }
    
    void Update() {
        if (!isValid) { return; }
        if (Input.GetKeyUp(KeyCode.Escape)) {
            ToggleMenu();
        }
    }
    
    #endregion

    #region [Private Methods]

    void ToggleMenu() {
        if (transform.GetChild(0).gameObject.activeSelf) {
            HideMenu();
        } else {
            ShowMenu();
        }
    }
    
    void ShowMenu() {
        bg.enabled = true;
        for (int i = 0; i < transform.childCount; i++) {
            transform.GetChild(i).gameObject.SetActive(true);
        }
    }

    void HideMenu() {
        for (int i = 0; i < transform.childCount; i++) {
            transform.GetChild(i).gameObject.SetActive(false);
        }
        bg.enabled = false;
    }
    
    #endregion

}
