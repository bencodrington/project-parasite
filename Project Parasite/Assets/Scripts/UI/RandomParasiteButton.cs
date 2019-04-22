﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RandomParasiteButton : MonoBehaviour
{

    #region [Private Variables]
    
    Toggle toggle;
    
    #endregion

    #region [MonoBehaviour Callbacks]
    
    public void Start() {
        toggle = GetComponent<Toggle>();
        if (toggle == null) {
            Debug.LogError("RandomParasiteButton: OnEnable: Toggle not found.");
        }
    }
    
    #endregion

    #region [Public Methods]

    public void ToggleIsRandomParasite() {
        bool isRandomParasite = toggle.isOn;
        MatchManager.Instance.SetIsRandomParasite(isRandomParasite);
    }

    #endregion
}
