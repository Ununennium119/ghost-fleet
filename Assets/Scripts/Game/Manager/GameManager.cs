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
    /// <summary>This class is responsible for managing game state.</summary>
    /// <remarks>This class is singleton.</remarks>
    public class GameManager : NetworkBehaviour {
        public static GameManager Instance { get; private set; }


        /// <summary>
        /// This event is triggered whenever the player toggles the pause state.
        /// </summary>
        public event EventHandler<OnPauseToggledArgs> OnPauseToggled;
        public class OnPauseToggledArgs : EventArgs {
            public bool IsGamePaused;
        }

        /// <summary>
        /// This event is triggered whenever the game phase is changed.
        /// </summary>
        public event EventHandler<OnPhaseChangedArgs> OnPhaseChanged;
        public class OnPhaseChangedArgs : EventArgs {
            public GamePhase GamePhase;
        }

        /// <summary>
        /// This event is triggered whenever a player wins the game.
        /// </summary>
        public event EventHandler<OnWinArgs> OnWin;
        public class OnWinArgs : EventArgs {
            public Player Winner;
        }

        /// <summary>
        /// This event is triggered whenever the placement is ready for a player.
        /// </summary>
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
            return _gameTypeManager.GetGameType() == GameTypeManager.GameType.Online
                ? _currentGamePhaseNetwork.Value
                : _currentGamePhase;
        }

        private void SetCurrentGamePhase(GamePhase value) {
            if (_gameTypeManager.GetGameType() == GameTypeManager.GameType.Online) {
                _currentGamePhaseNetwork.Value = value;
            } else {
                _currentGamePhase = value;
                OnPhaseChanged?.Invoke(this, new OnPhaseChangedArgs { GamePhase = value });
            }
        }


        /// <param name="player">The player to return his board.</param>
        /// <returns>The player's board.</returns>
        /// <exception cref="ArgumentOutOfRangeException">If player is invalid.</exception>
        public Board GetPlayerBoard(Player player) {
            return player switch {
                Player.Player1 => board1,
                Player.Player2 => board2,
                _ => throw new ArgumentOutOfRangeException(nameof(player), player, null)
            };
        }

        /// <summary>
        /// Toggles game pause state.
        /// </summary>
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
            Logger.LogInitializingInstance(this);
            if (Instance != null) {
                Logger.LogMultipleInstancesError(this);
                Destroy(gameObject);
                return;
            }
            Instance = this;
            Logger.LogInstanceInitialized(this);

            _gameTypeManager = GameTypeManager.Instance;
        }

        private void Start() {
            _multiplayerManager = MultiplayerManager.Instance;
            _musicManager = MusicManager.Instance;

            Cell.OnAnyAttack += OnAnyAttackAction;

            if (IsServer) {
                NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnLoadEventCompletedAction;
            }
        }

        private void LateStart() {
            _isLateStarted = true;
            if (_gameTypeManager.GetGameType() == GameTypeManager.GameType.Offline) {
                SetCurrentGamePhase(GamePhase.Start);
                NextPhase();
            }
        }

        private void Update() {
            if (!_isLateStarted) {
                LateStart();
            }
        }

        public override void OnNetworkSpawn() {
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
                if (_gameTypeManager.GetGameType() == GameTypeManager.GameType.Offline || IsServer) {
                    SetCurrentGamePhase(GamePhase.GameOver);
                }
            } else if (board2.IsDestroyed()) {
                OnWin?.Invoke(this, new OnWinArgs { Winner = Player.Player1 });
                _musicManager.PlayVictoryMusic();
                if (_gameTypeManager.GetGameType() == GameTypeManager.GameType.Offline || IsServer) {
                    SetCurrentGamePhase(GamePhase.GameOver);
                }
            } else if (!e.IsDestroyed) {
                if (_gameTypeManager.GetGameType() == GameTypeManager.GameType.Offline || IsServer) {
                    NextPhase();
                }
            }
        }


        /// <summary>
        /// Changes the phase to the next phase.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">If the current phase is invalid.</exception>
        private void NextPhase() {
            if (GetCurrentGamePhase() == GamePhase.GameOver) return;

            var newPhase = GetCurrentGamePhase() switch {
                GamePhase.Start => _gameTypeManager.GetGameType() == GameTypeManager.GameType.Offline
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
