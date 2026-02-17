using System;
using Common;
using Game.Manager;
using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI {
    public class GameUI : MonoBehaviour {
        [SerializeField, Tooltip("The main menu button")] [Required]
        private Button mainMenuButton;
        [SerializeField, Tooltip("The options button")] [Required]
        private Button optionsButton;

        [SerializeField, Tooltip("The text the winner")] [Required]
        private TextMeshProUGUI winText;

        [SerializeField, Tooltip("The text showing the current phase")] [Required]
        private TextMeshProUGUI phaseText;
        [SerializeField, Tooltip("The next phase button")] [Required]
        private Button nextPhaseButton;


        private GameManager _gameManager;


        private void Awake() {
            mainMenuButton.onClick.AddListener(() => SceneLoader.LoadScene(SceneLoader.Scene.MainMenuScene));
            optionsButton.onClick.AddListener(TogglePause);

            winText.gameObject.SetActive(false);

            nextPhaseButton.onClick.AddListener(NextPhase);
        }

        private void Start() {
            _gameManager = GameManager.Instance;

            _gameManager.OnPhaseChanged += OnPhaseChangedAction;
            _gameManager.OnShipPlaced += OnShipPlacedAction;
            _gameManager.OnWin += OnWinAction;
        }


        private void TogglePause() {
            _gameManager.TogglePause();
        }

        private void NextPhase() {
            _gameManager.NextPhase();
        }


        private void OnPhaseChangedAction(object sender, GameManager.OnPhaseChangedArgs e) {
            phaseText.text = e.Phase switch {
                GameManager.Phase.Start => "Start",
                GameManager.Phase.Placement1 => "Player 1 Placement",
                GameManager.Phase.Placement2 => "Player 2 Placement",
                GameManager.Phase.Attack1 => "Player 1 Attack",
                GameManager.Phase.Attack2 => "Player 2 Attack",
                GameManager.Phase.GameOver => "Game Over!",
                _ => throw new ArgumentOutOfRangeException()
            };
            nextPhaseButton.interactable = _gameManager.CurrentPhase switch {
                GameManager.Phase.Start => false,
                GameManager.Phase.Placement1 => false,
                GameManager.Phase.Placement2 => false,
                GameManager.Phase.GameOver => false,
                _ => true
            };
        }

        private void OnShipPlacedAction(object sender, EventArgs e) {
            // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
            nextPhaseButton.interactable = _gameManager.CurrentPhase switch {
                GameManager.Phase.Placement1 => _gameManager.AreShips1Placed(),
                GameManager.Phase.Placement2 => _gameManager.AreShips2Placed(),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private void OnWinAction(object sender, GameManager.OnWinArgs e) {
            winText.text = e.Winner switch {
                GameManager.Player.Player1 => "Player 1 Wins!",
                GameManager.Player.Player2 => "Player 2 Wins!",
                _ => throw new ArgumentOutOfRangeException()
            };
            winText.gameObject.SetActive(true);
        }
    }
}
