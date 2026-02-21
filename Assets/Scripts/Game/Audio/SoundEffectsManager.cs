using System;
using Common.Utility;
using Game.Manager;
using Game.ScriptableObjects;
using UnityEngine;
using Logger = Common.Utility.Logger;
using Random = UnityEngine.Random;

namespace Game.Audio {
    /// <summary>
    /// Manages playing the sound effects and modifying their volume.
    /// </summary>
    /// <remarks>This class is singleton.</remarks>
    public class SoundEffectsManager : MonoBehaviour {
        private const float DEFAULT_SOUND_EFFECTS_VOLUME = 0.5f;


        public static SoundEffectsManager Instance { get; private set; }


        [SerializeField, Tooltip("Audio clips are stored in this scriptable object.")]
        private AudioClipsSO audioClipsSO;


        /// <summary>
        /// Adjusts the volume of sound effects, configurable by the player in the options menu.
        /// </summary>
        private float _volumeMultiplier;

        private Vector3? _cameraPosition;


        /// <returns>Sound effects volume</returns>
        public float GetVolume() {
            return _volumeMultiplier;
        }

        /// <summary>
        /// Increases volume of sound effects by 0.1. If volume is 1, sets it to 0.
        /// </summary>
        public void SetVolume(float value) {
            _volumeMultiplier = value;
            PlayerPrefsManager.SetSoundEffectsVolume(_volumeMultiplier);
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

            _cameraPosition = Camera.main?.transform.position;

            UpdateVolumeMultiplier();
        }

        private void Start() {
            Cell.OnAnyAttack += PlayAttackAudioClip;
        }


        /// <remarks>
        /// Invoked when the <see cref="GameManager.OnAttack"/> event is triggered.
        /// </remarks>
        private void PlayAttackAudioClip(object sender, EventArgs e) {
            var position = _cameraPosition ?? gameObject.transform.position;
            PlaySound(audioClipsSO.attackAudioClips, position);
        }

        private void PlaySound(AudioClip[] clip, Vector3 position, float volume = 1.0f) {
            var selectedClip = clip[Random.Range(0, clip.Length)];
            PlaySound(selectedClip, position, volume);
        }

        private void PlaySound(AudioClip clip, Vector3 position, float volume = 1.0f) {
            AudioSource.PlayClipAtPoint(clip, position, volume * _volumeMultiplier);
        }


        private void UpdateVolumeMultiplier() {
            _volumeMultiplier = PlayerPrefsManager.GetSoundEffectsVolume(defaultValue: DEFAULT_SOUND_EFFECTS_VOLUME);
        }
    }
}
