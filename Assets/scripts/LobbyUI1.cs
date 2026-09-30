using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class LobbyUI1 : MonoBehaviour
{
    public void HostGame()
    {
        if (NetworkManager.Singleton.StartHost())
        {
            NetworkManager.Singleton.SceneManager.LoadScene(
                "Bibingers",
                LoadSceneMode.Single
            );
        }
    }

    public void JoinGame()
    {
        NetworkManager.Singleton.StartClient();
    }
}