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
        object[] content;
        if (photonEvent.Code == EventCodes.SetNpcCount) {
            content = (object[])photonEvent.CustomData;
            npcCount = (int)content[0];
        } else if (photonEvent.Code == EventCodes.NpcDespawned) {
            npcCount--;
            if (npcCount == 0) {
                // TODO:
                // Parasites Win
                Debug.Log("Parasite Wins!");
                EventCodes.RaiseGameOverEvent(CharacterType.Parasite);
		        PhotonNetwork.RemoveCallbackTarget(this);
            }
        }
    }
    
    #endregion

    
}
