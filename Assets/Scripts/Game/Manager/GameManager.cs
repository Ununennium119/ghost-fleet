using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Logger = Common.Logger;

namespace Game.Manager {
    /// <summary>This class is responsible for managing game state.</summary>
    /// <remarks>This class is singleton.</remarks>
    public class GameManager : MonoBehaviour {
        public static GameManager Instance { get; private set; }

        /// <summary>
        /// Represents different phases of the game.
        /// </summary>
        private enum Phase {
            Start,
            Placement1,
            Placement2,
            Attack1,
            Attack2,
            GameOver,
        }

        [Header("Development")]
        [SerializeField, Tooltip("The text showing the current phase")]
        private TextMeshProUGUI phaseText;
        [SerializeField, Tooltip("The next phase button")]
        private Button nextPhaseButton;
        [SerializeField, Tooltip("The game over button")]
        private Button gameOverButton;

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

        private Phase _currentPhase = Phase.Start;
        private Board _board1;
        private Board _board2;
        private Ship _selectedShip;

        private InputManager _inputManager;


        private void Awake() {
            Logger.LogInitializingInstance(this);
            if (Instance != null) {
                Logger.LogMultipleInstancesError(this);
                Destroy(gameObject);
                return;
            }
            Instance = this;
            Logger.LogInstanceInitialized(this);

            nextPhaseButton.onClick.AddListener(NextPhase);
            gameOverButton.onClick.AddListener(GameOver);
        }

        private void Start() {
            _inputManager = InputManager.Instance;

            Cell.OnAnyHover += MoveSelectedShipToCell;
            _inputManager.OnCancelPerformed += CancelShipSelection;

            foreach (var ship in ships1) {
                ship.gameObject.SetActive(false);
            }
            foreach (var ship in ships2) {
                ship.gameObject.SetActive(false);
            }

            UpdatePhaseText();
            CreateBoards();
            NextPhase();
        }

        private void Update() {
            switch (_currentPhase) {
                case Phase.Start:
                    nextPhaseButton.interactable = false;
                    break;
                case Phase.Placement1: {
                    nextPhaseButton.interactable = ships1.All(ship => ship.IsPlaced);
                    break;
                }
                case Phase.Placement2: {
                    nextPhaseButton.interactable = ships2.All(ship => ship.IsPlaced);
                    break;
                }
                default:
                    nextPhaseButton.interactable = true;
                    break;
            }
        }


        /// <summary>
        /// Selects the given ship and deselects the previous selected ship.
        /// </summary>
        /// <param name="ship">The ship to select.</param>
        /// <returns>True, if the ship is selected.</returns>
        public bool SelectShip(Ship ship) {
            if (_currentPhase is not Phase.Placement1 and not Phase.Placement2) {
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
            if (_currentPhase is not Phase.Placement1 and not Phase.Placement2) {
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
        /// Changes the phase to the next phase.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">If the current phase is invalid.</exception>
        private void NextPhase() {
            if (_currentPhase is not Phase.Placement1 and not Phase.Placement2) {
                foreach (var ship in ships1) {
                    ship.DisableSelection();
                }
                foreach (var ship in ships2) {
                    ship.DisableSelection();
                }
            }

            switch (_currentPhase) {
                case Phase.Start:
                    _currentPhase = Phase.Placement1;
                    foreach (var ship in ships1) {
                        ship.gameObject.SetActive(true);
                        ship.EnableSelection();
                    }
                    break;
                case Phase.Placement1: {
                    if (ships1.All(ship => ship.IsPlaced)) {
                        _currentPhase = Phase.Placement2;
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
                        _currentPhase = Phase.Attack1;
                        foreach (var ship in ships2) {
                            ship.gameObject.SetActive(false);
                        }
                    }
                    break;
                }
                case Phase.Attack1:
                    _currentPhase = Phase.Attack2;
                    break;
                case Phase.Attack2:
                    _currentPhase = Phase.Attack1;
                    break;
                case Phase.GameOver:
                    break;
                default: {
                    throw new ArgumentOutOfRangeException();
                }
            }

            UpdatePhaseText();
        }

        /// <summary>
        /// Changes the phase to game over phase.
        /// </summary>
        private void GameOver() {
            _currentPhase = Phase.GameOver;
            foreach (var ship in ships1) {
                ship.DisableSelection();
            }
            foreach (var ship in ships2) {
                ship.DisableSelection();
            }
            UpdatePhaseText();
        }

        /// <summary>
        /// Updates the text showing the current phase.
        /// </summary>
        private void UpdatePhaseText() {
            phaseText.text = _currentPhase.ToString();
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
            if (_currentPhase is not Phase.Placement1 and not Phase.Placement2) return;
            if (_currentPhase == Phase.Placement1 && cell.Board != _board1) return;
            if (_currentPhase == Phase.Placement2 && cell.Board != _board2) return;

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

            if (_currentPhase == Phase.Placement1) {
                var isPlaced = _selectedShip.Place(_board1);
                if (!isPlaced) return;
                _selectedShip.Deselect();
                _selectedShip.DisableSelection();
                _selectedShip = null;
            } else if (_currentPhase == Phase.Placement2) {
                var isPlaced = _selectedShip.Place(_board2);
                if (!isPlaced) return;
                _selectedShip.Deselect();
                _selectedShip.DisableSelection();
                _selectedShip = null;
            }
        }
    }
}
