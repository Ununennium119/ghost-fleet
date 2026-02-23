using System;
using System.Collections.Generic;
using Common.Logic;
using Game.Audio;
using Game.Enum;
using MainMenu.Logic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using Logger = Common.Utility.Logger;

namespace Game.Manager {
    /// <remarks>This class is singleton.</remarks>
    public class GameManager : NetworkBehaviour {
        public static GameManager Instance { get; private set; }


        public event EventHandler<OnPauseToggledArgs> OnPauseToggled;
        public class OnPauseToggledArgs : EventArgs {
            public bool IsGamePaused;
        }

        public event EventHandler<OnPhaseChangedArgs> OnPhaseChanged;
        public class OnPhaseChangedArgs : EventArgs {
            public GamePhase GamePhase;
        }

        public event EventHandler<OnWinArgs> OnWin;
        public class OnWinArgs : EventArgs {
            public Player Winner;
        }

        public event EventHandler<OnPlacementReadyArgs> OnPlacementReady;
        public class OnPlacementReadyArgs : EventArgs {
            public Player Player;
        }


        [Header("Board")]
        [SerializeField, Tooltip("The player 1 board")]
        private Board board1;
        [SerializeField, Tooltip("The player 2 board")]
        private Board board2;


        private readonly NetworkVariable<GamePhase> _currentGamePhaseNetwork = new();
        private readonly NetworkList<int> _placementReadyPlayers = new();

        private GamePhase _currentGamePhase;

        private bool _isGamePaused = false;
        private bool _isLateStarted = false;

        private GameTypeManager _gameTypeManager;
        private MultiplayerManager _multiplayerManager;
        private MusicManager _musicManager;


        public GamePhase GetCurrentGamePhase() {
            return _gameTypeManager.IsOnline()
                ? _currentGamePhaseNetwork.Value
                : _currentGamePhase;
        }

        private void SetCurrentGamePhase(GamePhase value) {
            if (_gameTypeManager.IsOnline()) {
                _currentGamePhaseNetwork.Value = value;
            } else {
                _currentGamePhase = value;
                OnPhaseChanged?.Invoke(this, new OnPhaseChangedArgs { GamePhase = value });
            }
        }


        public Board GetPlayerBoard(Player player) {
            return player switch {
                Player.Player1 => board1,
                Player.Player2 => board2,
                _ => throw new ArgumentOutOfRangeException(nameof(player), player, null)
            };
        }

        public void TogglePause() {
            _isGamePaused = !_isGamePaused;
            OnPauseToggled?.Invoke(this, new OnPauseToggledArgs { IsGamePaused = _isGamePaused });
        }

        public void LocalNextPhase() {
            if (GetCurrentGamePhase() == GamePhase.Placement) {
                SetPlacementReadyServerRpc(new RpcParams());
            } else if (GetCurrentGamePhase() == GamePhase.Placement1 || GetCurrentGamePhase() == GamePhase.Placement2) {
                NextPhase();
            }
        }


        private void Awake() {
            InitializeSingleton();
            ResolveSingletonsAwake();
        }

        private void Start() {
            ResolveSingletonsStart();
            SubscribeToEvents();
        }

        public override void OnNetworkSpawn() {
            SubscribeToNetworkEvents();
        }

        private void LateStart() {
            _isLateStarted = true;
            if (_gameTypeManager.IsOffline()) {
                SetCurrentGamePhase(GamePhase.Start);
                NextPhase();
            }
        }

        private void Update() {
            if (!_isLateStarted) {
                LateStart();
            }
        }


        private void InitializeSingleton() {
            Logger.LogInitializingInstance(this);
            if (Instance != null) {
                Logger.LogMultipleInstancesError(this);
                Destroy(gameObject);
                return;
            }
            Instance = this;
            Logger.LogInstanceInitialized(this);
        }

        private void ResolveSingletonsAwake() {
            _gameTypeManager = GameTypeManager.Instance;
        }

        private void ResolveSingletonsStart() {
            _multiplayerManager = MultiplayerManager.Instance;
            _musicManager = MusicManager.Instance;
        }

        private void SubscribeToEvents() {
            Cell.OnAnyAttack += OnAnyAttackAction;

            if (_gameTypeManager.IsOnline() && IsServer) {
                NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnLoadEventCompletedAction;
            }
        }

        private void SubscribeToNetworkEvents() {
            _currentGamePhaseNetwork.OnValueChanged += OnCurrentGamePhaseValueChanged;
            _placementReadyPlayers.OnListChanged += OnPlacementReadyPlayersListChangedAction;
        }


        private void OnLoadEventCompletedAction(
            string sceneName,
            LoadSceneMode loadSceneMode,
            List<ulong> clientsCompleted,
            List<ulong> clientsTimedOut
        ) {
            SetCurrentGamePhase(GamePhase.Start);
            NextPhase();
        }

        private void OnCurrentGamePhaseValueChanged(GamePhase previousValue, GamePhase newValue) {
            OnPhaseChanged?.Invoke(this, new OnPhaseChangedArgs { GamePhase = newValue });
        }

        private void OnPlacementReadyPlayersListChangedAction(NetworkListEvent<int> changeEvent) {
            if (changeEvent.Type == NetworkListEvent<int>.EventType.Add) {
                OnPlacementReady?.Invoke(this, new OnPlacementReadyArgs { Player = (Player)changeEvent.Value });
            }
        }

        private void OnAnyAttackAction(object sender, Cell.OnAnyAttackArgs e) {
            if (board1.IsDestroyed()) {
                OnWin?.Invoke(this, new OnWinArgs { Winner = Player.Player2 });
                _musicManager.PlayVictoryMusic();
                if (_gameTypeManager.IsOffline() || IsServer) {
                    SetCurrentGamePhase(GamePhase.GameOver);
                }
            } else if (board2.IsDestroyed()) {
                OnWin?.Invoke(this, new OnWinArgs { Winner = Player.Player1 });
                _musicManager.PlayVictoryMusic();
                if (_gameTypeManager.IsOffline() || IsServer) {
                    SetCurrentGamePhase(GamePhase.GameOver);
                }
            } else if (!e.IsDestroyed) {
                if (_gameTypeManager.IsOffline() || IsServer) {
                    NextPhase();
                }
            }
        }


        private void NextPhase() {
            if (GetCurrentGamePhase() == GamePhase.GameOver) return;

            var newPhase = GetCurrentGamePhase() switch {
                GamePhase.Start => _gameTypeManager.IsOffline()
                    ? GamePhase.Placement1
                    : GamePhase.Placement,
                GamePhase.Placement => GamePhase.Attack1,
                GamePhase.Attack1 => GamePhase.Attack2,
                GamePhase.Attack2 => GamePhase.Attack1,
                _ => GetCurrentGamePhase() + 1
            };
            SetCurrentGamePhase(newPhase);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void SetPlacementReadyServerRpc(RpcParams rpcParams) {
            if (GetCurrentGamePhase() != GamePhase.Placement) return;

            var playerInt = (int)_multiplayerManager.GetPlayerData(rpcParams.Receive.SenderClientId).Player;
            _placementReadyPlayers.Add(playerInt);
            if (
                _placementReadyPlayers.Contains((int)Player.Player1) &&
                _placementReadyPlayers.Contains((int)Player.Player2)
            ) {
                NextPhase();
            }
        }
    }
}
