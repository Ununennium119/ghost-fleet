using LobbyMenu.Logic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LobbyMenu.UI {
    public class CreateLobbyUI : MonoBehaviour {
        [SerializeField, Tooltip("The lobby name input")]
        private TMP_InputField lobbyNameInput;
        [SerializeField, Tooltip("The lobby private toggle")]
        private Toggle lobbyPrivateToggle;
        [SerializeField, Tooltip("The create button")]
        private Button createButton;
        [SerializeField, Tooltip("The close button")]
        private Button closeButton;


        private LobbyManager _lobbyManager;


        public void Show() {
            gameObject.SetActive(true);
            lobbyNameInput.Select();
        }


        private void Awake() {
            AddButtonListeners();
        }

        private void Start() {
            ResolveSingletons();
            Hide();
        }


        private void AddButtonListeners() {
            createButton.onClick.AddListener(() => {
                _lobbyManager.CreateLobby(lobbyNameInput.text, lobbyPrivateToggle.isOn);
            });
            closeButton.onClick.AddListener(() => {
                EventSystem.current.SetSelectedGameObject(null);
                Hide();
            });
        }

        private void ResolveSingletons() {
            _lobbyManager = LobbyManager.Instance;
        }

        private void Hide() {
            gameObject.SetActive(false);
        }
    }
}
