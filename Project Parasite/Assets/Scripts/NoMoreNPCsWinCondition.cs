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
        if (!PhotonNetwork.IsMasterClient) {
            // The original SetNpcCount event has likely already happened, so request
            //  the value be resent
            EventCodes.RaiseEventAll(EventCodes.RequestNpcCount, null);
        }
    }

    public void OnEvent(EventData photonEvent) {
        switch (photonEvent.Code) {
        case EventCodes.SetNpcCount:
            SetNpcCount((int)EventCodes.GetFirstEventContent(photonEvent));
            break;
        case EventCodes.NpcDespawned:
            SetNpcCount(npcCount - 1);
            if (PhotonNetwork.IsMasterClient && npcCount == 0) {
                // Parasites Win
                Debug.Log("Parasite Wins!");
                EventCodes.RaiseGameOverEvent(CharacterType.Parasite);
                PhotonNetwork.RemoveCallbackTarget(this);
            }
            break;
        case EventCodes.RequestNpcCount:
            if (PhotonNetwork.IsMasterClient) {
                // Resend the current NPC count
                object[] content = { npcCount };
                EventCodes.RaiseEventAll(EventCodes.SetNpcCount, content);
            }
            break;
        }
    }
    
    #endregion

    #region [Private Methods]
    
    void SetNpcCount(int newNpcCount) {
        npcCount = newNpcCount;
        UiManager.Instance.SetRemainingNpcCount(npcCount);
    }
    
    #endregion
}
