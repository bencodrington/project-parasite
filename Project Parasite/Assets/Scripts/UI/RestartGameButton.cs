﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RestartGameButton : MonoBehaviour
{

    #region [Public Methods]
    
    public void RestartGame() {
        MatchManager.Instance.SendStartGameEvent();
    }
    
    #endregion
    
}
