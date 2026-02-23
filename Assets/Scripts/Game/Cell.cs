using System;
using Common.Logic;
using Game.Enum;
using Game.Manager;
using MainMenu.Logic;
using NaughtyAttributes;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game {
    public class Cell : NetworkBehaviour {
        private const float RAYCAST_MAX_DISTANCE = 50f;


        public static event EventHandler OnAnyHover;

        public static event EventHandler<OnAnyAttackArgs> OnAnyAttack;
        public class OnAnyAttackArgs : EventArgs {
            public bool IsDestroyed;
        }


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


        private readonly NetworkVariable<bool> _hasShipNetwork = new();
        private readonly NetworkVariable<bool> _isAttackedNetwork = new();
        private readonly NetworkVariable<bool> _isTargetableNetwork = new();

        private bool _hasShip = false;
        private bool _isAttacked = false;
        private bool _isTargetable = false;

        private bool _isHovered;
        private bool _isSelectable;

        private Board _board;
        private Vector2Int _coordinate;

        private Camera _mainCamera;
        private Collider _objectCollider;

        private GameTypeManager _gameTypeManager;
        private MultiplayerManager _multiplayerManager;
        private InputManager _inputManager;
        private GameManager _gameManager;


        public bool HasShip() {
            return _gameTypeManager.IsOnline()
                ? _hasShipNetwork.Value
                : _hasShip;
        }

        public void SetFilled(bool value) {
            if (_gameTypeManager.IsOnline()) {
                _hasShipNetwork.Value = value;
            } else {
                _hasShip = value;
            }
        }


        public bool IsAttacked() {
            return _gameTypeManager.IsOnline()
                ? _isAttackedNetwork.Value
                : _isAttacked;
        }

        private void SetAttacked(bool value) {
            if (_gameTypeManager.IsOnline()) {
                _isAttackedNetwork.Value = value;
            } else {
                _isAttacked = value;
                if (_isAttacked) {
                    OnAnyAttack?.Invoke(this, new OnAnyAttackArgs { IsDestroyed = _hasShip });
                }
            }
        }


        public bool IsTargetable() {
            return _gameTypeManager.IsOnline()
                ? _isTargetableNetwork.Value
                : _isTargetable;
        }

        private void SetTargetable(bool value) {
            if (_gameTypeManager.IsOnline()) {
                _isTargetableNetwork.Value = value;
            } else {
                _isTargetable = value;
            }
        }


        public Vector2Int GetCoordinate() {
            return _coordinate;
        }


        public void Initialize(Board board, Vector2Int coordinate) {
            if (_gameTypeManager.IsOnline()) {
                InitializeClientRpc(new NetworkBehaviourReference(board), coordinate);
            } else {
                ApplyInitialization(board, coordinate);
            }
        }

        [ClientRpc]
        private void InitializeClientRpc(NetworkBehaviourReference boardRef, Vector2Int position) {
            if (boardRef.TryGet(out Board board)) {
                ApplyInitialization(board, position);
            }
        }

        private void ApplyInitialization(Board board, Vector2Int coordinate) {
            _board = board;
            _coordinate = coordinate;
            gameObject.name = $"Cell({_coordinate.x},{_coordinate.y})";
        }


        private void Awake() {
            ResolveSingletonsAwake();
            CacheReferences();
        }

        private void Start() {
            ResolveSingletonsStart();
            SubscribeToEvents();
            // To fix the problem of transparent objects not being rendered on top of the ocean material
            UpdateRenderQueue();
        }

        public override void OnNetworkSpawn() {
            SubscribeToNetworkEvents();
        }

        private void Update() {
            CheckHover();
        }


        private void ResolveSingletonsAwake() {
            _gameTypeManager = GameTypeManager.Instance;
        }

        private void CacheReferences() {
            _mainCamera = Camera.main;
            _objectCollider = GetComponent<Collider>();
        }

        private void ResolveSingletonsStart() {
            _multiplayerManager = MultiplayerManager.Instance;
            _inputManager = InputManager.Instance;
            _gameManager = GameManager.Instance;
        }

        private void SubscribeToEvents() {
            _inputManager.OnClickPerformed += OnClickPerformedAction;
            _gameManager.OnPhaseChanged += OnPhaseChangedAction;
        }

        private void SubscribeToNetworkEvents() {
            _isAttackedNetwork.OnValueChanged += OnIsAttackedChangedAction;
        }

        private void UpdateRenderQueue() {
            GetComponent<Renderer>().material.renderQueue = 3100;
            targetedVisual.GetComponent<Renderer>().material.renderQueue = 3100;
            attackedVisual.GetComponent<Renderer>().material.renderQueue = 3100;
            destroyedVisual.GetComponent<Renderer>().material.renderQueue = 3100;
        }

        private void CheckHover() {
            var mousePosition = Mouse.current.position.ReadValue();
            var ray = _mainCamera.ScreenPointToRay(mousePosition);
            _isHovered = _objectCollider.Raycast(ray, out _, RAYCAST_MAX_DISTANCE);

            if (_isHovered && _isSelectable) {
                OnAnyHover?.Invoke(this, EventArgs.Empty);
            }
            if (IsTargetable() && _multiplayerManager.GetLocalPlayerData().Player != _board.GetPlayer()) {
                targetedVisual.SetActive(_isHovered);
            } else {
                targetedVisual.SetActive(false);
            }
        }


        private void OnClickPerformedAction(object sender, EventArgs e) {
            HandleAttack();
        }

        private void OnPhaseChangedAction(object sender, GameManager.OnPhaseChangedArgs e) {
            UpdateSelectable(e.GamePhase);
            UpdateTargetable(e.GamePhase);
        }

        private void OnIsAttackedChangedAction(bool previousValue, bool newValue) {
            if (newValue) {
                OnAnyAttack?.Invoke(this, new OnAnyAttackArgs { IsDestroyed = HasShip() });
            }
        }


        private void UpdateSelectable(GamePhase gamePhase) {
            _isSelectable = gamePhase switch {
                GamePhase.Start => false,
                GamePhase.Placement1 => _board.GetPlayer() == Player.Player1,
                GamePhase.Placement2 => _board.GetPlayer() == Player.Player2,
                GamePhase.Placement => _multiplayerManager.GetLocalPlayerData().Player == _board.GetPlayer(),
                GamePhase.Attack1 => false,
                GamePhase.Attack2 => false,
                GamePhase.GameOver => false,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private void UpdateTargetable(GamePhase gamePhase) {
            if (_gameTypeManager.IsOnline() && !IsServer) return;

            SetTargetable(
                gamePhase switch {
                    GamePhase.Start => false,
                    GamePhase.Placement1 => false,
                    GamePhase.Placement2 => false,
                    GamePhase.Placement => false,
                    GamePhase.Attack1 => _board.GetPlayer() == Player.Player2 && !IsAttacked(),
                    GamePhase.Attack2 => _board.GetPlayer() == Player.Player1 && !IsAttacked(),
                    GamePhase.GameOver => false,
                    _ => throw new ArgumentOutOfRangeException()
                }
            );
        }


        private void HandleAttack() {
            if (!IsTargetable() || !_isHovered) return;

            if (_gameTypeManager.IsOnline()) {
                HandleAttackServerRpc(new RpcParams());
            } else {
                SetAttacked(true);
                HandleAttackVisual();
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void HandleAttackServerRpc(RpcParams rpcParams) {
            var sender = _multiplayerManager.GetPlayerData(rpcParams.Receive.SenderClientId).Player;
            if (sender == _board.GetPlayer()) return;
            if (_gameManager.GetCurrentGamePhase() is not GamePhase.Attack1 and not GamePhase.Attack2) return;
            if (
                _gameManager.GetCurrentGamePhase() == GamePhase.Attack1 &&
                _board.GetPlayer() == Player.Player1
            ) return;
            if (
                _gameManager.GetCurrentGamePhase() == GamePhase.Attack2 &&
                _board.GetPlayer() == Player.Player2
            ) return;

            SetAttacked(true);
            HandleAttackVisualClientRpc(new ClientRpcParams());
        }

        [ClientRpc]
        private void HandleAttackVisualClientRpc(ClientRpcParams _) {
            HandleAttackVisual();
        }

        private void HandleAttackVisual() {
            targetedVisual.SetActive(false);
            if (HasShip()) {
                destroyedVisual.SetActive(true);
            } else {
                attackedVisual.SetActive(true);
            }
        }
    }
}
