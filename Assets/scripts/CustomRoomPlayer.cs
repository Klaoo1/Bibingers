using Mirror;
using UnityEngine;

public class CustomRoomPlayer : NetworkRoomPlayer
{
    [SyncVar]
    public bool isLumen;

    [Command]
    public void CmdSetRole(bool lumen)
    {
        isLumen = lumen;
    }
}