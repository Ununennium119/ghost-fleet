using System;
using Game.Enum;
using Game.Manager;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game {
    /// <summary>
    /// Represents a board cell.
    /// </summary>
    public class Cell : MonoBehaviour {
        private const float RAYCAST_MAX_DISTANCE = 50f;


        /// <summary>
        /// This event is triggered whenever any cell is hovered.
        /// </summary>
        public static event EventHandler OnAnyHover;

        /// <summary>
        /// This event is triggered whenever any cell is attacked.
        /// </summary>
        public static event EventHandler<OnAnyAttackArgs> OnAnyAttack;
        public class OnAnyAttackArgs : EventArgs {
            public bool IsDestroyed;
        }


        /// <summary>
        /// Resets the static objects.
        /// </summary>
        public static void ResetStaticObjects() {
            OnAnyHover = null;
            OnAnyAttack = null;
        }


        [SerializeField, Tooltip("the targeted game object visual")] [Required]
        private GameObject targetedVisual;
        [SerializeField, Tooltip("the attacked game object visual")] [Required]
        private GameObject attackedVisual;
        [SerializeField, Tooltip("the destroyed game object visual")] [Required]
        private GameObject destroyedVisual;


        /// <returns>True if there is a ship in the cell.</returns>
        public bool IsFilled { get; set; } = false;

        /// <returns>True if the cell is attacked.</returns>
        public bool IsAttacked { get; private set; } = false;


        private bool _isHovered;
        private bool _isSelectable;
        private bool _isTargetable;

        private Board _board;
        private Vector2Int _position;

        private Camera _mainCamera;
        private Collider _objectCollider;

        private InputManager _inputManager;
        private GameManager _gameManager;


        public void Initialize(Board board, Vector2Int position) {
            _board = board;
            _position = position;
        }

        public Vector2Int GetPosition() {
            return _position;
        }


        private void Awake() {
            _mainCamera = Camera.main;
            _objectCollider = GetComponent<Collider>();
        }

        private void Start() {
            _inputManager = InputManager.Instance;
            _gameManager = GameManager.Instance;

            _inputManager.OnClickPerformed += OnClickPerformedAction;

            _gameManager.OnPhaseChanged += OnPhaseChangedAction;

            // To fix the problem of transparent objects not being rendered on top of the ocean material
            GetComponent<Renderer>().material.renderQueue = 3100;
            targetedVisual.GetComponent<Renderer>().material.renderQueue = 3100;
            attackedVisual.GetComponent<Renderer>().material.renderQueue = 3100;
            destroyedVisual.GetComponent<Renderer>().material.renderQueue = 3100;
        }

        private void Update() {
            var mousePosition = Mouse.current.position.ReadValue();
            var ray = _mainCamera.ScreenPointToRay(mousePosition);
            _isHovered = _objectCollider.Raycast(ray, out _, RAYCAST_MAX_DISTANCE);

            if (_isHovered && _isSelectable) {
                OnAnyHover?.Invoke(this, EventArgs.Empty);
            }
            if (_isTargetable) {
                targetedVisual.SetActive(_isHovered);
            }
        }


        private void OnClickPerformedAction(object sender, EventArgs e) {
            if (!_isTargetable || !_isHovered) return;

            targetedVisual.SetActive(false);
            if (IsFilled) {
                destroyedVisual.SetActive(true);
            } else {
                attackedVisual.SetActive(true);
            }
            IsAttacked = true;
            OnAnyAttack?.Invoke(this, new OnAnyAttackArgs { IsDestroyed = IsFilled });
        }

        private void OnPhaseChangedAction(object sender, GameManager.OnPhaseChangedArgs e) {
            _isSelectable = e.GamePhase switch {
                GamePhase.Start => false,
                GamePhase.Placement1 => _board.GetPlayer() == Player.Player1,
                GamePhase.Placement2 => _board.GetPlayer() == Player.Player2,
                GamePhase.Attack1 => false,
                GamePhase.Attack2 => false,
                GamePhase.GameOver => false,
                _ => throw new ArgumentOutOfRangeException()
            };

            _isTargetable = e.GamePhase switch {
                GamePhase.Start => false,
                GamePhase.Placement1 => false,
                GamePhase.Placement2 => false,
                GamePhase.Attack1 => _board.GetPlayer() == Player.Player2 && !IsAttacked,
                GamePhase.Attack2 => _board.GetPlayer() == Player.Player1 && !IsAttacked,
                GamePhase.GameOver => false,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}
