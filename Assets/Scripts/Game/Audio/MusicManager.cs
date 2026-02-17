using Common;
using UnityEngine;
using Logger = Common.Logger;

namespace Game.Audio {
    /// <summary>
    /// Manages playing the music and modifying its volume.
    /// </summary>
    /// <remarks>This class is singleton.</remarks>
    [RequireComponent(typeof(AudioSource))]
    public class MusicManager : MonoBehaviour {
        public static MusicManager Instance { get; private set; }


        [SerializeField, Tooltip("The victory music")]
        private AudioClip victoryMusic;


        /// <summary>
        /// Adjusts the volume of music, configurable by the player in the options menu.
        /// </summary>
        private float _volume;

        private AudioSource _audioSource;


        /// <returns>Music volume</returns>
        public float GetVolume() {
            return _volume;
        }

        /// <summary>
        /// Sets music volume.
        /// </summary>
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
            Logger.LogInitializingInstance(this);
            if (Instance != null) {
                Logger.LogMultipleInstancesError(this);
                Destroy(gameObject);
                return;
            }
            Instance = this;
            Logger.LogInstanceInitialized(this);

            _audioSource = GetComponent<AudioSource>();

            UpdateVolume();
        }


        private void UpdateVolume() {
            _volume = PlayerPrefsManager.GetMusicVolume(defaultValue: 0.5f);
            _audioSource.volume = _volume;
        }
    }
}
