using System;
using Common.Logic;
using Game.Enum;
using Game.Manager;
using NaughtyAttributes;
using UnityEngine;

namespace Game {
    public class ShipVisual : MonoBehaviour {
        [SerializeField, Tooltip("The related ship")] [Required]
        private Ship ship;

        [SerializeField, Tooltip("The visual for when the ship is selected")] [Required]
        private GameObject selectedGameObject;
        [SerializeField, Tooltip("The visual for when the ship is hovered")] [Required]
        private GameObject hoveredGameObject;

        
        private MultiplayerManager _multiplayerManager;
        private GameManager _gameManager;


        private void Awake() {
            ship.OnHoverChanged += OnHoverChangedAction;
            ship.OnSelectionChanged += OnSelectionChangedAction;
        }

        private void Start() {
            _gameManager = GameManager.Instance;
            _multiplayerManager = MultiplayerManager.Instance;

            _gameManager.OnPhaseChanged += OnPhaseChangedAction;
        }


        private void OnHoverChangedAction(object sender, Ship.OnHoverChangedArgs e) {
            hoveredGameObject.SetActive(e.IsHovered);
        }

        private void OnSelectionChangedAction(object sender, Ship.OnSelectionChangedArgs e) {
            selectedGameObject.SetActive(e.IsSelected);
        }

        private void OnPhaseChangedAction(object sender, GameManager.OnPhaseChangedArgs e) {
            var isActive = e.GamePhase switch {
                GamePhase.Start => false,
                GamePhase.Placement1 => ship.GetPlayer() == Player.Player1,
                GamePhase.Placement2 => ship.GetPlayer() == Player.Player2,
                GamePhase.Placement => _multiplayerManager.GetLocalPlayerData().Player == ship.GetPlayer(),
                GamePhase.Attack1 => false,
                GamePhase.Attack2 => false,
                GamePhase.GameOver => false,
                _ => throw new ArgumentOutOfRangeException()
            };
            gameObject.SetActive(isActive);
        }
    }
}
