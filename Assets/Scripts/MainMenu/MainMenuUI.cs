using Common;
using Game;
using UnityEngine;
using UnityEngine.UI;

namespace MainMenu.UI {
    /// <summary>
    /// The UI for the main menu.
    /// </summary>
    public class MainMenuUI : MonoBehaviour {
        [SerializeField, Tooltip("The play button")]
        private Button playButton;
        [SerializeField, Tooltip("The quit button")]
        private Button quitButton;


        private void Awake() {
            playButton.onClick.AddListener(() => { SceneLoader.LoadScene(SceneLoader.Scene.GameScene); });
            quitButton.onClick.AddListener(Application.Quit);

            // Resetting (setting to null) all static objects used when loading main menu
            Cell.ResetStaticObjects();

            // Resetting time scale
            Time.timeScale = 1f;
        }
    }
}
