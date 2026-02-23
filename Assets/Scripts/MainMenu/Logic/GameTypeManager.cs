using UnityEngine;
using Logger = Common.Utility.Logger;

namespace MainMenu.Logic {
    /// <remarks>This class is singleton.</remarks>
    public class GameTypeManager : MonoBehaviour {
        public static GameTypeManager Instance { get; private set; }


        public enum GameType {
            Offline,
            Online
        }


        private GameType _gameType;


        public void SetGameType(GameType value) {
            _gameType = value;
        }

        public bool IsOffline() {
            return _gameType == GameType.Offline;
        }

        public bool IsOnline() {
            return _gameType == GameType.Online;
        }


        private void Awake() {
            InitializeSingleton();
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
    }
}
