using System.Collections.Generic;
using LobbyMenu.Logic;
using Unity.Services.Lobbies.Models;
using UnityEngine;

namespace LobbyMenu.UI {
    public class LobbyListUI : MonoBehaviour {
        [SerializeField, Tooltip("The game object which contains lobbies")]
        private Transform lobbyContainer;
        [SerializeField, Tooltip("The lobby template")]
        private Transform lobbyTemplate;


        private LobbyManager _lobbyManager;


        private void Awake() {
            HideLobbyTemplate();
        }

        private void Start() {
            ResolveSingletons();
            SubscribeToEvents();
        }

        private void OnDestroy() {
            UnsubscribeFromEvents();
        }


        private void HideLobbyTemplate() {
            lobbyTemplate.gameObject.SetActive(false);
        }

        private void ResolveSingletons() {
            _lobbyManager = LobbyManager.Instance;
        }

        private void SubscribeToEvents() {
            _lobbyManager.OnLobbyListRefreshed += OnLobbyListRefreshedAction;
        }

        private void UnsubscribeFromEvents() {
            _lobbyManager.OnLobbyListRefreshed -= OnLobbyListRefreshedAction;
        }


        private void OnLobbyListRefreshedAction(object sender, LobbyManager.OnLobbyListRefreshedEventArgs e) {
            UpdateLobbyList(e.LobbyList);
        }


        private void UpdateLobbyList(List<Lobby> lobbyList) {
            foreach (Transform child in lobbyContainer) {
                if (child == lobbyTemplate) continue;

                Destroy(child.gameObject);
            }

            foreach (var lobby in lobbyList) {
                var lobbyItem = Instantiate(lobbyTemplate, lobbyContainer);
                lobbyItem.GetComponent<SingleLobbyUI>().SetLobby(lobby);
                lobbyItem.gameObject.SetActive(true);
            }
        }
    }
}
