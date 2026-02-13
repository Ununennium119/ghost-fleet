using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Logger = Common.Logger;

namespace Game {
    /// <summary>This class is responsible for managing game state.</summary>
    /// <remarks>This class is singleton.</remarks>
    public class GameManager : MonoBehaviour {
        public static GameManager Instance { get; private set; }

        /// <summary>
        /// Represents different phases of the game.
        /// </summary>
        private enum Phase {
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
        [SerializeField, Tooltip("The size of the board")]
        private int boardSize;

        private Phase _currentPhase = Phase.Placement1;
        private Board _board1;
        private Board _board2;


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
            UpdatePhaseText();
            CreateBoards();
        }


        /// <returns>The size of the boards.</returns>
        public int GetBoardSize() => boardSize;


        /// <summary>
        /// Changes the phase to the next phase.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">If the current phase is invalid.</exception>
        private void NextPhase() {
            _currentPhase = _currentPhase switch {
                Phase.Placement1 => Phase.Placement2,
                Phase.Placement2 => Phase.Attack1,
                Phase.Attack1 => Phase.Attack2,
                Phase.Attack2 => Phase.Attack1,
                Phase.GameOver => Phase.GameOver,
                _ => throw new ArgumentOutOfRangeException()
            };
            UpdatePhaseText();
        }

        /// <summary>
        /// Changes the phase to game over phase.
        /// </summary>
        private void GameOver() {
            _currentPhase = Phase.GameOver;
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
    }
}
