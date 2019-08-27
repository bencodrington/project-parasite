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
    int startingNpcCount = -1;
    bool hasMutated = false;
    
    #endregion

    #region [Public Methods]

    public NoMoreNPCsWinCondition() {
		PhotonNetwork.AddCallbackTarget(this);
        // The original SetNpcCount event has already happened, so request
        //  the value be resent
        EventCodes.RaiseEventAll(EventCodes.RequestNpcCount, null);
    }

    public void OnEvent(EventData photonEvent) {
        switch (photonEvent.Code) {
        case EventCodes.SetNpcCount:
            SetNpcCount((int)EventCodes.GetFirstEventContent(photonEvent));
            break;
        case EventCodes.NpcDespawned:
            SetNpcCount(npcCount - 1);
            if (PhotonNetwork.IsMasterClient && npcCount == 0) {
                OnLastNpcKilled();
            }
            if (PhotonNetwork.IsMasterClient && !hasMutated && npcCount <= (0.5f * startingNpcCount)) {
                EventCodes.RaiseEventAll(EventCodes.Mutation, null);
                // Don't repeatedly raise the event
                hasMutated = true;
            }
            break;
        }
    }
    
    #endregion

    protected virtual void OnLastNpcKilled() {
        // Parasites Win
        Debug.Log("Parasite Wins!");
        EventCodes.RaiseGameOverEvent(CharacterType.Parasite);
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    #region [Private Methods]
    
    void SetNpcCount(int newNpcCount) {
        if (startingNpcCount < 0) {
            // This is the first time npc count is being set
            startingNpcCount = newNpcCount;
        }
        npcCount = newNpcCount;
        UiManager.Instance.SetRemainingNpcCount(npcCount);
    }
    
    #endregion
}
