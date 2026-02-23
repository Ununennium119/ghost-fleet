using Common.Utility;
using Game;
using MainMenu.Logic;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.UI;

namespace MainMenu.UI {
    public class MainMenuUI : MonoBehaviour {
        [Header("Buttons")]
        [SerializeField, Tooltip("The play offline button")] [Required]
        private Button playOfflineButton;
        [SerializeField, Tooltip("The play online button")] [Required]
        private Button playOnlineButton;
        [SerializeField, Tooltip("The options button")] [Required]
        private Button optionsButton;
        [SerializeField, Tooltip("The quit button")] [Required]
        private Button quitButton;

        [Header("UIs")]
        [SerializeField, Tooltip("The options UI")] [Required]
        private OptionsUI optionsUI;


        private GameTypeManager _gameTypeManager;


        private void Awake() {
            AddButtonListeners();
            ResetStaticObjects();            
            ResetTimeScale();
        }

        private void Start() {
            ResolveSingletons();
        }


        private void AddButtonListeners() {
            playOfflineButton.onClick.AddListener(() => {
                SceneLoader.LoadScene(SceneLoader.Scene.GameScene);
                _gameTypeManager.SetGameType(GameTypeManager.GameType.Offline);
            });
            playOnlineButton.onClick.AddListener(() => {
                SceneLoader.LoadScene(SceneLoader.Scene.LobbyScene);
                _gameTypeManager.SetGameType(GameTypeManager.GameType.Online);
            });
            optionsButton.onClick.AddListener(optionsUI.Show);
            quitButton.onClick.AddListener(Application.Quit);
        }
        
        private void ResetStaticObjects() {
            Cell.ResetStaticObjects();
            Ship.ResetStaticObjects();
        }

        private void ResetTimeScale() {
            Time.timeScale = 1f;
        }


        private void ResolveSingletons() {
            _gameTypeManager = GameTypeManager.Instance;
        }
    }
}
