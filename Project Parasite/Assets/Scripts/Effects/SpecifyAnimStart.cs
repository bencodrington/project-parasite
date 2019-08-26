using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpecifyAnimStart : MonoBehaviour
{
    #region [Public Variables]
    
    [Range(0, 1)]
    public float normalizedTime;
    public string animationStateName;
    
    #endregion

    #region [MonoBehaviour Callbacks]
    
    void Start() {
        GetComponent<Animator>().Play(animationStateName, 0, normalizedTime);
    }
    
    #endregion
}
