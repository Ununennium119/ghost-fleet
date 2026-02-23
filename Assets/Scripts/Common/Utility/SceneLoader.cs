using Unity.Netcode;
using UnityEngine.SceneManagement;

namespace Common.Utility {
    public static class SceneLoader {
        public enum Scene {
            MainMenuScene,
            LoadingScene,
            GameScene,
            LobbyScene,
            WaitingScene
        }


        private static Scene _targetScene;


        public static void LoadScene(Scene scene) {
            _targetScene = scene;
            SceneManager.LoadScene(nameof(Scene.LoadingScene));
        }

        public static void LoadNetworkScene(Scene scene) {
            NetworkManager.Singleton.SceneManager.LoadScene(scene.ToString(), LoadSceneMode.Single);
        }

        /// <summary>
        /// This method is called after loading scene is load to load the actual target scene.
        /// </summary>
        public static void LoadSceneCallback() {
            SceneManager.LoadScene(_targetScene.ToString());
        }

        public static bool IsSceneActive(Scene scene) {
            return SceneManager.GetActiveScene().name == scene.ToString();
        }
    }
}
