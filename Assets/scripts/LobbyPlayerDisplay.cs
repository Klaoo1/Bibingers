using UnityEngine;
using TMPro;
using Mirror;

public class LobbyPlayerDisplay : MonoBehaviour
{
    public TMP_Text playerStatus;

    void Update()
    {
        if (playerStatus == null)
            return;

        NetworkRoomPlayer[] players =
            FindObjectsByType<NetworkRoomPlayer>(
                FindObjectsSortMode.None
            );

        playerStatus.text =
            "PLAYER 1: " + (players.Length >= 1 ? "CONNECTED" : "WAITING...") +
            "\nPLAYER 2: " + (players.Length >= 2 ? "CONNECTED" : "WAITING...");
    }
}