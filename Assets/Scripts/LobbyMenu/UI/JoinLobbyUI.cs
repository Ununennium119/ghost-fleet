using LobbyMenu.Logic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LobbyMenu.UI {
    public class JoinLobbyUI : MonoBehaviour {
        [SerializeField, Tooltip("The lobby code input")]
        private TMP_InputField lobbyCodeInput;
        [SerializeField, Tooltip("The join button")]
        private Button joinButton;
        [SerializeField, Tooltip("The close button")]
        private Button closeButton;


        private LobbyManager _lobbyManager;


        public void Show() {
            gameObject.SetActive(true);
            lobbyCodeInput.Select();
        }


        private void Awake() {
            AddButtonListeners();
        }

        private void Start() {
            ResolveSingletons();
            Hide();
        }


        private void AddButtonListeners() {
            joinButton.onClick.AddListener(() => { _lobbyManager.JoinLobbyByCode(lobbyCodeInput.text); });
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
