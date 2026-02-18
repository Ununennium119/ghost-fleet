using NaughtyAttributes;
using UnityEngine;

namespace Game {
    public class ShipVisual : MonoBehaviour {
        [SerializeField, Tooltip("The related ship")] [Required]
        private Ship ship;

        [SerializeField, Tooltip("The visual for when the ship is selected")] [Required]
        private GameObject selectedGameObject;
        [SerializeField, Tooltip("The visual for when the ship is hovered")] [Required]
        private GameObject hoveredGameObject;


        private void Awake() {
            ship.OnHoverChanged += OnHoverChangedAction;
            ship.OnSelectionChanged += OnSelectionChangedAction;
        }


        private void OnHoverChangedAction(object sender, Ship.OnHoverChangedArgs e) {
            hoveredGameObject.SetActive(e.IsHovered);
        }

        private void OnSelectionChangedAction(object sender, Ship.OnSelectionChangedArgs e) {
            selectedGameObject.SetActive(e.IsSelected);
        }
    }
}
