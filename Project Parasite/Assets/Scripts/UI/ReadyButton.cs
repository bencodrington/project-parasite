using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using UnityEngine.UI;

public class ReadyButton : MonoBehaviour
{

    #region [Private Variables]
    
    Toggle toggle;
    
    #endregion

    #region [MonoBehaviour Callbacks]
    
    public void Start() {
        toggle = GetComponent<Toggle>();
        if (toggle == null) {
            Debug.LogError("ReadyButton: OnEnable: Toggle not found.");
        }
    }
    
    #endregion

    #region [Public Methods]

    public void SetReady() {
        // Construct event
        bool isReady = toggle.isOn;
        byte eventCode = EventCodes.SetReady;
        object[] content = new object[] { PhotonNetwork.LocalPlayer.ActorNumber, isReady };
        EventCodes.RaiseEventAll(eventCode, content);
    }

    #endregion
}
