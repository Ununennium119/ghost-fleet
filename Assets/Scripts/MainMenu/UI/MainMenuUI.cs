using Common.Utility;
using Game;
using MainMenu.Logic;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.UI;

namespace MainMenu.UI {
    /// <summary>
    /// The UI for the main menu.
    /// </summary>
    public class MainMenuUI : MonoBehaviour {
        [SerializeField, Tooltip("The play offline button")] [Required]
        private Button playOfflineButton;
        [SerializeField, Tooltip("The play online button")] [Required]
        private Button playOnlineButton;
        [SerializeField, Tooltip("The quit button")] [Required]
        private Button quitButton;
        
        
        private GameTypeManager _gameTypeManager;


        private void Awake() {
            playOfflineButton.onClick.AddListener(() => {
                SceneLoader.LoadScene(SceneLoader.Scene.GameScene);
                _gameTypeManager.SetGameType(GameTypeManager.GameType.Offline);
            });
            playOnlineButton.onClick.AddListener(() => {
                SceneLoader.LoadScene(SceneLoader.Scene.LobbyScene);
                _gameTypeManager.SetGameType(GameTypeManager.GameType.Online);
            });
            quitButton.onClick.AddListener(Application.Quit);

            // Resetting (setting to null) all static objects used when loading main menu
            Cell.ResetStaticObjects();
            Ship.ResetStaticObjects();

            Time.timeScale = 1f;
        }

        private void Start() {
            _gameTypeManager = GameTypeManager.Instance;
        }
    }
}
