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


        public static event EventHandler<OnAnySelectionChangedArgs> OnAnySelectionChanged;
        public class OnAnySelectionChangedArgs : EventArgs {
            public bool IsSelected;
            public Ship Ship;
        }

        public static event EventHandler OnAnyShipPlaced;


        public static void ResetStaticObjects() {
            OnAnySelectionChanged = null;
            OnAnyShipPlaced = null;
        }


        public event EventHandler<OnHoverChangedArgs> OnHoverChanged;
        public class OnHoverChangedArgs : EventArgs {
            public bool IsHovered;
        }

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
        private readonly NetworkVariable<Vector2Int> _coordinateNetwork = new();
        private readonly NetworkVariable<bool> _isOnBoardNetwork = new();

        private Direction _direction;
        private Vector2Int _coordinate;
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
            return _gameTypeManager.IsOnline()
                ? _directionNetwork.Value
                : _direction;
        }

        private void SetDirection(Direction value) {
            if (_gameTypeManager.IsOnline()) {
                _directionNetwork.Value = value;
            } else {
                _direction = value;
            }
        }


        public Vector2Int GetCoordinate() {
            return _gameTypeManager.IsOnline()
                ? _coordinateNetwork.Value
                : _coordinate;
        }

        private void SetCoordinate(Vector2Int value) {
            if (_gameTypeManager.IsOnline()) {
                _coordinateNetwork.Value = value;
            } else {
                _coordinate = value;
            }
        }


        public bool IsOnBoard() {
            return _gameTypeManager.IsOnline()
                ? _isOnBoardNetwork.Value
                : _isOnBoard;
        }

        private void SetOnBoard(bool value) {
            if (_gameTypeManager.IsOnline()) {
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


        /// <returns>List of coordintes which this ship fills. Empty list if the ship is not on the board.</returns>
        /// <exception cref="ArgumentOutOfRangeException">If direction of the ship is invalid.</exception>
        public List<Vector2Int> GetCoordinates() {
            if (!IsOnBoard()) return new List<Vector2Int>();

            var coordinates = new List<Vector2Int>();
            for (var i = 0; i < size; i++) {
                switch (GetDirection()) {
                    case Direction.Up:
                        coordinates.Add(GetCoordinate() + new Vector2Int(0, i));
                        break;
                    case Direction.Down:
                        coordinates.Add(GetCoordinate() + new Vector2Int(0, -i));
                        break;
                    case Direction.Left:
                        coordinates.Add(GetCoordinate() + new Vector2Int(-i, 0));
                        break;
                    case Direction.Right:
                        coordinates.Add(GetCoordinate() + new Vector2Int(i, 0));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            return coordinates;
        }


        private void Awake() {
            ResolveSingletonsAwake();
            CacheReferences();

            if (_gameTypeManager.IsOffline()) {
                SetDirection(startingDirection);
            }
        }

        private void Start() {
            ResolveSingletonsStart();
            SubscribeToEvents();
        }

        public override void OnNetworkSpawn() {
            if (_gameTypeManager.IsOnline() && IsServer) {
                SetDirection(startingDirection);
            }

            _isOnBoardNetwork.OnValueChanged += OnIsOnBoardValueChangedAction;
        }

        private void Update() {
            HandleHover();
        }


        private void ResolveSingletonsAwake() {
            _gameTypeManager = GameTypeManager.Instance;
        }

        private void CacheReferences() {
            _mainCamera = Camera.main;
            _objectCollider = GetComponent<Collider>();
        }

        private void ResolveSingletonsStart() {
            _inputManager = InputManager.Instance;
            _gameManager = GameManager.Instance;
            _multiplayerManager = MultiplayerManager.Instance;
        }

        private void SubscribeToEvents() {
            _inputManager.OnClickPerformed += OnClickPerformedAction;

            _gameManager.OnPhaseChanged += OnPhaseChangedAction;
            _gameManager.OnPlacementReady += OnPlacementReadyAction;

            OnAnySelectionChanged += OnAnySelectionChangedAction;
        }

        private void HandleHover() {
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
                InstantiatePlaceholder();
            } else {
                MoveToPlaceholder();
                Destroy(_placeholderGameObject);
            }
        }

        private void InstantiatePlaceholder() {
            _placeholderGameObject = Instantiate(
                original: placeholderPrefab,
                position: transform.position,
                rotation: transform.rotation,
                parent: null
            );
            var placeholder = _placeholderGameObject.GetComponent<PlaceholderShip>();
            placeholder.direction = GetDirection();
            placeholder.size = size;
        }

        private void MoveToPlaceholder() {
            var placeholder = _placeholderGameObject.GetComponent<PlaceholderShip>();
            if (placeholder.IsOnBoard) {
                if (_gameTypeManager.IsOnline()) {
                    MoveServerRpc(
                        placeholder.Coordinate,
                        placeholder.direction,
                        _placeholderGameObject.transform.position,
                        _placeholderGameObject.transform.rotation,
                        new RpcParams()
                    );
                } else {
                    Move(
                        placeholder.Coordinate,
                        placeholder.direction,
                        _placeholderGameObject.transform.position,
                        _placeholderGameObject.transform.rotation
                    );
                }
            }
        }
        
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void MoveServerRpc(
            Vector2Int coordinate,
            Direction direction,
            Vector3 position,
            Quaternion rotation,
            RpcParams rpcParams
        ) {
            var sender = _multiplayerManager.GetPlayerData(rpcParams.Receive.SenderClientId).Player;
            if (sender != player) return;

            Move(coordinate, direction, position, rotation);
        }

        private void Move(Vector2Int coordinate, Direction direction, Vector3 position, Quaternion rotation) {
            var isMoved = board.MoveShip(this, coordinate, direction);
            if (isMoved) {
                SetCoordinate(coordinate);
                SetDirection(direction);
                SetOnBoard(true);
                transform.position = position;
                transform.rotation = rotation;
            }
        }
    }
}
