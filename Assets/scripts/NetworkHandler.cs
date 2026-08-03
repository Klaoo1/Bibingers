using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NetworkHandler : MonoBehaviour {
    public Button hostBtn;
    public Button clientBtn;
    void Start() {
        hostBtn.onClick.AddListener(() => NetworkManager.Singleton.StartHost());
        clientBtn.onClick.AddListener(() => NetworkManager.Singleton.StartClient());
    }
}