using Common.Utility;
using UnityEngine;
using Logger = Common.Utility.Logger;

namespace Game.Audio {
    /// <remarks>This class is singleton.</remarks>
    [RequireComponent(typeof(AudioSource))]
    public class MusicManager : MonoBehaviour {
        private const float DEFAULT_MUSIC_VOLUME = 0.5f;
        
        
        public static MusicManager Instance { get; private set; }


        [SerializeField, Tooltip("The victory music")]
        private AudioClip victoryMusic;


        private float _volume;

        private AudioSource _audioSource;


        public float GetVolume() {
            return _volume;
        }

        public void SetVolume(float value) {
            _volume = value;
            _audioSource.volume = _volume;
            PlayerPrefsManager.SetMusicVolume(_volume);
        }

        public void PlayVictoryMusic() {
            _audioSource.Stop();
            _audioSource.loop = false;
            _audioSource.clip = victoryMusic;
            _audioSource.Play();
        }


        private void Awake() {
            InitializeSingleton();
            CacheReferences();
            UpdateVolume();
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

        private void CacheReferences() {
            _audioSource = GetComponent<AudioSource>();
        }

        private void UpdateVolume() {
            _volume = PlayerPrefsManager.GetMusicVolume(defaultValue: DEFAULT_MUSIC_VOLUME);
            _audioSource.volume = _volume;
        }
    }
}
