using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game {
    /// <summary>
    /// Represents a board.
    /// </summary>
    public class Board : MonoBehaviour {
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
            var boardSize = GameManager.Instance.GetBoardSize();
            _cells = new Cell[boardSize, boardSize];
            for (var x = 0; x < boardSize; x++) {
                for (var z = 0; z < boardSize; z++) {
                    var xSign = growLeft ? -1 : 1;
                    var position = new Vector3(
                        x: transform.position.x + xSign * x * (cellSpacing + cellPrefab.transform.localScale.x),
                        y: 0,
                        z: (z - boardSize / 2) * (cellSpacing + cellPrefab.transform.localScale.z)
                    );
                    var cellGameObject = Instantiate(
                        original: cellPrefab,
                        position: position,
                        rotation: Quaternion.identity,
                        parent: transform
                    );

                    if (growLeft) {
                        _cells[boardSize - 1 - x, z] = cellGameObject.GetComponent<Cell>();
                    } else {
                        _cells[x, z] = cellGameObject.GetComponent<Cell>();
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
                var cell = _cells[position.x, position.y];
                if (cell.IsFilled || cell.IsDestroyed) {
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
        public bool DestroyCell(Vector2Int position) {
            var cell = _cells[position.x, position.y];
            cell.IsDestroyed = true;
            return cell.IsFilled;
        }

        /// <returns>
        /// True if all the cells containing a ship are destroyed.
        /// </returns>
        public bool isDestroyed() {
            return _cells.Cast<Cell>().All(cell => !cell.IsFilled || cell.IsDestroyed);
        }
    }
}
