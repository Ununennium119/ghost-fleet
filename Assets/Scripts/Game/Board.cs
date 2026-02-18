using System;
using System.Collections.Generic;
using System.Linq;
using Game.Enum;
using Game.Manager;
using UnityEngine;

namespace Game {
    /// <summary>
    /// Represents a board.
    /// </summary>
    public class Board : MonoBehaviour {
        private const float CELL_SIZE = 1.0f;
        private const float CELL_SPACING = 0.1f;


        [SerializeField, Tooltip("The size of the board")]
        private int boardSize = 10;
        [SerializeField, Tooltip("The player owning the board")]
        private Player player;
        [SerializeField, Tooltip("The board's ships")]
        private Ship[] ships;

        [SerializeField, Tooltip("The cell prefab")]
        private GameObject cellPrefab;
        [SerializeField, Tooltip("The cells container")]
        private GameObject cellsContainer;


        private Cell[,] _cells;


        /// <returns>The player owning the board.</returns>
        public Player GetPlayer() {
            return player;
        }

        /// <summary>
        /// Moves the ship to the target position on the board.
        /// </summary>
        /// <param name="ship"> The ship to move.</param>
        /// <param name="targetPosition">The position to move the ship into.</param>
        /// <param name="targetDirection">The new direction of the ship.</param>
        public bool MoveShip(Ship ship, Vector2Int targetPosition, Direction targetDirection) {
            var oldPositions = ship.GetPositions();
            var newPositions = new List<Vector2Int>();
            for (var i = 0; i < ship.GetSize(); i++) {
                switch (targetDirection) {
                    case Direction.Up:
                        newPositions.Add(targetPosition + new Vector2Int(0, i));
                        break;
                    case Direction.Down:
                        newPositions.Add(targetPosition + new Vector2Int(0, -i));
                        break;
                    case Direction.Left:
                        newPositions.Add(targetPosition + new Vector2Int(-i, 0));
                        break;
                    case Direction.Right:
                        newPositions.Add(targetPosition + new Vector2Int(i, 0));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            var cellsToFill = new List<Cell>();
            foreach (var position in newPositions) {
                if (
                    position.x < 0 ||
                    position.x >= boardSize ||
                    position.y < 0 ||
                    position.y >= boardSize
                ) {
                    return false;
                }
                var cell = _cells[position.x, position.y];
                if (!oldPositions.Contains(position) && cell.IsFilled) {
                    return false;
                }
                cellsToFill.Add(cell);
            }

            var cellsToUnfill = new List<Cell>();
            foreach (var position in oldPositions) {
                var cell = _cells[position.x, position.y];
                cellsToUnfill.Add(cell);
            }

            foreach (var cell in cellsToUnfill) {
                cell.IsFilled = false;
            }
            foreach (var cell in cellsToFill) {
                cell.IsFilled = true;
            }
            return true;
        }

        /// <returns>True if all the ships are placed on the board.</returns>
        public bool AreShipsOnBoard() {
            return ships.All(ship => ship.IsOnBoard());
        }

        /// <returns>
        /// True if all the ships are destroyed.
        /// </returns>
        public bool IsDestroyed() {
            var cells = (
                from ship in ships
                from position in ship.GetPositions()
                select _cells[position.x, position.y]
            ).ToList();
            return cells.All(cell => cell.IsAttacked);
        }


        private void Awake() {
            var containerPosition = cellsContainer.transform.transform.position;
            _cells = new Cell[boardSize, boardSize];
            for (var x = 0; x < boardSize; x++) {
                for (var z = 0; z < boardSize; z++) {
                    var cellGameObject = Instantiate(
                        original: cellPrefab,
                        position: new Vector3(
                            x: containerPosition.x + (CELL_SIZE + CELL_SPACING) * (x - (boardSize - 1) / 2f),
                            y: 0,
                            z: containerPosition.z + (CELL_SIZE + CELL_SPACING) * (z - (boardSize - 1) / 2f)
                        ),
                        rotation: Quaternion.identity,
                        parent: cellsContainer.transform
                    );
                    var cell = cellGameObject.GetComponent<Cell>();
                    cell.Initialize(this, new Vector2Int(x, z));
                    _cells[x, z] = cell;
                }
            }
        }
    }
}
