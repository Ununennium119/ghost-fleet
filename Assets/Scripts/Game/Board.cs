using System;
using System.Collections.Generic;
using System.Linq;
using Game.Enum;
using MainMenu.Logic;
using Unity.Netcode;
using UnityEngine;

namespace Game {
    public class Board : NetworkBehaviour {
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


        private readonly NetworkList<NetworkObjectReference> _cellReferencesFlattened = new();

        private Cell[,] _cells;

        private GameTypeManager _gameTypeManager;


        public Player GetPlayer() {
            return player;
        }


        private Cell GetCell(Vector2Int position) {
            if (_gameTypeManager.IsOnline()) {
                var index = position.x * boardSize + position.y;
                var cellReference = _cellReferencesFlattened[index];
                if (cellReference.TryGet(out var cellNetworkObject)) {
                    var cell = cellNetworkObject.GetComponent<Cell>();
                    return cell;
                }
                throw new InvalidOperationException($"Failed to get network object of cell at position {position}");
            }
            return _cells[position.x, position.y];
        }


        public bool MoveShip(Ship ship, Vector2Int targetCoordinate, Direction targetDirection) {
            var newCoordinates = new List<Vector2Int>();
            for (var i = 0; i < ship.GetSize(); i++) {
                switch (targetDirection) {
                    case Direction.Up:
                        newCoordinates.Add(targetCoordinate + new Vector2Int(0, i));
                        break;
                    case Direction.Down:
                        newCoordinates.Add(targetCoordinate + new Vector2Int(0, -i));
                        break;
                    case Direction.Left:
                        newCoordinates.Add(targetCoordinate + new Vector2Int(-i, 0));
                        break;
                    case Direction.Right:
                        newCoordinates.Add(targetCoordinate + new Vector2Int(i, 0));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            var oldCoordinates = ship.GetCoordinates();
            var cellsToFill = new List<Cell>();
            foreach (var coordinate in newCoordinates) {
                if (
                    coordinate.x < 0 ||
                    coordinate.x >= boardSize ||
                    coordinate.y < 0 ||
                    coordinate.y >= boardSize
                ) {
                    return false;
                }
                var cell = GetCell(coordinate);
                if (!oldCoordinates.Contains(coordinate) && cell.HasShip()) {
                    return false;
                }
                cellsToFill.Add(cell);
            }

            var cellsToUnfill = new List<Cell>();
            foreach (var coordinate in oldCoordinates) {
                var cell = GetCell(coordinate);
                cellsToUnfill.Add(cell);
            }

            foreach (var cell in cellsToUnfill) {
                cell.SetFilled(false);
            }
            foreach (var cell in cellsToFill) {
                cell.SetFilled(true);
            }
            return true;
        }

        public bool AreShipsOnBoard() {
            return ships.All(ship => ship.IsOnBoard());
        }

        public bool IsDestroyed() {
            var cells = (
                from ship in ships
                from position in ship.GetCoordinates()
                select GetCell(position)
            ).ToList();
            return cells.All(cell => cell.IsAttacked());
        }


        private void Awake() {
            ResolveSingletons();

            if (_gameTypeManager.IsOffline()) {
                InstantiateCells();
            }
        }

        private void Start() {
            if (_gameTypeManager.IsOnline() && IsServer) {
                InstantiateCells();
            }
        }


        private void ResolveSingletons() {
            _gameTypeManager = GameTypeManager.Instance;
        }


        private void InstantiateCells() {
            _cells = new Cell[boardSize, boardSize];
            var containerPosition = cellsContainer.transform.transform.position;
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

                    if (_gameTypeManager.IsOnline()) {
                        var networkObject = cellGameObject.GetComponent<NetworkObject>();
                        networkObject.Spawn(true);
                        networkObject.TrySetParent(cellsContainer.transform);
                    }

                    var cell = cellGameObject.GetComponent<Cell>();
                    cell.Initialize(
                        board: this,
                        coordinate: new Vector2Int(x, z)
                    );

                    if (_gameTypeManager.IsOnline()) {
                        _cellReferencesFlattened.Add(cell.NetworkObject);
                    } else {
                        _cells[x, z] = cell;
                    }
                }
            }
        }
    }
}
