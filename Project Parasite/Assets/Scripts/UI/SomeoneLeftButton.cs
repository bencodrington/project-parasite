using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SomeoneLeftButton : MonoBehaviour
{
    #region [Public Variables]
    
    public GameObject someoneLeftPanel;
    
    #endregion

    #region [Public Methods]
    
    public void OnClick() {
        someoneLeftPanel.SetActive(false);
    }
    
    #endregion
}
