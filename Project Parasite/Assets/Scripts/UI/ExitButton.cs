using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExitButton : MonoBehaviour
{
    
    #region [Public Methods]
    
    public void OnClick() {
        Application.Quit();
    }
    
    #endregion

}
