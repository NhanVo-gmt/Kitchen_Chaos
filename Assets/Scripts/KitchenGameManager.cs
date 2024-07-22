using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class KitchenGameManager : NetworkBehaviour {


    public static KitchenGameManager Instance { get; private set; }

    public event EventHandler OnStateChanged;
    public event EventHandler OnLocalGamePaused;
    public event EventHandler OnLocalGameUnpaused;
    public event EventHandler OnMultiplayerGamePaused;
    public event EventHandler OnMultiplayerGameUnpaused;
    public event EventHandler OnLocalPlayerReadyChanged;


    private enum State {
        WaitingToStart,
        CountdownToStart,
        GamePlaying,
        GameOver,
    }

    [SerializeField] private Transform playerPrefab;


    private NetworkVariable<State>  state                 = new NetworkVariable<State>(State.WaitingToStart);
    private bool                    isLocalPlayerReady    = false;
    private NetworkVariable<float>  countdownToStartTimer = new NetworkVariable<float>(3f);
    private NetworkVariable<float>  gamePlayingTimer      = new NetworkVariable<float>(0f);
    private float                   gamePlayingTimerMax   = 90f;
    private bool                    isLocalGamePaused     = false;
    private NetworkVariable<bool>   isGamePaused          = new NetworkVariable<bool>(false);
    private Dictionary<ulong, bool> playerReadyDictionary;
    private Dictionary<ulong, bool> playerPauseDictionary;
    private bool                    autoTestGamePauseState = false;


    private void Awake() {
        Instance = this;

        playerReadyDictionary = new();
        playerPauseDictionary = new();
    }

    private void Start() {
        GameInput.Instance.OnPauseAction += GameInput_OnPauseAction;
        GameInput.Instance.OnInteractAction += GameInput_OnInteractAction;
    }

    public override void OnNetworkSpawn()
    {
        state.OnValueChanged += State_OnValueChanged;
        isGamePaused.OnValueChanged += IsGamePause_OnValueChanged;
        
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_OnClientDisconnectCallback;
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += SceneManager_OnLoadEventCompleted;
        }
    }
    private void SceneManager_OnLoadEventCompleted(string      scenename,        LoadSceneMode loadscenemode,
                                                   List<ulong> clientscompleted, List<ulong>   clientstimedout)
    {
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            Transform playerTransform = Instantiate(playerPrefab);
            playerTransform.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);
        }
    }


    private void NetworkManager_OnClientDisconnectCallback(ulong clientId)
    {
        autoTestGamePauseState = true;
    }
    
    private void IsGamePause_OnValueChanged(bool previousvalue, bool newvalue)
    {
        if (isGamePaused.Value)
        {
            Time.timeScale = 0f;
            
            OnMultiplayerGamePaused?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            Time.timeScale = 1f;
            
            OnMultiplayerGameUnpaused?.Invoke(this, EventArgs.Empty);
        }
    }

    public override void OnNetworkDespawn()
    {
        state.OnValueChanged -= State_OnValueChanged;
    }

    private void State_OnValueChanged(State previousvalue, State newvalue)
    {
        OnStateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void GameInput_OnInteractAction(object sender, EventArgs e) {
        if (state.Value == State.WaitingToStart)
        {
            isLocalPlayerReady = true;
            OnLocalPlayerReadyChanged?.Invoke(this, EventArgs.Empty);
            SetPlayerReadyServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetPlayerReadyServerRpc(ServerRpcParams serverRpcParams = default)
    {
        playerReadyDictionary[serverRpcParams.Receive.SenderClientId] = true;

        bool allClientReady = true;
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (!playerReadyDictionary.ContainsKey(clientId) || !playerReadyDictionary[clientId])
            {
                allClientReady = false;
                break;
            }
        }

        if (allClientReady)
        {
            state.Value = State.CountdownToStart;
        }
    }

    private void GameInput_OnPauseAction(object sender, EventArgs e) {
        TogglePauseGame();
    }

    private void Update()
    {
        if (!IsServer) return;
        
        switch (state.Value) {
            case State.WaitingToStart:
                break;
            case State.CountdownToStart:
                countdownToStartTimer.Value -= Time.deltaTime;
                if (countdownToStartTimer.Value < 0f) {
                    state.Value = State.GamePlaying;
                    gamePlayingTimer.Value = gamePlayingTimerMax;
                }
                break;
            case State.GamePlaying:
                gamePlayingTimer.Value -= Time.deltaTime;
                if (gamePlayingTimer.Value < 0f) {
                    state.Value = State.GameOver;
                }
                break;
            case State.GameOver:
                break;
        }
    }

    private void LateUpdate()
    {
        if (!IsServer) return;
        
        if (autoTestGamePauseState)
        {
            autoTestGamePauseState = false;
            TestGamePauseState();
        }
    }

    public bool IsGamePlaying() {
        return state.Value == State.GamePlaying;
    }

    public bool IsCountdownToStartActive() {
        return state.Value == State.CountdownToStart;
    }

    public float GetCountdownToStartTimer() {
        return countdownToStartTimer.Value;
    }
    
    public bool IsWaitingToStart() {
        return state.Value == State.WaitingToStart;
    }

    public bool IsGameOver() {
        return state.Value == State.GameOver;
    }

    public bool IsLocalPlayerReady()
    {
        return isLocalPlayerReady;
    }

    public float GetGamePlayingTimerNormalized() {
        return 1 - (gamePlayingTimer.Value / gamePlayingTimerMax);
    }

    public void TogglePauseGame() {
        isLocalGamePaused = !isLocalGamePaused;
        if (isLocalGamePaused) {
            PauseGameServerRpc();

            OnLocalGamePaused?.Invoke(this, EventArgs.Empty);
        } else {
            UnpauseServerRpc();

            OnLocalGameUnpaused?.Invoke(this, EventArgs.Empty);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void PauseGameServerRpc(ServerRpcParams serverRpcParams = default)
    {
        playerPauseDictionary[serverRpcParams.Receive.SenderClientId] = true;
        
        TestGamePauseState();
    }

    [ServerRpc(RequireOwnership = false)]
    void UnpauseServerRpc(ServerRpcParams serverRpcParams = default)
    {
        playerPauseDictionary[serverRpcParams.Receive.SenderClientId] = false;
        
        TestGamePauseState();
    }

    private void TestGamePauseState()
    {
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (playerPauseDictionary.ContainsKey(clientId) && playerPauseDictionary[clientId])
            {
                isGamePaused.Value = true;
                return;
            }
        }

        isGamePaused.Value = false;
    }
}