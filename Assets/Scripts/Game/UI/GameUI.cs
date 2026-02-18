using System;
using Common;
using Game.Enum;
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
            _gameManager.OnWin += OnWinAction;

            Ship.OnAnyShipMoved += OnAnyShipMovedAction;
        }


        private void TogglePause() {
            _gameManager.TogglePause();
        }

        private void NextPhase() {
            _gameManager.NextPhase();
        }


        private void OnPhaseChangedAction(object sender, GameManager.OnPhaseChangedArgs e) {
            phaseText.text = e.GamePhase switch {
                GamePhase.Start => "Start",
                GamePhase.Placement1 => "Player 1 Placement",
                GamePhase.Placement2 => "Player 2 Placement",
                GamePhase.Attack1 => "Player 1 Attack",
                GamePhase.Attack2 => "Player 2 Attack",
                GamePhase.GameOver => "Game Over!",
                _ => throw new ArgumentOutOfRangeException()
            };
            nextPhaseButton.interactable = _gameManager.CurrentGamePhase switch {
                GamePhase.Start => false,
                GamePhase.Placement1 => false,
                GamePhase.Placement2 => false,
                GamePhase.GameOver => false,
                _ => true
            };
        }

        private void OnAnyShipMovedAction(object sender, EventArgs e) {
            // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
            nextPhaseButton.interactable = _gameManager.CurrentGamePhase switch {
                GamePhase.Placement1 => _gameManager.GetPlayerBoard(Player.Player1).AreShipsOnBoard(),
                GamePhase.Placement2 => _gameManager.GetPlayerBoard(Player.Player2).AreShipsOnBoard(),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private void OnWinAction(object sender, GameManager.OnWinArgs e) {
            winText.text = e.Winner switch {
                Player.Player1 => "Player 1 Wins!",
                Player.Player2 => "Player 2 Wins!",
                _ => throw new ArgumentOutOfRangeException()
            };
            winText.gameObject.SetActive(true);
        }
    }
}
