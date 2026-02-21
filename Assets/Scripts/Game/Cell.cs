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
    /// <summary>
    /// Represents a board cell.
    /// </summary>
    public class Cell : NetworkBehaviour {
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


        private readonly NetworkVariable<bool> _isFilledNetwork = new();
        private readonly NetworkVariable<bool> _isAttackedNetwork = new();

        private bool _isFilled = false;
        private bool _isAttacked = false;

        private bool _isHovered;
        private bool _isSelectable;
        private bool _isTargetable;

        private Board _board;
        private Vector2Int _position;

        private Camera _mainCamera;
        private Collider _objectCollider;

        private GameTypeManager _gameTypeManager;
        private MultiplayerManager _multiplayerManager;
        private InputManager _inputManager;
        private GameManager _gameManager;


        /// <returns>True if there is a ship in the cell.</returns>
        public bool IsFilled() {
            return _gameTypeManager.GetGameType() == GameTypeManager.GameType.Online
                ? _isFilledNetwork.Value
                : _isFilled;
        }

        public void SetFilled(bool value) {
            if (_gameTypeManager.GetGameType() == GameTypeManager.GameType.Online) {
                _isFilledNetwork.Value = value;
            } else {
                _isFilled = value;
            }
        }


        /// <returns>True if the cell is attacked.</returns>
        public bool IsAttacked() {
            return _gameTypeManager.GetGameType() == GameTypeManager.GameType.Online
                ? _isAttackedNetwork.Value
                : _isAttacked;
        }

        private void SetAttacked(bool value) {
            if (_gameTypeManager.GetGameType() == GameTypeManager.GameType.Online) {
                _isAttackedNetwork.Value = value;
            } else {
                _isAttacked = value;
                if (_isAttacked) {
                    OnAnyAttack?.Invoke(this, new OnAnyAttackArgs { IsDestroyed = _isFilled });
                }
            }
        }


        public void Initialize(Board board, Vector2Int position) {
            if (_gameTypeManager.GetGameType() == GameTypeManager.GameType.Online) {
                InitializeClientRpc(new NetworkBehaviourReference(board), position);
            } else {
                _board = board;
                _position = position;
                gameObject.name = $"Cell({_position.x},{_position.y})";
            }
        }

        public Vector2Int GetPosition() {
            return _position;
        }


        private void Awake() {
            _gameTypeManager = GameTypeManager.Instance;

            _mainCamera = Camera.main;
            _objectCollider = GetComponent<Collider>();
        }

        private void Start() {
            _multiplayerManager = MultiplayerManager.Instance;
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

        public override void OnNetworkSpawn() {
            _isAttackedNetwork.OnValueChanged += OnIsAttackedChangedAction;
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

            if (_gameTypeManager.GetGameType() == GameTypeManager.GameType.Online) {
                OnClickPerformedActionServerRpc(new RpcParams());
            } else {
                SetAttacked(true);
                targetedVisual.SetActive(false);
                if (IsFilled()) {
                    destroyedVisual.SetActive(true);
                } else {
                    attackedVisual.SetActive(true);
                }
            }
        }

        private void OnIsAttackedChangedAction(bool previousValue, bool newValue) {
            OnAnyAttack?.Invoke(this, new OnAnyAttackArgs { IsDestroyed = IsFilled() });
        }

        private void OnPhaseChangedAction(object sender, GameManager.OnPhaseChangedArgs e) {
            _isSelectable = e.GamePhase switch {
                GamePhase.Start => false,
                GamePhase.Placement1 => _board.GetPlayer() == Player.Player1,
                GamePhase.Placement2 => _board.GetPlayer() == Player.Player2,
                GamePhase.Placement => _multiplayerManager.GetLocalPlayerData().Player == _board.GetPlayer(),
                GamePhase.Attack1 => false,
                GamePhase.Attack2 => false,
                GamePhase.GameOver => false,
                _ => throw new ArgumentOutOfRangeException()
            };

            _isTargetable = e.GamePhase switch {
                GamePhase.Start => false,
                GamePhase.Placement1 => false,
                GamePhase.Placement2 => false,
                GamePhase.Placement => false,
                GamePhase.Attack1 => (
                                         _gameTypeManager.GetGameType() == GameTypeManager.GameType.Offline ||
                                         _multiplayerManager.GetLocalPlayerData().Player == Player.Player1
                                     ) &&
                                     _board.GetPlayer() == Player.Player2 &&
                                     !IsAttacked(),
                GamePhase.Attack2 => (
                                         _gameTypeManager.GetGameType() == GameTypeManager.GameType.Offline ||
                                         _multiplayerManager.GetLocalPlayerData().Player == Player.Player2
                                     ) &&
                                     _board.GetPlayer() == Player.Player1 &&
                                     !IsAttacked(),
                GamePhase.GameOver => false,
                _ => throw new ArgumentOutOfRangeException()
            };
        }


        [ClientRpc]
        private void InitializeClientRpc(NetworkBehaviourReference boardRef, Vector2Int position) {
            if (boardRef.TryGet(out Board board)) {
                _board = board;
                _position = position;
                gameObject.name = $"Cell({_position.x},{_position.y})";
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void OnClickPerformedActionServerRpc(RpcParams rpcParams) {
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
            targetedVisual.SetActive(false);
            if (IsFilled()) {
                destroyedVisual.SetActive(true);
            } else {
                attackedVisual.SetActive(true);
            }
        }
    }
}
