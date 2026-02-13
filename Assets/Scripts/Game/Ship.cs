using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game {
    /// <summary>
    /// Represents a ship.
    /// </summary>
    public class Ship {
        /// <summary>
        /// The position of the ship.
        /// </summary>
        private readonly Vector2Int _position;
        /// <summary>
        /// The size of the board.
        /// </summary>
        private readonly int _size;
        /// <summary>
        /// The direction of the ship.
        /// </summary>
        private readonly Direction _direction;

        public Ship(Vector2Int position, int size, Direction direction) {
            _position = position;
            _size = size;
            _direction = direction;
        }

        /// <returns>List of positions which this ship fills.</returns>
        /// <exception cref="ArgumentOutOfRangeException">If direction of the ship is invalid.</exception>
        public List<Vector2Int> GetPositions() {
            var positions = new List<Vector2Int>();
            for (var i = 0; i < _size; i++) {
                switch (_direction) {
                    case Direction.Top:
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
    }
}
