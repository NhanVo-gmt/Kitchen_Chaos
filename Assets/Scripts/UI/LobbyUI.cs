using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{
    [SerializeField] private Button mainMenuBtn;
    [SerializeField] private Button createLobbyBtn;
    [SerializeField] private Button joinLobbyBtn;

    private void Awake()
    {
        mainMenuBtn.onClick.AddListener(() =>
        {
            Loader.Load(Loader.Scene.MainMenuScene);
        });
        
        createLobbyBtn.onClick.AddListener(() =>
        {
            KitchenGameLobby.Instance.CreateLobby("LobbyName", false);
        });
        
        joinLobbyBtn.onClick.AddListener(() =>
        {
            KitchenGameLobby.Instance.QuickJoin();
        });
    }
}
