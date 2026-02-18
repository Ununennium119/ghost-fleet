using System;
using Game.Audio;
using Game.Enum;
using UnityEngine;
using Logger = Common.Logger;

namespace Game.Manager {
    /// <summary>This class is responsible for managing game state.</summary>
    /// <remarks>This class is singleton.</remarks>
    public class GameManager : MonoBehaviour {
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


        /// <summary>The current phase of the game.</summary>
        public GamePhase CurrentGamePhase { get; private set; }


        [Header("Board")]
        [SerializeField, Tooltip("The player 1 board")]
        private Board board1;
        [SerializeField, Tooltip("The player 2 board")]
        private Board board2;


        private bool _hasLateStarted = false;
        private bool _isGamePaused = false;

        private MusicManager _musicManager;


        private void Awake() {
            Logger.LogInitializingInstance(this);
            if (Instance != null) {
                Logger.LogMultipleInstancesError(this);
                Destroy(gameObject);
                return;
            }
            Instance = this;
            Logger.LogInstanceInitialized(this);
        }

        private void Start() {
            _musicManager = MusicManager.Instance;

            Cell.OnAnyAttack += OnAnyAttackAction;
        }

        private void LateStart() {
            _hasLateStarted = true;
            ChangePhase(GamePhase.Start);
            NextPhase();
        }

        private void Update() {
            if (!_hasLateStarted) {
                LateStart();
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

        /// <summary>
        /// Changes the phase to the next phase.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">If the current phase is invalid.</exception>
        public void NextPhase() {
            if (CurrentGamePhase == GamePhase.GameOver) return;

            var newPhase = CurrentGamePhase switch {
                GamePhase.Attack1 => GamePhase.Attack2,
                GamePhase.Attack2 => GamePhase.Attack1,
                _ => CurrentGamePhase + 1
            };
            ChangePhase(newPhase);
        }


        private void GameOver() {
            _musicManager.PlayVictoryMusic();
            ChangePhase(GamePhase.GameOver);
        }

        private void ChangePhase(GamePhase gamePhase) {
            CurrentGamePhase = gamePhase;
            OnPhaseChanged?.Invoke(this, new OnPhaseChangedArgs { GamePhase = gamePhase });
        }


        private void OnAnyAttackAction(object sender, Cell.OnAnyAttackArgs e) {
            if (board1.IsDestroyed()) {
                OnWin?.Invoke(this, new OnWinArgs { Winner = Player.Player2 });
                GameOver();
            } else if (board2.IsDestroyed()) {
                OnWin?.Invoke(this, new OnWinArgs { Winner = Player.Player1 });
                GameOver();
            } else if (!e.IsDestroyed) {
                NextPhase();
            }
        }
    }
}
