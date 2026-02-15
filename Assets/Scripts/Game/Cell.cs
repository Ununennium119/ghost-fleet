using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game {
    /// <summary>
    /// Represents a board cell.
    /// </summary>
    public class Cell : MonoBehaviour {
        /// <summary>
        /// This event is triggered whenever any cell is hovered.
        /// </summary>
        public static event EventHandler OnAnyHover;


        /// <returns>True if the cell is destroyed.</returns>
        public bool IsDestroyed { get; set; } = false;

        /// <returns>True if there is a ship in the cell.</returns>
        public bool IsFilled { get; set; } = false;

        /// <returns>The position of the cell.</returns>
        public Vector2Int Position { get; set; } = new();

        /// <returns>The board containing the cell.</returns>
        public Board Board { get; set; }


        private Camera _mainCamera;
        private Collider _objectCollider;


        private void Awake() {
            _mainCamera = Camera.main;
            _objectCollider = GetComponent<Collider>();
        }

        private void Update() {
            var mousePosition = Mouse.current.position.ReadValue();
            var ray = _mainCamera.ScreenPointToRay(mousePosition);
            if (_objectCollider.Raycast(ray, out _, 100f)) {
                OnAnyHover?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
