using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReturnToMenuButton : MonoBehaviour
{
    #region [Public Methods]
    
    public void OnClick() {
        MatchManager.Instance.EndTutorial();
    }
    
    #endregion
}
