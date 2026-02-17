using System;
using System.Linq;
using Game.Audio;
using UnityEngine;
using Logger = Common.Logger;

namespace Game.Manager {
    /// <summary>This class is responsible for managing game state.</summary>
    /// <remarks>This class is singleton.</remarks>
    public class GameManager : MonoBehaviour {
        public static GameManager Instance { get; private set; }

        /// <summary>
        /// Represents different phases of the game.
        /// </summary>
        public enum Phase {
            Start,
            Placement1,
            Placement2,
            Attack1,
            Attack2,
            GameOver,
        }

        public enum Player {
            Player1,
            Player2,
        }


        public event EventHandler OnAttack;

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
            public Phase Phase;
        }

        /// <summary>
        /// This event is triggered whenever a ship is placed.
        /// </summary>
        public event EventHandler OnShipPlaced;

        /// <summary>
        /// This event is triggered whenever the game phase is changed.
        /// </summary>
        public event EventHandler<OnWinArgs> OnWin;
        public class OnWinArgs : EventArgs {
            public Player Winner;
        }


        public Phase CurrentPhase { get; private set; } = Phase.Start;


        [Header("Board")]
        [SerializeField, Tooltip("The game objects containing the boards")]
        private GameObject boardsGameObject;
        [SerializeField, Tooltip("The board prefab")]
        private GameObject boardPrefab;

        [Header("Ships")]
        [SerializeField, Tooltip("The player 1 ships")]
        private Ship[] ships1;
        [SerializeField, Tooltip("The player 2 ships")]
        private Ship[] ships2;

        private Board _board1;
        private Board _board2;
        private Ship _selectedShip;

        private bool _hasLateStarted = false;
        private bool _isGamePaused = false;

        private InputManager _inputManager;
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
            _inputManager = InputManager.Instance;
            _musicManager = MusicManager.Instance;

            Cell.OnAnyHover += MoveSelectedShipToCell;
            _inputManager.OnCancelPerformed += CancelShipSelection;

            foreach (var ship in ships1) {
                ship.gameObject.SetActive(false);
            }
            foreach (var ship in ships2) {
                ship.gameObject.SetActive(false);
            }

