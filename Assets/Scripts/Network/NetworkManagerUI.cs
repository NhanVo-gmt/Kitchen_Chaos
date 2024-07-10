using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NetworkManagerUI : MonoBehaviour
{
    [SerializeField] private Button hostBtn;
    [SerializeField] private Button serverBtn;
    [SerializeField] private Button clientBtn;

    private void OnEnable()
    {
        hostBtn.onClick.AddListener(StartHost);
        serverBtn.onClick.AddListener(StartServer);
        clientBtn.onClick.AddListener(StartClient);
    }

    public void StartHost()
    {
        NetworkManager.Singleton.StartHost();
    }

    public void StartServer()
    {
        NetworkManager.Singleton.StartServer();
    }

    public void StartClient()
    {
        NetworkManager.Singleton.StartClient();
    }
}
