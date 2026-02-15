using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game {
    /// <summary>
    /// Represents a board.
    /// </summary>
    public class Board : MonoBehaviour {
        [SerializeField, Tooltip("The size of the board")]
        private int boardSize;

        [SerializeField, Tooltip("The board cell prefab")]
        private GameObject cellPrefab;
        
        [SerializeField, Tooltip("The space between cells")]
        private float cellSpacing = 0.1f;

        private Cell[,] _cells;
        private readonly List<Ship> _ships = new();

        /// <summary>
        /// Initializes the board with cells.
        /// </summary>
        /// <param name="growLeft">
        /// If true, cells are instantiated from bottom-right to top-left, otherwise, cells are instantiated from bottom-left to top-right.
        /// </param>
        public void Initialize(bool growLeft) {
            _cells = new Cell[boardSize, boardSize];
            for (var x = 0; x < boardSize; x++) {
                for (var z = 0; z < boardSize; z++) {
                    var xSign = growLeft ? -1 : 1;
                    var position = new Vector3(
                        x: transform.position.x + xSign * x * (cellSpacing + cellPrefab.transform.localScale.x),
                        y: 0,
                        z: (z - boardSize / 2f) * (cellSpacing + cellPrefab.transform.localScale.z)
                    );
                    var cellGameObject = Instantiate(
                        original: cellPrefab,
                        position: position,
                        rotation: Quaternion.identity,
                        parent: transform
                    );

                    var cell = cellGameObject.GetComponent<Cell>();
                    cell.Board = this;
                    if (growLeft) {
                        cell.Position = new Vector2Int(boardSize - 1 - x, z);
                        _cells[boardSize - 1 - x, z] = cell;
                    } else {
                        cell.Position = new Vector2Int(x, z);
                        _cells[x, z] = cell;
                    }
                }
            }
        }

        /// <summary>
        /// Adds the ship to the board based on its position.
        /// </summary>
        /// <param name="ship">
        /// The ship to the board.
        /// </param>
        public bool AddShip(Ship ship) {
            var positions = ship.GetPositions();

            var cellsToFill = new List<Cell>();
            foreach (var position in positions) {
                if (
                    position.x < 0 ||
                    position.x >= boardSize ||
                    position.y < 0 ||
                    position.y >= boardSize
                ) {
                    return false;
                }
                var cell = _cells[position.x, position.y];
                if (cell.IsFilled || cell.IsAttacked) {
                    return false;
                }
                cellsToFill.Add(cell);
            }

            foreach (var cell in cellsToFill) {
                cell.IsFilled = true;
            }
            _ships.Add(ship);

            return true;
        }

        /// <summary>
        /// Destroyed the cell with the given position in the board.
        /// </summary>
        /// <param name="position">
        /// The position of the cell to destroy.
        /// </param>
        /// <returns>
        /// True if the cell was filled (a ship was in it).
        /// </returns>
        public bool AttackCell(Vector2Int position) {
            var cell = _cells[position.x, position.y];
            cell.Attack();
            return cell.IsFilled;
        }

        /// <returns>
        /// True if all the cells containing a ship are destroyed.
        /// </returns>
        public bool IsDestroyed() {
            return _cells.Cast<Cell>().All(cell => !cell.IsFilled || cell.IsAttacked);
        }

        /// <summary>
        /// Sets if the cells are targetable.
        /// </summary>
        public void SetTargetable(bool isTargetable) {
            foreach (var cell in _cells) {
                if (cell.IsAttacked) {
                    cell.SetTargetable(false);
                } else {
                    cell.SetTargetable(isTargetable);
                }
            }
        }
    }
}
