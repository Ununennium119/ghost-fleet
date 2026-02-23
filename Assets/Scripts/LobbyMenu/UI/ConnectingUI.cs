using System;
using Common.Logic;
using UnityEngine;

namespace LobbyMenu.UI {
    public class ConnectingUI : MonoBehaviour {
        private MultiplayerManager _multiplayerManager;


        private void Start() {
            ResolveSingletons();
            SubscribeToEvents();
            Hide();
        }

        private void OnDestroy() {
            UnsubscribeFromEvents();
        }


        private void ResolveSingletons() {
            _multiplayerManager = MultiplayerManager.Instance;
        }

        private void SubscribeToEvents() {
            _multiplayerManager.OnTryingToJoin += OnTryingToJoinAction;
            _multiplayerManager.OnFailedToJoin += OnFailedToJoinAction;
        }

        private void UnsubscribeFromEvents() {
            _multiplayerManager.OnTryingToJoin -= OnTryingToJoinAction;
            _multiplayerManager.OnFailedToJoin -= OnFailedToJoinAction;
        }


        private void OnTryingToJoinAction(object sender, EventArgs e) {
            Show();
        }

        private void OnFailedToJoinAction(object sender, EventArgs e) {
            Hide();
        }


        private void Show() {
            gameObject.SetActive(true);
        }

        private void Hide() {
            gameObject.SetActive(false);
        }
    }
}
