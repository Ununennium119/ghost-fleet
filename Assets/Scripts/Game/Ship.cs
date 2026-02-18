using System;
using System.Collections.Generic;
using Game.Enum;
using Game.Manager;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game {
    public class Ship : MonoBehaviour {
        private const float RAYCAST_MAX_DISTANCE = 50f;


        /// <summary>
        /// This event is triggered whenever selection of any ship changes.
        /// </summary>
        public static event EventHandler<OnAnySelectionChangedArgs> OnAnySelectionChanged;
        public class OnAnySelectionChangedArgs : EventArgs {
            public bool IsSelected;
            public Ship Ship;
        }


        /// <summary>
        /// This event is triggered whenever any ship is moved.
        /// </summary>
        public static event EventHandler OnAnyShipMoved;


        /// <summary>
        /// Resets the static objects.
        /// </summary>
        public static void ResetStaticObjects() {
            OnAnySelectionChanged = null;
            OnAnyShipMoved = null;
        }


        /// <summary>
        /// This event is triggered whenever hover of the ship changes.
        /// </summary>
        public event EventHandler<OnHoverChangedArgs> OnHoverChanged;
        public class OnHoverChangedArgs : EventArgs {
            public bool IsHovered;
        }

        /// <summary>
        /// This event is triggered whenever selection of the ship changes.
        /// </summary>
        public event EventHandler<OnSelectionChangedArgs> OnSelectionChanged;
        public class OnSelectionChangedArgs : EventArgs {
            public bool IsSelected;
        }


        [SerializeField, Tooltip("The board the ship is in")] [Required]
        private Board board;
        [SerializeField, Tooltip("The player owning the ship")]
        private Player player;

        [SerializeField, Tooltip("The size of the ship")]
        private int size;
        [SerializeField, Tooltip("The starting direction")]
        private Direction startingDirection;

        [SerializeField, Tooltip("The placeholder prefab used when placing the ship")] [Required]
        private GameObject placeholderPrefab;


        private bool _isSelectable;
        private bool _isHovered;
        private bool _isSelected;
        private GameObject _placeholderGameObject;

        private Direction _direction;
        private Vector2Int _boardPosition;
        private bool _isOnBoard;

        private Camera _mainCamera;
        private Collider _objectCollider;

        private InputManager _inputManager;
        private GameManager _gameManager;


        public int GetSize() => size;

        /// <returns>List of positions which this ship fills. Empty list if the ship is not on the board.</returns>
        /// <exception cref="ArgumentOutOfRangeException">If direction of the ship is invalid.</exception>
        public List<Vector2Int> GetPositions() {
            if (!_isOnBoard) return new List<Vector2Int>();

            var positions = new List<Vector2Int>();
            for (var i = 0; i < size; i++) {
                switch (_direction) {
                    case Direction.Up:
                        positions.Add(_boardPosition + new Vector2Int(0, i));
                        break;
                    case Direction.Down:
                        positions.Add(_boardPosition + new Vector2Int(0, -i));
                        break;
                    case Direction.Left:
                        positions.Add(_boardPosition + new Vector2Int(-i, 0));
                        break;
                    case Direction.Right:
                        positions.Add(_boardPosition + new Vector2Int(i, 0));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            return positions;
        }

        /// <returns>True if ship is on the board.</returns>
        public bool IsOnBoard() => _isOnBoard;


        private void Awake() {
            _mainCamera = Camera.main;
            _objectCollider = GetComponent<Collider>();

            _direction = startingDirection;
        }

        private void Start() {
            _inputManager = InputManager.Instance;
            _gameManager = GameManager.Instance;

            _inputManager.OnClickPerformed += OnClickPerformedAction;

            _gameManager.OnPhaseChanged += OnPhaseChangedAction;

            OnAnySelectionChanged += OnAnySelectionChangedAction;
        }

        private void Update() {
            if (!_isSelectable) return;

            var newIsHovered = false;
            var mousePosition = Mouse.current.position.ReadValue();
            var ray = _mainCamera.ScreenPointToRay(mousePosition);
            if (_objectCollider.Raycast(ray, out _, RAYCAST_MAX_DISTANCE)) {
                newIsHovered = true;
            }

            if (newIsHovered != _isHovered) {
                _isHovered = newIsHovered;
                OnHoverChanged?.Invoke(this, new OnHoverChangedArgs { IsHovered = _isHovered });
            }
        }


        private void OnClickPerformedAction(object sender, EventArgs e) {
            var newIsSelected = _isHovered;
            if (newIsSelected != _isSelected) {
                SetIsSelected(newIsSelected);
            }
        }

        private void OnPhaseChangedAction(object sender, GameManager.OnPhaseChangedArgs e) {
            _isSelectable = e.GamePhase switch {
                GamePhase.Start => false,
                GamePhase.Placement1 => player == Player.Player1,
                GamePhase.Placement2 => player == Player.Player2,
                GamePhase.Attack1 => false,
                GamePhase.Attack2 => false,
                GamePhase.GameOver => false,
                _ => throw new ArgumentOutOfRangeException()
            };

            var isActive = e.GamePhase switch {
                GamePhase.Start => false,
                GamePhase.Placement1 => player == Player.Player1,
                GamePhase.Placement2 => player == Player.Player2,
                GamePhase.Attack1 => false,
                GamePhase.Attack2 => false,
                GamePhase.GameOver => false,
                _ => throw new ArgumentOutOfRangeException()
            };
            gameObject.SetActive(isActive);
        }

        private void OnAnySelectionChangedAction(object sender, OnAnySelectionChangedArgs e) {
            if (_isSelected && e.IsSelected && e.Ship != this) {
                SetIsSelected(false);
            }
        }


        private void SetIsSelected(bool value) {
            _isSelected = value;
            OnSelectionChanged?.Invoke(this, new OnSelectionChangedArgs { IsSelected = _isSelected });
            OnAnySelectionChanged?.Invoke(
                this,
                new OnAnySelectionChangedArgs {
                    IsSelected = _isSelected,
                    Ship = this
                }
            );
            if (_isSelected) {
                _placeholderGameObject = Instantiate(
                    original: placeholderPrefab,
                    position: transform.position,
                    rotation: transform.rotation,
                    parent: null
                );
                var placeholder = _placeholderGameObject.GetComponent<PlaceholderShip>();
                placeholder.direction = _direction;
                placeholder.size = size;
            } else {
                var placeholder = _placeholderGameObject.GetComponent<PlaceholderShip>();
                if (placeholder.IsOnBoard) {
                    var isMoved = board.MoveShip(this, placeholder.BoardPosition, placeholder.direction);
                    if (isMoved) {
                        _boardPosition = placeholder.BoardPosition;
                        _direction = placeholder.direction;
                        _isOnBoard = true;
                        transform.position = _placeholderGameObject.transform.position;
                        transform.rotation = _placeholderGameObject.transform.rotation;
                        OnAnyShipMoved?.Invoke(this, EventArgs.Empty);
                    }
                }
                Destroy(_placeholderGameObject);
            }
        }
    }
}
