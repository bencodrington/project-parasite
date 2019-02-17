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
    
    public void SetNpcCount(int newNpcCount) {
        npcCount = newNpcCount;
        UiManager.Instance.SetRemainingNpcCount(npcCount);
    }

    public void OnEvent(EventData photonEvent) {
        if (photonEvent.Code == EventCodes.SetNpcCount) {
            SetNpcCount((int)EventCodes.GetFirstEventContent(photonEvent));
        } else if (photonEvent.Code == EventCodes.NpcDespawned) {
            SetNpcCount(npcCount - 1);
            if (npcCount == 0) {
                // Parasites Win
                Debug.Log("Parasite Wins!");
                EventCodes.RaiseGameOverEvent(CharacterType.Parasite);
		        PhotonNetwork.RemoveCallbackTarget(this);
            }
        }
    }
    
    #endregion

    
}
