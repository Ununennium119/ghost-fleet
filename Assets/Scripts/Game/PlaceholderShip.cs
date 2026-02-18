using System;
using Game.Enum;
using Game.Manager;
using UnityEngine;

namespace Game {
    public class PlaceholderShip : MonoBehaviour {
        private const float CELL_SIZE = 1.0f;
        private const float CELL_SPACING = 0.1f;


        public Direction direction;
        public int size;

        public Vector2Int BoardPosition { get; private set; }
        public bool IsOnBoard { get; private set; }


        private InputManager _inputManager;


        private void Start() {
            _inputManager = InputManager.Instance;

            _inputManager.OnRotatePerformed += Rotate;

            Cell.OnAnyHover += CellOnAnyHoverAction;

            UpdateRotationBasedOnDirection();
        }

        private void OnDestroy() {
            _inputManager.OnRotatePerformed -= Rotate;

            Cell.OnAnyHover -= CellOnAnyHoverAction;
        }


        private void CellOnAnyHoverAction(object sender, EventArgs e) {
            var cell = sender as Cell;
            if (!cell) return;

            var offset = direction switch {
                Direction.Up => new Vector3(0, 0, (CELL_SIZE + CELL_SPACING) / 2f * (size - 1)),
                Direction.Down => new Vector3(0, 0, -(CELL_SIZE + CELL_SPACING) / 2f * (size - 1)),
                Direction.Left => new Vector3(-(CELL_SIZE + CELL_SPACING) / 2f * (size - 1), 0, 0),
                Direction.Right => new Vector3((CELL_SIZE + CELL_SPACING) / 2f * (size - 1), 0, 0),
                _ => throw new ArgumentOutOfRangeException()
            };
            transform.position = cell.transform.position + offset;

            BoardPosition = cell.GetPosition();
            IsOnBoard = true;
        }

        private void Rotate(object sender, InputManager.OnRotatePerformedArgs e) {
            direction = e.Value switch {
                > 0 => direction switch {
                    Direction.Up => Direction.Left,
                    Direction.Left => Direction.Down,
                    Direction.Down => Direction.Right,
                    Direction.Right => Direction.Up,
                    _ => throw new ArgumentOutOfRangeException()
                },
                < 0 => direction switch {
                    Direction.Up => Direction.Right,
                    Direction.Right => Direction.Down,
                    Direction.Down => Direction.Left,
                    Direction.Left => Direction.Up,
                    _ => throw new ArgumentOutOfRangeException()
                },
                _ => direction
            };
            UpdateRotationBasedOnDirection();
        }

        private void UpdateRotationBasedOnDirection() {
            transform.forward = direction switch {
                Direction.Up => Vector3.left,
                Direction.Down => Vector3.right,
                Direction.Left => -Vector3.forward,
                Direction.Right => Vector3.forward,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}
