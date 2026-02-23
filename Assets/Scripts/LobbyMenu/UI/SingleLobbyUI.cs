using LobbyMenu.Logic;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

namespace LobbyMenu.UI {
    public class SingleLobbyUI : MonoBehaviour {
        [SerializeField, Tooltip("The lobby name text")]
        private TextMeshProUGUI lobbyName;
        [SerializeField, Tooltip("The join button")]
        private Button joinButton;


        private string _lobbyId;

        private LobbyManager _lobbyManager;


        public void SetLobby(Lobby lobby) {
            lobbyName.text = lobby.Name;
            _lobbyId = lobby.Id;
        }


        private void Awake() {
            AddButtonListeners();
        }

        private void Start() {
            ResolveSingletons();
        }


        private void AddButtonListeners() {
            joinButton.onClick.AddListener(() => _lobbyManager.JoinLobbyById(_lobbyId));
        }

        private void ResolveSingletons() {
            _lobbyManager = LobbyManager.Instance;
        }
    }
}
