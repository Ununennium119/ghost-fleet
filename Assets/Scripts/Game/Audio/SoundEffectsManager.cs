using System;
using Game.Manager;
using Game.ScriptableObjects;
using UnityEngine;
using Logger = Common.Logger;
using Random = UnityEngine.Random;

namespace Game.Audio {
    /// <summary>
    /// Manages playing the sound effects and modifying their volume.
    /// </summary>
    /// <remarks>This class is singleton.</remarks>
    public class SoundEffectsManager : MonoBehaviour {
        private static SoundEffectsManager Instance { get; set; }


        [SerializeField, Tooltip("Audio clips are stored in this scriptable object.")]
        private AudioClipsSO audioClipsSO;


        private GameManager _gameManager;
        
        private Vector3? _cameraPosition;


        private void Awake() {
            Logger.LogInitializingInstance(this);
            if (Instance != null) {
                Logger.LogMultipleInstancesError(this);
                Destroy(gameObject);
                return;
            }
            Instance = this;
            Logger.LogInstanceInitialized(this);
            
            _cameraPosition = Camera.main?.transform.position;
        }

        private void Start() {
            _gameManager = GameManager.Instance;

            _gameManager.OnAttack += PlayAttackAudioClip;
        }


        /// <remarks>
        /// Invoked when the <see cref="DeliveryManager.OnDeliverySuccess"/> event is triggered.
        /// </remarks>
        private void PlayAttackAudioClip(object sender, EventArgs e) {
            var position = _cameraPosition ?? gameObject.transform.position;
            PlaySound(audioClipsSO.attackAudioClips, position);
        }

        private void PlaySound(AudioClip[] clip, Vector3 position, float volume = 0.5f) {
            var selectedClip = clip[Random.Range(0, clip.Length)];
            PlaySound(selectedClip, position, volume);
        }

        private void PlaySound(AudioClip clip, Vector3 position, float volume = 0.5f) {
            AudioSource.PlayClipAtPoint(clip, new Vector3(0, 20, 0), volume);
        }
    }
}
