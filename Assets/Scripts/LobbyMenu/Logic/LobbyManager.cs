using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Logic;
using Common.Utility;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using Logger = Common.Utility.Logger;

namespace LobbyMenu.Logic {
    public class LobbyManager : MonoBehaviour {
        private const float MAX_HEARTBEAT_TIMER = 15f;
        private const string RELAY_JOIN_CODE_KEY = "RelayJoinCode";


        public static LobbyManager Instance { get; private set; }


        [SerializeField, Tooltip("The network connection type")]
        private ConnectionType connectionType;

        [SerializeField, Tooltip("Specifies whether to use relay")]
        private bool useRelay = false;


        public event EventHandler OnCreateLobbyStarted;

        public event EventHandler OnCreateLobbyFailed;

        public event EventHandler OnJoinLobbyStarted;

        public event EventHandler OnJoinLobbyFailed;

        public event EventHandler OnQuickJoinNotFound;

        public event EventHandler<OnLobbyListRefreshedEventArgs> OnLobbyListRefreshed;
        public class OnLobbyListRefreshedEventArgs : EventArgs {
            public List<Lobby> LobbyList;
        }


        private Lobby _joinedLobby;
        private float _heartbeatTimer = MAX_HEARTBEAT_TIMER;

        private ILobbyService _lobbyService;


        public Lobby GetJoinedLobby() {
            return _joinedLobby;
        }

        public async void CreateLobby(string lobbyName, bool isPrivate) {
            try {
                OnCreateLobbyStarted?.Invoke(this, EventArgs.Empty);

                _joinedLobby = await _lobbyService.CreateLobbyAsync(
                    lobbyName: lobbyName,
                    maxPlayers: MultiplayerManager.MAX_PLAYER_COUNT,
                    options: new CreateLobbyOptions {
                        IsPrivate = isPrivate
                    }
                );

                if (useRelay) {
                    var relayAllocation = await AllocateRelay();
                    var relayJoinCode = await GetRelayJoinCode(relayAllocation);
                    await _lobbyService.UpdateLobbyAsync(_joinedLobby.Id, new UpdateLobbyOptions {
                        Data = new Dictionary<string, DataObject> {
                            {
                                RELAY_JOIN_CODE_KEY, new DataObject(
                                    DataObject.VisibilityOptions.Member,
                                    relayJoinCode
                                )
                            }
                        }
                    });
                    NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(
                        relayAllocation.ToRelayServerData(connectionType.GetValue())
                    );
                }

                MultiplayerManager.Instance.StartHost();
                SceneLoader.LoadNetworkScene(SceneLoader.Scene.WaitingScene);
            } catch (Exception e) {
                Debug.LogError(e);
                OnCreateLobbyFailed?.Invoke(this, EventArgs.Empty);
            }
        }

        public async void JoinLobbyByCode(string lobbyCode) {
            try {
                OnJoinLobbyStarted?.Invoke(this, EventArgs.Empty);

                _joinedLobby = await _lobbyService.JoinLobbyByCodeAsync(lobbyCode);

                if (useRelay) {
                    await JoinRelay();
                }

                MultiplayerManager.Instance.StartClient();
                SceneLoader.LoadNetworkScene(SceneLoader.Scene.WaitingScene);
            } catch (Exception e) {
                Debug.LogError(e);
                OnJoinLobbyFailed?.Invoke(this, EventArgs.Empty);
            }
        }

        public async void JoinLobbyById(string lobbyId) {
            try {
                OnJoinLobbyStarted?.Invoke(this, EventArgs.Empty);

                _joinedLobby = await _lobbyService.JoinLobbyByIdAsync(lobbyId);

                if (useRelay) {
                    await JoinRelay();
                }

                MultiplayerManager.Instance.StartClient();
            } catch (Exception e) {
                Debug.LogError(e);
                OnJoinLobbyFailed?.Invoke(this, EventArgs.Empty);
            }
        }

