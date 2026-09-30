using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using TMPro;
using System.Collections.Generic;

public class MultiplayerChat : NetworkBehaviour
{
    [Header("Chat UI")]
    public TMP_InputField chatInput;
    public TMP_Text chatText;

    [Header("Scrolling")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform content;

    [Header("Message Settings")]
    [SerializeField] private int maxMessageLength = 200;
    [SerializeField] private int maxNickLength = 20;

    // Local player's handle
    private string localHandle;

    // Server-side player handles
    private static Dictionary<ulong, string> playerHandles =
        new Dictionary<ulong, string>();

    private void Start()
    {
        localHandle = "User_" + Random.Range(100, 1000);

        if (chatInput != null)
        {
            chatInput.ActivateInputField();
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            NetworkManager.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;
        }

        if (IsClient)
        {
            AddLocalMessage("[SYSTEM] Your handle: " + localHandle);

            if (chatInput != null)
            {
                chatInput.ActivateInputField();
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && NetworkManager != null)
        {
            NetworkManager.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
        }

        base.OnNetworkDespawn();
    }

    // =========================================================
    // SEND MESSAGE
    // =========================================================

    public void SendMessage()
    {
        if (chatInput == null)
            return;

        string message = chatInput.text.Trim();

        if (string.IsNullOrEmpty(message))
        {
            chatInput.ActivateInputField();
            return;
        }

        // Commands stay local
        if (message.StartsWith("/"))
        {
            ProcessCommand(message);

            chatInput.text = "";
            chatInput.ActivateInputField();

            return;
        }

        // Normal message goes to server
        SendMessageServerRpc(message);

        chatInput.text = "";
        chatInput.ActivateInputField();
    }

    // =========================================================
    // LOCAL COMMANDS
    // =========================================================

    private void ProcessCommand(string command)
    {
        string[] parts = command.Split(' ');

        string commandName = parts[0].ToLower();

        // -----------------------------------------------------
        // /nick <new_handle>
        // -----------------------------------------------------

        if (commandName == "/nick")
        {
            if (parts.Length < 2)
            {
                AddLocalMessage("[SYSTEM] Usage: /nick <new_handle>");
                return;
            }

            string newHandle = parts[1].Trim();

            if (newHandle.Length < 2)
            {
                AddLocalMessage("[SYSTEM] Handle must be at least 2 characters.");
                return;
            }

            if (newHandle.Length > maxNickLength)
            {
                AddLocalMessage(
                    "[SYSTEM] Handle cannot exceed " +
                    maxNickLength +
                    " characters."
                );

                return;
            }

            // Change it locally immediately
            localHandle = newHandle;

            // Tell the server
            ChangeNickServerRpc(newHandle);

            AddLocalMessage(
                "[SYSTEM] Your handle is now: " +
                localHandle
            );

            return;
        }

        // -----------------------------------------------------
        // /clear
        // -----------------------------------------------------

        if (commandName == "/clear")
        {
            if (chatText != null)
            {
                chatText.text = "";
            }

            return;
        }

        // -----------------------------------------------------
        // /ping
        // -----------------------------------------------------

        if (commandName == "/ping")
        {
            float frameTime = Time.deltaTime * 1000f;

            string latencyText = "N/A";

            if (NetworkManager.Singleton != null &&
                NetworkManager.Singleton.IsConnectedClient)
            {
                try
                {
                    double rtt =
                        NetworkManager.Singleton
                        .NetworkConfig
                        .NetworkTransport
                        .GetCurrentRtt(
                            NetworkManager.Singleton.LocalClientId
                        );

                    latencyText = rtt.ToString("F0") + " ms";
                }
                catch
                {
                    latencyText = "Unavailable";
                }
            }

            AddLocalMessage(
                "[PING] Frame: " +
                frameTime.ToString("F1") +
                " ms | Network: " +
                latencyText
            );

            return;
        }

        AddLocalMessage(
            "[SYSTEM] Unknown command: " +
            commandName
        );
    }

    // =========================================================
    // CHANGE NICKNAME - SERVER
    // =========================================================

    [Rpc(SendTo.Server)]
    private void ChangeNickServerRpc(
        string newHandle,
        RpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;

        // Validate
        if (string.IsNullOrWhiteSpace(newHandle))
            return;

        if (newHandle.Length < 2)
            return;

        if (newHandle.Length > maxNickLength)
            return;

        // Check whether another player already has this handle
        foreach (var pair in playerHandles)
        {
            if (pair.Key == senderId)
                continue;

            if (pair.Value.ToLower() == newHandle.ToLower())
            {
                SendNickRejectedClientRpc(
                    "Handle '" +
                    newHandle +
                    "' is already in use."
                );

                return;
            }
        }

        // Update the server-authoritative handle
        playerHandles[senderId] = newHandle;

        // Tell everyone about the nickname change
        SendSystemMessageClientRpc(
            "[NICK] Player " +
            senderId +
            " is now " +
            newHandle
        );
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SendNickRejectedClientRpc(string message)
    {
        AddLocalMessage("[SYSTEM] " + message);
    }

    // =========================================================
    // NORMAL MESSAGE - CLIENT → SERVER
    // =========================================================

    [Rpc(SendTo.Server)]
    private void SendMessageServerRpc(
        string message,
        RpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;

        // Validate message
        if (string.IsNullOrWhiteSpace(message))
            return;

        if (message.Length > maxMessageLength)
        {
            SendMessageRejectedClientRpc(
                "Message rejected. Maximum length is " +
                maxMessageLength +
                " characters."
            );

            return;
        }

        // Make sure the player has a handle
        if (!playerHandles.ContainsKey(senderId))
        {
            playerHandles[senderId] =
                "User_" + Random.Range(100, 1000);
        }

        string senderHandle =
            playerHandles[senderId];

        // Server broadcasts the message
        ReceiveMessageClientRpc(
            senderHandle,
            message
        );
    }

    // =========================================================
    // SERVER → ALL CLIENTS
    // =========================================================

    [Rpc(SendTo.ClientsAndHost)]
    private void ReceiveMessageClientRpc(
        string senderHandle,
        string message)
    {
        AddLocalMessage(
            "<" +
            senderHandle +
            "> " +
            message
        );
    }

    // =========================================================
    // CONNECTION
    // =========================================================

    private void OnClientConnected(ulong clientId)
    {
        string handle;

        do
        {
            handle =
                "User_" +
                Random.Range(100, 1000);

        } while (playerHandles.ContainsValue(handle));

        playerHandles[clientId] = handle;

        SendSystemMessageClientRpc(
            handle +
            " connected."
        );
    }

    private void OnClientDisconnected(ulong clientId)
    {
        string handle = "Unknown";

        if (playerHandles.ContainsKey(clientId))
        {
            handle =
                playerHandles[clientId];

            playerHandles.Remove(clientId);
        }

        SendSystemMessageClientRpc(
            handle +
            " disconnected."
        );
    }

    // =========================================================
    // SYSTEM MESSAGES
    // =========================================================

    [Rpc(SendTo.ClientsAndHost)]
    private void SendSystemMessageClientRpc(
        string message)
    {
        AddLocalMessage(
            "[SYSTEM] " +
            message
        );
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SendMessageRejectedClientRpc(
        string message)
    {
        AddLocalMessage(
            "[SYSTEM] " +
            message
        );
    }

    // =========================================================
    // TERMINAL OUTPUT
    // =========================================================

    private void AddLocalMessage(string message)
    {
        if (chatText == null)
            return;

        chatText.text +=
            "\n" +
            message;

        // Update TMP
        chatText.ForceMeshUpdate();

        // Resize scrolling content
        if (content != null)
        {
            float requiredHeight =
                chatText.preferredHeight + 20f;

            float newHeight =
                Mathf.Max(
                    requiredHeight,
                    210f
                );

            content.sizeDelta =
                new Vector2(
                    content.sizeDelta.x,
                    newHeight
                );
        }

        // Update UI
        Canvas.ForceUpdateCanvases();

        // Scroll to newest message
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }

    // =========================================================
    // INPUT FOCUS
    // =========================================================

    private void Update()
    {
        if (!IsClient)
            return;

        // Enter sends message
        if (Input.GetKeyDown(KeyCode.Return))
        {
            if (chatInput != null &&
                chatInput.isFocused)
            {
                SendMessage();
            }
        }

        // Keep input focused
        if (chatInput != null &&
            !chatInput.isFocused &&
            Input.GetMouseButtonDown(0))
        {
            chatInput.ActivateInputField();
        }
    }
}