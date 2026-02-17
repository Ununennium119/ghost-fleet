using System;
using Game.Manager;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Game {
    /// <summary>
    /// Represents a board cell.
    /// </summary>
    public class Cell : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler {
        /// <summary>
        /// This event is triggered whenever any cell is hovered.
        /// </summary>
        public static event EventHandler OnAnyHover;


        /// <summary>
        /// Resets the static objects, specifically the OnTrash event.
        /// This method is used to clean up the event subscription.
        /// </summary>
        public static void ResetStaticObjects() {
            OnAnyHover = null;
        }


        [SerializeField, Tooltip("the targeted game object visual")] [Required]
        private GameObject targetedVisual;

        [SerializeField, Tooltip("the attacked game object visual")] [Required]
        private GameObject attackedVisual;

        [SerializeField, Tooltip("the destroyed game object visual")] [Required]
        private GameObject destroyedVisual;


        private bool _isTargetable;


        /// <returns>True if the cell is attacked.</returns>
        public bool IsAttacked { get; private set; } = false;

        /// <returns>True if there is a ship in the cell.</returns>
        public bool IsFilled { get; set; } = false;

        /// <returns>The position of the cell.</returns>
        public Vector2Int Position { get; set; } = new();

        /// <returns>The board containing the cell.</returns>
        public Board Board { get; set; }


        private Camera _mainCamera;
        private Collider _objectCollider;

        private GameManager _gameManager;


        private void Awake() {
            _mainCamera = Camera.main;
            _objectCollider = GetComponent<Collider>();
        }

        private void Start() {
            _gameManager = GameManager.Instance;

            // To fix the problem of these transparent objects not being rendered on top of the ocean material
            GetComponent<Renderer>().material.renderQueue = 3100;
            targetedVisual.GetComponent<Renderer>().material.renderQueue = 3100;
            attackedVisual.GetComponent<Renderer>().material.renderQueue = 3100;
            destroyedVisual.GetComponent<Renderer>().material.renderQueue = 3100;
        }

        private void Update() {
            var mousePosition = Mouse.current.position.ReadValue();
            var ray = _mainCamera.ScreenPointToRay(mousePosition);
            if (_objectCollider.Raycast(ray, out _, 100f)) {
                OnAnyHover?.Invoke(this, EventArgs.Empty);
            }
        }


        public void OnPointerEnter(PointerEventData eventData) {
            if (!_isTargetable) return;

            targetedVisual.SetActive(true);
        }

        public void OnPointerExit(PointerEventData eventData) {
            if (!_isTargetable) return;

            targetedVisual.SetActive(false);
        }

        public void OnPointerClick(PointerEventData eventData) {
            if (!_isTargetable) return;
            _gameManager.AttackCell(Position);
        }


        /// <summary>
        /// Sets if the cell is targetable.
        /// </summary>
        public void SetTargetable(bool isTargetable) {
            _isTargetable = isTargetable;
        }

        /// <summary>
        /// Attacks the cell.
        /// </summary>
        public void Attack() {
            IsAttacked = true;
            targetedVisual.SetActive(false);
            if (IsFilled) {
                destroyedVisual.SetActive(true);
            } else {
                attackedVisual.SetActive(true);
            }
        }
    }
}
