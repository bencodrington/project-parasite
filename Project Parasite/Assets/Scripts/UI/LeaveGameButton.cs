using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LeaveGameButton : MonoBehaviour
{
    
    #region [Public Methods]
    
    public void OnClick() {
        MatchManager.Instance.LeaveGame();
    }
    
    #endregion

}
