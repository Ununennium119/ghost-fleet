using System;
using Common.Logic;
using Common.Utility;
using LobbyMenu.Logic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using WaitingMenu.Logic;

namespace WaitingMenu.UI {
    public class WaitingUI : NetworkBehaviour {
        [Header("Buttons")]
        [SerializeField, Tooltip("The main menu button")]
        private Button mainMenuButton;
        [SerializeField, Tooltip("The ready button")]
        private Button readyButton;

        [Header("Texts")]
        [SerializeField, Tooltip("The lobby name text")]
        private TextMeshProUGUI lobbyNameText;
        [SerializeField, Tooltip("The lobby code text")]
        private TextMeshProUGUI lobbyCodeText;
        [SerializeField, Tooltip("The name of the player 1")]
        private TextMeshProUGUI player1NameText;
        [SerializeField, Tooltip("The name of the player 2")]
        private TextMeshProUGUI player2NameText;


        private MultiplayerManager _multiplayerManager;
        private WaitingManager _waitingManager;
        private LobbyManager _lobbyManager;


        private void Start() {
            ResolveSingletons();
            SubscribeToEvents();
            AddButtonListeners();
            UpdateLobbyInfo();
            UpdatePlayerInfo();
        }

        public override void OnDestroy() {
            UnsubscribeFromEvents();
        }


        private void ResolveSingletons() {
            _multiplayerManager = MultiplayerManager.Instance;
            _waitingManager = WaitingManager.Instance;
            _lobbyManager = LobbyManager.Instance;
        }

        private void SubscribeToEvents() {
            _waitingManager.OnReadyChanged += OnReadyChangedAction;
            _multiplayerManager.OnPlayerDataListChanged += OnPlayerDataListChangedAction;
        }

        private void UnsubscribeFromEvents() {
            _multiplayerManager.OnPlayerDataListChanged -= OnPlayerDataListChangedAction;
        }

        private void AddButtonListeners() {
            mainMenuButton.onClick.AddListener(() => {
                _lobbyManager.LeaveLobby();
                NetworkManager.Singleton.Shutdown();
                SceneLoader.LoadScene(SceneLoader.Scene.MainMenuScene);
            });
            readyButton.onClick.AddListener(() => { _waitingManager.SetPlayerReady(); });
        }
        
        private void UpdateLobbyInfo() {
            var lobby = _lobbyManager.GetJoinedLobby();
            lobbyNameText.text = lobby.Name;
            lobbyCodeText.text = $"Code: {lobby.LobbyCode}";
        }


        private void OnReadyChangedAction(object sender, EventArgs e) {
            UpdatePlayerInfo();
        }

        private void OnPlayerDataListChangedAction(object sender, EventArgs e) {
            UpdatePlayerInfo();
        }


        private void UpdatePlayerInfo() {
            if (_multiplayerManager.HasPlayerData(0)) {
                var playerData = _multiplayerManager.GetPlayerData(0);
                player1NameText.text =
                    $"{playerData.Name}{(_waitingManager.IsPlayerReady(playerData.ClientId) ? " (Ready)" : "")}";
            } else {
                player1NameText.text = "";
            }
            if (_multiplayerManager.HasPlayerData(1)) {
                var playerData = _multiplayerManager.GetPlayerData(1);
                player2NameText.text =
                    $"{playerData.Name}{(_waitingManager.IsPlayerReady(playerData.ClientId) ? " (Ready)" : "")}";
                readyButton.interactable = true;
            } else {
                player2NameText.text = "";
                readyButton.interactable = false;
            }
        }
    }
}
