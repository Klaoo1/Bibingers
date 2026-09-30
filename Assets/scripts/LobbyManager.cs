using UnityEngine;
using TMPro;
using Mirror;

public class LobbyManager : NetworkBehaviour
{
    public TMP_Text playerStatus;

    [SyncVar]
    private int readyPlayers = 0;

    private bool isReady = false;

    public override void OnStartServer()
    {
        base.OnStartServer();
        UpdateLobby();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        UpdateLobby();
    }

    [Command]
    public void CmdReady()
    {
        if (isReady)
            return;

        isReady = true;
        readyPlayers++;

        UpdateLobby();

        Debug.Log("Player is ready.");

        if (readyPlayers >= 2)
        {
            Debug.Log("Both players are ready!");
        }
    }

    public void OnReadyButtonPressed()
    {
        if (isReady)
            return;

        CmdReady();
    }

    private void UpdateLobby()
    {
        if (playerStatus == null)
            return;

        int players = NetworkServer.connections.Count;

        playerStatus.text =
            "Players Connected: " + players +
            "\nPlayers Ready: " + readyPlayers;
    }
}