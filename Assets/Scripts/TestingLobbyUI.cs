using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TestingLobbyUI : MonoBehaviour
{
    [SerializeField] private Button createGameBtn;
    [SerializeField] private Button joinGameBtn;

    private void OnEnable()
    {
        createGameBtn.onClick.AddListener(() =>
        {
            KitchenGameMultiplayer.Instance.StartHost();
            Loader.LoadNetwork(Loader.Scene.CharacterCreateScene);
        });
        joinGameBtn.onClick.AddListener(() =>
        {
            KitchenGameMultiplayer.Instance.StartClient();
            // Automatically load scene
        });
    }
}
