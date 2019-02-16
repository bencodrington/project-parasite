using System.Collections;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class NoMoreNPCsWinCondition : IOnEventCallback
{
    #region [Private Variables]
    
    int npcCount;
    
    #endregion

    #region [Public Methods]

    public NoMoreNPCsWinCondition() {
		PhotonNetwork.AddCallbackTarget(this);
    }
    
    public void SetNpcCount(int startingCount) {
        npcCount = startingCount;
    }

    public void OnEvent(EventData photonEvent) {
        if (photonEvent.Code == EventCodes.NpcDespawned) {
            npcCount--;
            if (npcCount <= 0) {
                // TODO:
                // Hunters Win
                Debug.Log("HUNTERS WIN");
		        PhotonNetwork.RemoveCallbackTarget(this);
            }
        }
    }
    
    #endregion

    
}
