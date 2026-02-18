using Common;
using Game;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.UI;

namespace MainMenu {
    /// <summary>
    /// The UI for the main menu.
    /// </summary>
    public class MainMenuUI : MonoBehaviour {
        [SerializeField, Tooltip("The play button")] [Required]
        private Button playButton;
        [SerializeField, Tooltip("The quit button")] [Required]
        private Button quitButton;


        private void Awake() {
            playButton.onClick.AddListener(() => { SceneLoader.LoadScene(SceneLoader.Scene.GameScene); });
            quitButton.onClick.AddListener(Application.Quit);

            // Resetting (setting to null) all static objects used when loading main menu
            Cell.ResetStaticObjects();
            Ship.ResetStaticObjects();

            Time.timeScale = 1f;
        }
    }
}
