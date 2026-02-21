using System;
using System.Collections.Generic;
using Common.Logic;
using Game.Enum;
using Game.Manager;
using MainMenu.Logic;
using NaughtyAttributes;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game {
    public class Ship : NetworkBehaviour {
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
        public static event EventHandler OnAnyShipPlaced;


        /// <summary>
        /// Resets the static objects.
        /// </summary>
        public static void ResetStaticObjects() {
            OnAnySelectionChanged = null;
            OnAnyShipPlaced = null;
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


        private readonly NetworkVariable<Direction> _directionNetwork = new();
        private readonly NetworkVariable<Vector2Int> _boardPositionNetwork = new();
        private readonly NetworkVariable<bool> _isOnBoardNetwork = new();

        private Direction _direction;
        private Vector2Int _boardPosition;
        private bool _isOnBoard = false;

        private bool _isSelectable;
        private bool _isHovered;
        private bool _isSelected;
        private GameObject _placeholderGameObject;

        private Camera _mainCamera;
        private Collider _objectCollider;

        private GameTypeManager _gameTypeManager;
        private MultiplayerManager _multiplayerManager;
        private InputManager _inputManager;
        private GameManager _gameManager;


        public Direction GetDirection() {
            return _gameTypeManager.GetGameType() == GameTypeManager.GameType.Online
                ? _directionNetwork.Value
                : _direction;
        }

        private void SetDirection(Direction value) {
            if (_gameTypeManager.GetGameType() == GameTypeManager.GameType.Online) {
                _directionNetwork.Value = value;
            } else {
                _direction = value;
            }
        }


        public Vector2Int GetBoardPosition() {
            return _gameTypeManager.GetGameType() == GameTypeManager.GameType.Online
                ? _boardPositionNetwork.Value
                : _boardPosition;
        }

        private void SetBoardPosition(Vector2Int value) {
            if (_gameTypeManager.GetGameType() == GameTypeManager.GameType.Online) {
                _boardPositionNetwork.Value = value;
            } else {
                _boardPosition = value;
            }
        }


        public bool IsOnBoard() {
            return _gameTypeManager.GetGameType() == GameTypeManager.GameType.Online
                ? _isOnBoardNetwork.Value
                : _isOnBoard;
        }

        private void SetOnBoard(bool value) {
            if (_gameTypeManager.GetGameType() == GameTypeManager.GameType.Online) {
                _isOnBoardNetwork.Value = value;
            } else {
                _isOnBoard = value;
                if (value) {
                    OnAnyShipPlaced?.Invoke(this, EventArgs.Empty);
                }
            }
        }


        public Player GetPlayer() => player;

        public int GetSize() => size;


        /// <returns>List of positions which this ship fills. Empty list if the ship is not on the board.</returns>
        /// <exception cref="ArgumentOutOfRangeException">If direction of the ship is invalid.</exception>
        public List<Vector2Int> GetPositions() {
            if (!IsOnBoard()) return new List<Vector2Int>();

            var positions = new List<Vector2Int>();
            for (var i = 0; i < size; i++) {
                switch (GetDirection()) {
                    case Direction.Up:
                        positions.Add(GetBoardPosition() + new Vector2Int(0, i));
                        break;
                    case Direction.Down:
                        positions.Add(GetBoardPosition() + new Vector2Int(0, -i));
                        break;
                    case Direction.Left:
                        positions.Add(GetBoardPosition() + new Vector2Int(-i, 0));
                        break;
                    case Direction.Right:
                        positions.Add(GetBoardPosition() + new Vector2Int(i, 0));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            return positions;
        }


        private void Awake() {
            _gameTypeManager = GameTypeManager.Instance;

            _mainCamera = Camera.main;
            _objectCollider = GetComponent<Collider>();

            if (_gameTypeManager.GetGameType() == GameTypeManager.GameType.Offline) {
                SetDirection(startingDirection);
            }
        }

        private void Start() {
            _inputManager = InputManager.Instance;
            _gameManager = GameManager.Instance;
            _multiplayerManager = MultiplayerManager.Instance;

            _inputManager.OnClickPerformed += OnClickPerformedAction;

            _gameManager.OnPhaseChanged += OnPhaseChangedAction;
            _gameManager.OnPlacementReady += OnPlacementReadyAction;

            OnAnySelectionChanged += OnAnySelectionChangedAction;
        }

        public override void OnNetworkSpawn() {
            if (IsServer) {
                SetDirection(startingDirection);
            }

            _isOnBoardNetwork.OnValueChanged += OnIsOnBoardValueChangedAction;
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
                GamePhase.Placement => _multiplayerManager.GetLocalPlayerData().Player == player,
                GamePhase.Attack1 => false,
                GamePhase.Attack2 => false,
                GamePhase.GameOver => false,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private void OnPlacementReadyAction(object sender, GameManager.OnPlacementReadyArgs e) {
            if (e.Player == _multiplayerManager.GetLocalPlayerData().Player) {
                _isSelectable = false;
            }
        }

        private void OnAnySelectionChangedAction(object sender, OnAnySelectionChangedArgs e) {
            if (_isSelected && e.IsSelected && e.Ship != this) {
                SetIsSelected(false);
            }
        }

        private void OnIsOnBoardValueChangedAction(bool previousValue, bool newValue) {
            if (!previousValue && newValue) {
                OnAnyShipPlaced?.Invoke(this, EventArgs.Empty);
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
                placeholder.direction = GetDirection();
                placeholder.size = size;
            } else {
                var placeholder = _placeholderGameObject.GetComponent<PlaceholderShip>();
                if (placeholder.IsOnBoard) {
                    if (_gameTypeManager.GetGameType() == GameTypeManager.GameType.Online) {
                        MoveShipRpc(
                            placeholder.BoardPosition,
                            placeholder.direction,
                            _placeholderGameObject.transform.position,
                            _placeholderGameObject.transform.rotation,
                            new RpcParams()
                        );
                    } else {
                        MoveShip(
                            placeholder.BoardPosition,
                            placeholder.direction,
                            _placeholderGameObject.transform.position,
                            _placeholderGameObject.transform.rotation
                        );
                    }
                }
                Destroy(_placeholderGameObject);
            }
        }

        private void MoveShip(Vector2Int boardPosition, Direction direction, Vector3 position, Quaternion rotation) {
            var isMoved = board.MoveShip(this, boardPosition, direction);
            if (isMoved) {
                SetBoardPosition(boardPosition);
                SetDirection(direction);
                SetOnBoard(true);
                transform.position = position;
                transform.rotation = rotation;
            }
        }


        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void MoveShipRpc(
            Vector2Int boardPosition,
            Direction direction,
            Vector3 position,
            Quaternion rotation,
            RpcParams rpcParams
        ) {
            var sender = _multiplayerManager.GetPlayerData(rpcParams.Receive.SenderClientId).Player;
            if (sender != player) return;

            MoveShip(boardPosition, direction, position, rotation);
        }
    }
}