        public async void QuickJoinLobby() {
            try {
                OnJoinLobbyStarted?.Invoke(this, EventArgs.Empty);

                _joinedLobby = await _lobbyService.QuickJoinLobbyAsync();

                if (useRelay) {
                    await JoinRelay();
                }

                MultiplayerManager.Instance.StartClient();
                SceneLoader.LoadNetworkScene(SceneLoader.Scene.WaitingScene);
            } catch (LobbyServiceException e) {
                if (e.Reason == LobbyExceptionReason.NoOpenLobbies) {
                    OnQuickJoinNotFound?.Invoke(this, EventArgs.Empty);
                } else {
                    Debug.LogError(e);
                    OnJoinLobbyFailed?.Invoke(this, EventArgs.Empty);
                }
            } catch (Exception e) {
                Debug.LogError(e);
                OnJoinLobbyFailed?.Invoke(this, EventArgs.Empty);
            }
        }

        public async void DeleteLobby() {
            try {
                if (_joinedLobby == null) return;

                await _lobbyService.DeleteLobbyAsync(_joinedLobby.Id);
                _joinedLobby = null;
            } catch (Exception e) {
                Debug.LogError(e);
            }
        }

        public async void LeaveLobby() {
            try {
                if (_joinedLobby == null) return;

                await _lobbyService.RemovePlayerAsync(_joinedLobby.Id, AuthenticationService.Instance.PlayerId);
                _joinedLobby = null;
            } catch (Exception e) {
                Debug.LogError(e);
            }
        }

        public async void RefreshLobbyList() {
            try {
                var options = new QueryLobbiesOptions {
                    Filters = new List<QueryFilter> {
                        new(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
                    }
                };
                var response = await _lobbyService.QueryLobbiesAsync(options);
                OnLobbyListRefreshed?.Invoke(this, new OnLobbyListRefreshedEventArgs {
                    LobbyList = response.Results
                });
            } catch (Exception e) {
                Debug.LogError(e);
            }
        }


        private void Awake() {
            InitializeSingleton();
            InitializeLobby();
            ResolveSingletons();
            DontDestroyOnLoad(gameObject);
        }

        private void Update() {
            if (IsLobbyHost()) {
                HandleHeartbeat();
            }
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
            _lobbyService = LobbyService.Instance;
            Debug.Log(_lobbyService);
        }

        private async void InitializeLobby() {
            try {
                await InitializeUnityAuthentication();
                RefreshLobbyList();
            } catch (Exception e) {
                Debug.LogError(e);
            }
        }

        private bool IsLobbyHost() {
            if (_joinedLobby == null) return false;
            return _joinedLobby.HostId == AuthenticationService.Instance.PlayerId;
        }

        private void HandleHeartbeat() {
            _heartbeatTimer -= Time.deltaTime;
            if (_heartbeatTimer <= 0) {
                _heartbeatTimer = MAX_HEARTBEAT_TIMER;
                _lobbyService.SendHeartbeatPingAsync(_joinedLobby.Id);
            }
        }


        private async Task InitializeUnityAuthentication() {
            try {
                if (UnityServices.State != ServicesInitializationState.Initialized) {
                    var options = new InitializationOptions();
                    await UnityServices.InitializeAsync(options);
                }
                if (!AuthenticationService.Instance.IsSignedIn) {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                }
            } catch (Exception e) {
                Debug.LogError(e);
            }
        }

        private async Task<Allocation> AllocateRelay() {
            try {
                var allocation = await RelayService.Instance.CreateAllocationAsync(
                    MultiplayerManager.MAX_PLAYER_COUNT - 1
                );
                return allocation;
            } catch (Exception e) {
                Debug.LogError(e);
            }
            return null;
        }

        private async Task<string> GetRelayJoinCode(Allocation allocation) {
            try {
                var joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
                return joinCode;
            } catch (Exception e) {
                Debug.LogError(e);
            }
            return null;
        }

        private async Task JoinRelay() {
            try {
                var joinCode = _joinedLobby.Data[RELAY_JOIN_CODE_KEY].Value;
                var joinAllocation = await JoinAllocation(joinCode);
                NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(
                    joinAllocation.ToRelayServerData(connectionType.GetValue())
                );
            } catch (Exception e) {
                Debug.LogError(e);
            }
        }

        private async Task<JoinAllocation> JoinAllocation(string joinCode) {
            try {
                var joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
                return joinAllocation;
            } catch (Exception e) {
                Debug.LogError(e);
            }
            return null;
        }
    }
}
