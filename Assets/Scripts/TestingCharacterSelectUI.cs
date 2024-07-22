using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TestingCharacterSelectUI : MonoBehaviour
{
    [SerializeField] private Button readyBtn;

    private void OnEnable()
    {
        readyBtn.onClick.AddListener(() =>
        {
            CharacterSelectReady.Instance.SetPlayerReady();
        });
    }
}
