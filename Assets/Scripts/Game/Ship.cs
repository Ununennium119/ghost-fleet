using System;
using System.Collections.Generic;
using Game.Manager;
using UnityEngine;

namespace Game {
    /// <summary>
    /// Represents a ship.
    /// </summary>
    public class Ship : MonoBehaviour {
        [SerializeField, Tooltip("The size of the ship")]
        private int size;
        
        [SerializeField, Tooltip("The selection handler")]
        private ShipSelectionHandler selectionHandler;

        
        /// <summary>
        /// True, if the ship is placed in the board.
        /// </summary>
        public bool IsPlaced { get; private set; } = false;

        /// <summary>
        /// True, if the position of the ship is set in the board.
        /// </summary>
        public bool IsPositionSet { get; private set; } = false;


        private Vector2Int _position;
        private Direction _direction = Direction.Right;
        private Vector3 _defaultTransformPosition;
        private Quaternion _defaultTransformRotation;
        
        private InputManager _inputManager;


        private void Awake() {
            _defaultTransformPosition = transform.position;
            _defaultTransformRotation = transform.rotation;
        }

        private void Start() {
            _inputManager = InputManager.Instance;

            _inputManager.OnRotatePerformed += Rotate;
            _inputManager.OnCancelPerformed += ResetPosition;
        }

        private void Update() {
            transform.forward = _direction switch {
                Direction.Up => Vector3.right,
                Direction.Down => Vector3.left,
                Direction.Left => -Vector3.forward,
                Direction.Right => Vector3.forward,
                _ => throw new ArgumentOutOfRangeException()
            };
        }


        /// <summary>
        /// Selects the ship.
        /// </summary>
        public void Select() {
            selectionHandler.SelectVisually();
        }

        /// <summary>
        /// Deselects the ship.
        /// </summary>
        public void Deselect() {
            selectionHandler.DeselectVisually();

            if (IsPositionSet) {
                selectionHandler.SetSelectionEnabled(false);
            }
        }

        /// <summary>
        /// Enables selection.
        /// </summary>
        public void EnableSelection() {
            selectionHandler.SetSelectionEnabled(true);
        }

        /// <summary>
        /// Disables selection.
        /// </summary>
        public void DisableSelection() {
            selectionHandler.SetSelectionEnabled(false);
        }


        /// <returns>List of positions which this ship fills.</returns>
        /// <exception cref="ArgumentOutOfRangeException">If direction of the ship is invalid.</exception>
        public List<Vector2Int> GetPositions() {
            var positions = new List<Vector2Int>();
            for (var i = 0; i < size; i++) {
                switch (_direction) {
                    case Direction.Up:
                        positions.Add(_position + new Vector2Int(0, i));
                        break;
                    case Direction.Down:
                        positions.Add(_position + new Vector2Int(0, -i));
                        break;
                    case Direction.Left:
                        positions.Add(_position + new Vector2Int(-i, 0));
                        break;
                    case Direction.Right:
                        positions.Add(_position + new Vector2Int(i, 0));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            return positions;
        }

        /// <summary>
        /// Sets the position of the ship in the board.
        /// </summary>
        /// <param name="position">The xz position of the target cell.</param>
        /// <param name="transformPosition">The xyz position of the target cell transform.</param>
        /// <exception cref="ArgumentOutOfRangeException">If direction of the ship is invalid.</exception>
        public void SetPosition(Vector2Int position, Vector3 transformPosition) {
            _position = position;

            var offset = _direction switch {
                Direction.Up => new Vector3(0, 0, 0.55f * (size - 1)),
                Direction.Down => new Vector3(0, 0, -0.55f * (size - 1)),
                Direction.Left => new Vector3(-0.55f * (size - 1), 0, 0),
                Direction.Right => new Vector3(0.55f * (size - 1), 0, 0),
                _ => throw new ArgumentOutOfRangeException()
            };
            transform.position = transformPosition + offset;
            IsPositionSet = true;
        }

        /// <summary>
        /// Places the ship on the board.
        /// </summary>
        /// <param name="board">The target board.</param>
        public bool Place(Board board) {
            var isAdded = board.AddShip(this);
            if (!isAdded) return false;
            IsPlaced = true;
            return true;
        }

        
        private void ResetPosition(object sender, EventArgs e) {
            if (!selectionHandler.IsSelected) return;
            
            _direction = Direction.Right;
            _position = new Vector2Int(-1, -1);
            transform.position = _defaultTransformPosition;
            transform.rotation = _defaultTransformRotation;
            selectionHandler.DeselectVisually();
            IsPositionSet = false;
        }

        private void Rotate(object sender, InputManager.OnRotatePerformedArgs e) {
            if (!selectionHandler.IsSelected) return;
            _direction = e.Value switch {
                > 0 => _direction switch {
                    Direction.Up => Direction.Left,
                    Direction.Left => Direction.Down,
                    Direction.Down => Direction.Right,
                    Direction.Right => Direction.Up,
                    _ => throw new ArgumentOutOfRangeException()
                },
                < 0 => _direction switch {
                    Direction.Up => Direction.Right,
                    Direction.Right => Direction.Down,
                    Direction.Down => Direction.Left,
                    Direction.Left => Direction.Up,
                    _ => throw new ArgumentOutOfRangeException()
                },
                _ => _direction
            };
        }
    }
}
