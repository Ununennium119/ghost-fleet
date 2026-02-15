using Game.Manager;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game {
    /// <summary>
    /// Handles ship selection logic.
    /// </summary>
    public class ShipSelectionHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler {
        [SerializeField, Tooltip("The visual for when the ship is selected")]
        private GameObject selectedGameObject;

        [SerializeField, Tooltip("The visual for when the ship is hovered")]
        private GameObject hoveredGameObject;

        [SerializeField, Tooltip("The related ship")]
        private Ship ship;


        /// <summary>
        /// True if the ship is selected.
        /// </summary>
        public bool IsSelected { get; private set; }


        private GameManager _gameManager;

        private bool _isSelectionEnabled = true;


        private void Start() {
            _gameManager = GameManager.Instance;
        }


        public void OnPointerEnter(PointerEventData eventData) {
            if (!_isSelectionEnabled) return;
            hoveredGameObject.SetActive(true);
        }

        public void OnPointerExit(PointerEventData eventData) {
            if (!_isSelectionEnabled) return;
            hoveredGameObject.SetActive(false);
        }

        public void OnPointerClick(PointerEventData eventData) {
            if (!_isSelectionEnabled) return;
            if (selectedGameObject.activeSelf) {
                _gameManager.DeselectShip(ship);
            } else {
                _gameManager.SelectShip(ship);
            }
        }


        /// <summary>
        /// Selects the ship visually.
        /// </summary>
        public void SelectVisually() {
            selectedGameObject.SetActive(true);
            IsSelected = true;
        }

        /// <summary>
        /// Deselects the ship visually.
        /// </summary>
        public void DeselectVisually() {
            selectedGameObject.SetActive(false);
            IsSelected = false;
        }

        /// <summary>
        /// Disables or enables the ship selection.
        /// </summary>
        /// <param name="value"></param>
        public void SetSelectionEnabled(bool value) {
            _isSelectionEnabled = value;
            if (!_isSelectionEnabled) {
                selectedGameObject.SetActive(false);
                hoveredGameObject.SetActive(false);
            }
        }
    }
}
