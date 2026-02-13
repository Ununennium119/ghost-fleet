using UnityEngine;

namespace Game {
    /// <summary>
    /// Represents a board cell.
    /// </summary>
    public class Cell : MonoBehaviour {
        /// <returns>True if the cell is destroyed.</returns>
        public bool IsDestroyed { get; set; } = false;

        /// <returns>True if there is a ship in the cell.</returns>
        public  bool IsFilled { get; set; } = false;
    }
}
