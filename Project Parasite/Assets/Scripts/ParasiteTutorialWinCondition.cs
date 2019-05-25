using Photon.Pun;

public class ParasiteTutorialWinCondition : NoMoreNPCsWinCondition
{

    protected override void OnLastNpcKilled() {
        UiManager.Instance.SetReturnToMenuPanelActive(true);
        PhotonNetwork.RemoveCallbackTarget(this);
    }

}
