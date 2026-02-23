using System;
using System.Collections.Generic;
using System.Linq;
using Common.Utility;
using LobbyMenu.Logic;
using Unity.Netcode;
using Logger = Common.Utility.Logger;

namespace WaitingMenu.Logic {
    /// <remarks>This class is singleton.</remarks>
    public class WaitingManager : NetworkBehaviour {
        public static WaitingManager Instance { get; private set; }


        public event EventHandler OnReadyChanged;


        private LobbyManager _lobbyManager;

        /// <summary>
        /// Tracks the ready status of each player by their client ID.
        /// </summary>
        private readonly Dictionary<ulong, bool> _playerReadyDictionary = new();


        public void SetPlayerReady() {
            SetPlayerReadyServerRpc();
        }

        public bool IsPlayerReady(ulong clientId) {
            return _playerReadyDictionary.GetValueOrDefault(clientId, false);
        }


        private void Awake() {
            InitializeSingleton();
        }

        private void Start() {
            ResolveSingletons();
        }


        private void InitializeSingleton() {
            Logger.LogInitializingInstance(this);
            if (Instance != null) {
                Logger.LogMultipleInstancesError(this);
                Destroy(gameObject);
                return;
            }
            Instance = this;
            Logger.LogInstanceInitialized(this);
        }

        private void ResolveSingletons() {
            _lobbyManager = LobbyManager.Instance;
        }


        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void SetPlayerReadyServerRpc(RpcParams rpcParams = default) {
            var clientId = rpcParams.Receive.SenderClientId;
            _playerReadyDictionary[clientId] = true;
            SetLocalPlayerReadyClientRpc(clientId);

            var playerReadyList = NetworkManager.Singleton.ConnectedClientsIds.Select(playerId =>
                _playerReadyDictionary.TryGetValue(playerId, out var isReady) && isReady
            );
            if (playerReadyList.All(isPlayerReady => isPlayerReady)) {
                _lobbyManager.DeleteLobby();
                SceneLoader.LoadNetworkScene(SceneLoader.Scene.GameScene);
            }
        }

        [ClientRpc]
        private void SetLocalPlayerReadyClientRpc(ulong clientId) {
            _playerReadyDictionary[clientId] = true;
            OnReadyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
