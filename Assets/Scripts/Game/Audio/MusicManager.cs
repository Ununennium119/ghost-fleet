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
        
        
        private AudioSource _audioSource;


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
        }


        public void PlayVictoryMusic() {
            _audioSource.Stop();
            _audioSource.loop = false;
            _audioSource.clip = victoryMusic;
            _audioSource.volume = 0.5f;
            _audioSource.Play();
        }
    }
}
