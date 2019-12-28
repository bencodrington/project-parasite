using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Pun;

public class WaitingFor : MonoBehaviour
{

    #region [Private Variables]

    string CLIENT_TEXT = "Waiting for Group Leader...";
    string LEADER_TEXT = "Waiting for players...";

    
    TextMeshProUGUI text;
    
    #endregion

    #region [MonoBehaviour Callbacks]

    void OnEnable() {
        text = GetComponent<TextMeshProUGUI>();
        if (text == null) { return; }
        if (PhotonNetwork.IsMasterClient) {
            text.text = LEADER_TEXT;
        } else {
            text.text = CLIENT_TEXT;
        }
    }

    #endregion

}