            CreateBoards();
        }

        private void Update() {
            if (!_hasLateStarted) {
                _hasLateStarted = true;
                NextPhase();
            }
        }


        /// <summary>
        /// Toggles game pause state.
        /// </summary>
        public void TogglePause() {
            _isGamePaused = !_isGamePaused;
            OnPauseToggled?.Invoke(this, new OnPauseToggledArgs { IsGamePaused = _isGamePaused });
        }

        /// <summary>
        /// Selects the given ship and deselects the previous selected ship.
        /// </summary>
        /// <param name="ship">The ship to select.</param>
        /// <returns>True, if the ship is selected.</returns>
        public bool SelectShip(Ship ship) {
            if (CurrentPhase is not Phase.Placement1 and not Phase.Placement2) {
                return false;
            }

            _selectedShip?.Deselect();
            _selectedShip = ship;
            _selectedShip.Select();
            return true;
        }

        /// <summary>
        /// Deselects the given ship if it's selected.
        /// </summary>
        /// <param name="ship"></param>
        /// <returns></returns>
        public bool DeselectShip(Ship ship) {
            if (CurrentPhase is not Phase.Placement1 and not Phase.Placement2) {
                return false;
            }
            if (_selectedShip != ship) {
                return false;
            }

            if (_selectedShip.IsPositionSet) {
                PlaceSelectedShip();
            } else {
                _selectedShip.Deselect();
                _selectedShip = null;
            }
            return true;
        }

        /// <summary>
        /// Attacks the cell in the given position in a board based on current phase.
        /// </summary>
        /// <param name="position">The position of the cell to attack</param>
        public void AttackCell(Vector2Int position) {
            if (CurrentPhase is not Phase.Attack1 and not Phase.Attack2) return;

            OnAttack?.Invoke(this, EventArgs.Empty);
            var isDestroyed = false;
            if (CurrentPhase == Phase.Attack1) {
                isDestroyed = _board2.AttackCell(position);
                if (_board2.IsDestroyed()) {
                    OnWin?.Invoke(this, new OnWinArgs { Winner = Player.Player1 });
                    GameOver();
                }
            }
            if (CurrentPhase == Phase.Attack2) {
                isDestroyed = _board1.AttackCell(position);
                if (_board1.IsDestroyed()) {
                    OnWin?.Invoke(this, new OnWinArgs { Winner = Player.Player2 });
                    GameOver();
                }
            }
            if (!isDestroyed) {
                NextPhase();
            }
        }

        public bool AreShips1Placed() {
            return ships1.All(ship => ship.IsPlaced);
        }

        public bool AreShips2Placed() {
            return ships2.All(ship => ship.IsPlaced);
        }


        /// <summary>
        /// Changes the phase to the next phase.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">If the current phase is invalid.</exception>
        public void NextPhase() {
            if (CurrentPhase is not Phase.Placement1 and not Phase.Placement2) {
                foreach (var ship in ships1) {
                    ship.DisableSelection();
                }
                foreach (var ship in ships2) {
                    ship.DisableSelection();
                }
            }

            switch (CurrentPhase) {
                case Phase.Start:
                    ChangePhase(Phase.Placement1);
                    foreach (var ship in ships1) {
                        ship.gameObject.SetActive(true);
                        ship.EnableSelection();
                    }
                    break;
                case Phase.Placement1: {
                    if (ships1.All(ship => ship.IsPlaced)) {
                        ChangePhase(Phase.Placement2);
                        foreach (var ship in ships1) {
                            ship.gameObject.SetActive(false);
                        }
                        foreach (var ship in ships2) {
                            ship.gameObject.SetActive(true);
                            ship.EnableSelection();
                        }
                    }
                    break;
                }
                case Phase.Placement2: {
                    if (ships2.All(ship => ship.IsPlaced)) {
                        _board1.SetTargetable(false);
                        _board2.SetTargetable(true);
                        ChangePhase(Phase.Attack1);
                        foreach (var ship in ships2) {
                            ship.gameObject.SetActive(false);
                        }
                    }
                    break;
                }
                case Phase.Attack1:
                    _board1.SetTargetable(true);
                    _board2.SetTargetable(false);
                    ChangePhase(Phase.Attack2);
                    break;
                case Phase.Attack2:
                    _board1.SetTargetable(false);
                    _board2.SetTargetable(true);
                    ChangePhase(Phase.Attack1);
                    break;
                case Phase.GameOver:
                    _board1.SetTargetable(false);
                    _board2.SetTargetable(false);
                    break;
                default: {
                    throw new ArgumentOutOfRangeException();
                }
            }
        }

        /// <summary>
        /// Changes the phase to game over phase.
        /// </summary>
        private void GameOver() {
            _musicManager.PlayVictoryMusic();
            ChangePhase(Phase.GameOver);
            foreach (var ship in ships1) {
                ship.DisableSelection();
            }
            foreach (var ship in ships2) {
                ship.DisableSelection();
            }
        }

        /// <summary>
        /// Creates player 1 and 2 boards.
        /// </summary>
        private void CreateBoards() {
            var board1GameObject = Instantiate(
                original: boardPrefab,
                position: new Vector3(-1, 0, 0),
                rotation: Quaternion.identity,
                parent: boardsGameObject.transform
            );
            _board1 = board1GameObject.GetComponent<Board>();
            _board1.Initialize(growLeft: true);

            var board2GameObject = Instantiate(
                original: boardPrefab,
                position: new Vector3(1, 0, 0),
                rotation: Quaternion.identity,
                parent: boardsGameObject.transform
            );
            _board2 = board2GameObject.GetComponent<Board>();
            _board2.Initialize(growLeft: false);
        }

        /// <remarks>
        /// Invoked when the <see cref="Cell.OnAnyHover"/> event is triggered.
        /// </remarks>
        private void MoveSelectedShipToCell(object sender, EventArgs e) {
            var cell = sender as Cell;
            if (!cell) return;
            if (!_selectedShip) return;
            if (CurrentPhase is not Phase.Placement1 and not Phase.Placement2) return;
            if (CurrentPhase == Phase.Placement1 && cell.Board != _board1) return;
            if (CurrentPhase == Phase.Placement2 && cell.Board != _board2) return;

            _selectedShip.SetPosition(cell.Position, cell.transform.position);
        }

        /// <remarks>
        /// Invoked when the <see cref="_inputManager.OnCancelPerformed"/> event is triggered.
        /// </remarks>
        private void CancelShipSelection(object sender, EventArgs e) {
            _selectedShip = null;
        }

        private void PlaceSelectedShip() {
            if (!_selectedShip) return;

            if (CurrentPhase == Phase.Placement1) {
                var isPlaced = _selectedShip.Place(_board1);
                if (!isPlaced) return;

                _selectedShip.Deselect();
                _selectedShip.DisableSelection();
                _selectedShip = null;
                OnShipPlaced?.Invoke(this, EventArgs.Empty);
            } else if (CurrentPhase == Phase.Placement2) {
                var isPlaced = _selectedShip.Place(_board2);
                if (!isPlaced) return;

                _selectedShip.Deselect();
                _selectedShip.DisableSelection();
                _selectedShip = null;
                OnShipPlaced?.Invoke(this, EventArgs.Empty);
            }
        }

        private void ChangePhase(Phase phase) {
            CurrentPhase = phase;
            OnPhaseChanged?.Invoke(this, new OnPhaseChangedArgs { Phase = phase });
        }
    }
}
