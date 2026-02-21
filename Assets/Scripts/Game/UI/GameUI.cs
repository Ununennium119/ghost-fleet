using System;
using Common.Logic;
using Common.Utility;
using Game.Enum;
using Game.Manager;
using MainMenu.Logic;
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

        [SerializeField, Tooltip("The player 1 text")] [Required]
        private TextMeshProUGUI player1Text;
        [SerializeField, Tooltip("The player 2 text")] [Required]
        private TextMeshProUGUI player2Text;
        [SerializeField, Tooltip("The text the winner")] [Required]
        private TextMeshProUGUI winText;

        [SerializeField, Tooltip("The text showing the current phase")] [Required]
        private TextMeshProUGUI phaseText;
        [SerializeField, Tooltip("The next phase button")] [Required]
        private Button nextPhaseButton;


        private bool _isLocalPlacementReady = false;

        private GameTypeManager _gameTypeManager;
        private MultiplayerManager _multiplayerManager;
        private GameManager _gameManager;


        private void Awake() {
            _gameTypeManager = GameTypeManager.Instance;
            
            mainMenuButton.onClick.AddListener(() => SceneLoader.LoadScene(SceneLoader.Scene.MainMenuScene));
            optionsButton.onClick.AddListener(TogglePause);

            winText.gameObject.SetActive(false);

            nextPhaseButton.onClick.AddListener(NextPhase);
        }

        private void Start() {
            _multiplayerManager = MultiplayerManager.Instance;
            _gameManager = GameManager.Instance;

            _gameManager.OnPhaseChanged += OnPhaseChangedAction;
            _gameManager.OnWin += OnWinAction;
            _gameManager.OnPlacementReady += OnPlacementReadyAction;

            Ship.OnAnyShipPlaced += OnAnyShipPlacedAction;

            if (_gameTypeManager.GetGameType() == GameTypeManager.GameType.Online) {
                UpdatePlayerNames();
            }
        }


        private void OnPhaseChangedAction(object sender, GameManager.OnPhaseChangedArgs e) {
            phaseText.text = e.GamePhase switch {
                GamePhase.Start => "Start",
                GamePhase.Placement1 => "Player 1 Placement",
                GamePhase.Placement2 => "Player 2 Placement",
                GamePhase.Placement => "Placement",
                GamePhase.Attack1 => "Player 1 Attack",
                GamePhase.Attack2 => "Player 2 Attack",
                GamePhase.GameOver => "Game Over!",
                _ => throw new ArgumentOutOfRangeException()
            };
            nextPhaseButton.interactable = false;
        }

        private void OnAnyShipPlacedAction(object sender, EventArgs e) {
            // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
            nextPhaseButton.interactable = _gameManager.GetCurrentGamePhase() switch {
                GamePhase.Placement1 => _gameManager.GetPlayerBoard(Player.Player1).AreShipsOnBoard(),
                GamePhase.Placement2 => _gameManager.GetPlayerBoard(Player.Player2).AreShipsOnBoard(),
                GamePhase.Placement => !_isLocalPlacementReady && _gameManager
                    .GetPlayerBoard(_multiplayerManager.GetLocalPlayerData().Player).AreShipsOnBoard(),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private void OnWinAction(object sender, GameManager.OnWinArgs e) {
            if (_gameTypeManager.GetGameType() == GameTypeManager.GameType.Online) {
                winText.text = e.Winner switch {
                    Player.Player1 => $"{_multiplayerManager.GetPlayerData(index: 0).Name} Wins!",
                    Player.Player2 => $"{_multiplayerManager.GetPlayerData(index: 1).Name} Wins!",
                    _ => throw new ArgumentOutOfRangeException()
                };
            } else {
                winText.text = e.Winner switch {
                    Player.Player1 => "Player 1 Wins!",
                    Player.Player2 => "Player 2 Wins!",
                    _ => throw new ArgumentOutOfRangeException()
                };
            }
            winText.gameObject.SetActive(true);
        }

        private void OnPlacementReadyAction(object sender, GameManager.OnPlacementReadyArgs e) {
            if (e.Player == _multiplayerManager.GetLocalPlayerData().Player) {
                _isLocalPlacementReady = true;
                nextPhaseButton.interactable = false;
                phaseText.text = "Placement Done";
            }
        }


        private void UpdatePlayerNames() {
            player1Text.text = _multiplayerManager.GetPlayerData(index: 0).Name.ToString();
            player2Text.text = _multiplayerManager.GetPlayerData(index: 1).Name.ToString();
        }


        private void TogglePause() {
            _gameManager.TogglePause();
        }

        private void NextPhase() {
            _gameManager.LocalNextPhase();
        }
    }
}
