using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;

public class EventCodes
{
    public const byte SetReady          = 0;
    public const byte StartGame         = 1;
    public const byte AssignPlayerType  = 2;
    public const byte NpcDespawned      = 3;
    public const byte SetNpcCount       = 4;
    public const byte GameOver          = 5;

    public static void RaiseEventAll(byte eventCode, object[] content) {
        RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All };
        SendOptions sendOptions = new SendOptions { Reliability = true };
        // Send to all players
        PhotonNetwork.RaiseEvent(eventCode, content, raiseEventOptions, sendOptions);
    }

    public static void RaiseGameOverEvent(CharacterType victorType) {
        object[] content = new object[] { victorType };
        RaiseEventAll(GameOver, content);
    }

    public static object GetEventContentAtPosition(EventData photonEvent, int pos) {
        object[] content = (object[])photonEvent.CustomData;
        return content[pos];
    }

    public static object GetFirstEventContent(EventData photonEvent) {
        return GetEventContentAtPosition(photonEvent, 0);
    }
}
