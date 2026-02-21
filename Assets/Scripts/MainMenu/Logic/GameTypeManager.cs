using UnityEngine;
using Logger = Common.Utility.Logger;

namespace MainMenu.Logic {
    /// <summary>This class is responsible for managing game type.</summary>
    /// <remarks>This class is singleton.</remarks>
    public class GameTypeManager : MonoBehaviour {
        public static GameTypeManager Instance { get; private set; }


        public enum GameType {
            Offline,
            Online
        }


        private GameType _gameType;


        public GameType GetGameType() {
            return _gameType;
        }

        public void SetGameType(GameType value) {
            _gameType = value;
        }


        private void Awake() {
            Logger.LogInitializingInstance(this);
            if (Instance != null) {
                Logger.LogMultipleInstancesError(this);
                Destroy(gameObject);
                return;
            }
            Instance = this;
            Logger.LogInstanceInitialized(this);

            DontDestroyOnLoad(gameObject);
        }
    }
}
