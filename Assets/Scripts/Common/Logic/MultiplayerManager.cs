using System;
using Common.Utility;
using Game;
using Game.Enum;
using Unity.Netcode;
using Unity.Services.Authentication;

namespace Common.Logic {
    /// <remarks>This class is singleton.</remarks>
    public class MultiplayerManager : NetworkBehaviour {
        public const int MAX_PLAYER_COUNT = 2;


        public static MultiplayerManager Instance { get; private set; }


        public event EventHandler OnTryingToJoin;

        public event EventHandler OnFailedToJoin;

        public event EventHandler OnPlayerDataListChanged;


        private readonly NetworkList<PlayerData> _playerDataList = new();


        public void StartClient() {
            OnTryingToJoin?.Invoke(this, EventArgs.Empty);
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallbackAction;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallbackAction;
            NetworkManager.Singleton.StartClient();
        }

        public void StartHost() {
            NetworkManager.Singleton.ConnectionApprovalCallback = ConnectionApprovalCallback;
            NetworkManager.Singleton.OnClientConnectedCallback += HostOnClientConnectedCallbackAction;
            NetworkManager.Singleton.OnClientDisconnectCallback += HostOnClientDisconnectCallbackAction;
            NetworkManager.Singleton.StartHost();
        }


        public bool HasPlayerData(int index) {
            return index >= 0 && _playerDataList.Count > index;
        }

        public PlayerData GetPlayerData(int index) {
            return _playerDataList[index];
        }

        public PlayerData GetPlayerData(ulong clientId) {
            foreach (var playerData in _playerDataList) {
                if (playerData.ClientId == clientId) {
                    return playerData;
                }
            }
            return default;
        }

        public PlayerData GetLocalPlayerData() {
            return GetPlayerData(NetworkManager.Singleton.LocalClientId);
        }


        private void Awake() {
            InitializeSingleton();
            SubscribeToEvents();
            DontDestroyOnLoad(gameObject);
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

        private void SubscribeToEvents() {
            _playerDataList.OnListChanged += OnPlayerDataListChangedAction;
        }


        private void ConnectionApprovalCallback(
            NetworkManager.ConnectionApprovalRequest request,
            NetworkManager.ConnectionApprovalResponse response
        ) {
            // Host is approved
            if (request.ClientNetworkId == 0) {
                response.Approved = true;
                return;
            }

            if (!SceneLoader.IsSceneActive(SceneLoader.Scene.WaitingScene)) {
                response.Approved = false;
                response.Reason = "Game started already!";
                return;
            }
            if (NetworkManager.Singleton.ConnectedClientsIds.Count >= MAX_PLAYER_COUNT) {
                response.Approved = false;
                response.Reason = "Game is full!";
                return;
            }
            response.Approved = true;
        }

        private void OnClientConnectedCallbackAction(ulong clientId) {
            ChangePlayerNameAndIdServerRpc(
                PlayerPrefsManager.GetPlayerName(),
                AuthenticationService.Instance.PlayerId,
                new RpcParams()
            );
        }

        private void OnClientDisconnectCallbackAction(ulong clientId) {
            OnFailedToJoin?.Invoke(this, EventArgs.Empty);
        }

        private void HostOnClientConnectedCallbackAction(ulong clientId) {
            _playerDataList.Add(
                new PlayerData {
                    ClientId = clientId,
                    Player = _playerDataList.Count == 0 ? Player.Player1 : Player.Player2
                }
            );
            if (clientId == NetworkManager.Singleton.LocalClientId) {
                ChangePlayerNameAndIdServerRpc(
                    PlayerPrefsManager.GetPlayerName(),
                    AuthenticationService.Instance.PlayerId,
                    new RpcParams()
                );
            }
        }

        private void HostOnClientDisconnectCallbackAction(ulong clientId) {
            if (clientId == NetworkManager.Singleton.LocalClientId) return;
            var playerDataIndex = GetPlayerDataIndex(clientId);
            if (playerDataIndex == -1) return;
            _playerDataList.RemoveAt(playerDataIndex);
        }


        private void OnPlayerDataListChangedAction(NetworkListEvent<PlayerData> changeEvent) {
            OnPlayerDataListChanged?.Invoke(this, EventArgs.Empty);
        }


        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void ChangePlayerNameAndIdServerRpc(
            string playerName,
            string playerId,
            RpcParams rpcParams
        ) {
            var playerDataIndex = GetPlayerDataIndex(rpcParams.Receive.SenderClientId);
            var playerData = _playerDataList[playerDataIndex];
            playerData.Name = playerName;
            playerData.PlayerId = playerId;
            _playerDataList[playerDataIndex] = playerData;
        }


        private int GetPlayerDataIndex(ulong clientId) {
            for (var i = 0; i < _playerDataList.Count; i++) {
                if (_playerDataList[i].ClientId == clientId) {
                    return i;
                }
            }
            return -1;
        }
    }
}
