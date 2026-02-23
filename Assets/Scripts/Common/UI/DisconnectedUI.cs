using Common.Utility;
using MainMenu.Logic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Common.UI {
    public class DisconnectedUI : NetworkBehaviour {
        [SerializeField, Tooltip("The main menu button")]
        private Button mainMenuButton;


        private GameTypeManager _gameTypeManager;


        private void Awake() {
            AddButtonListeners();
        }

        private void Start() {
            ResolveSingletons();
            SubscribeToEvents();
            Hide();
        }

        public override void OnDestroy() {
            UnsubscribeFromEvents();
        }


        private void AddButtonListeners() {
            mainMenuButton.onClick.AddListener(() => SceneLoader.LoadScene(SceneLoader.Scene.MainMenuScene));
        }

        private void ResolveSingletons() {
            _gameTypeManager = GameTypeManager.Instance;
        }

        private void SubscribeToEvents() {
            if (_gameTypeManager.IsOnline()) {
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallbackAction;
            }
        }

        private void UnsubscribeFromEvents() {
            if (_gameTypeManager.IsOnline()) {
                if (NetworkManager.Singleton != null) {
                    NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectCallbackAction;
                }
            }
        }

        
        private void OnClientDisconnectCallbackAction(ulong clientId) {
            if (clientId == NetworkManager.LocalClientId || clientId == NetworkManager.ServerClientId) {
                Show();
            }
        }


        private void Show() {
            gameObject.SetActive(true);
            mainMenuButton.Select();
        }

        private void Hide() {
            gameObject.SetActive(false);
        }
    }
}
