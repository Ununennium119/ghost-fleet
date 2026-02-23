using System;
using Common.Utility;
using Game.ScriptableObjects;
using UnityEngine;
using Logger = Common.Utility.Logger;
using Random = UnityEngine.Random;

namespace Game.Audio {
    /// <remarks>This class is singleton.</remarks>
    public class SoundEffectsManager : MonoBehaviour {
        private const float DEFAULT_SOUND_EFFECTS_VOLUME = 0.5f;


        public static SoundEffectsManager Instance { get; private set; }


        [SerializeField, Tooltip("Audio clips are stored in this scriptable object.")]
        private AudioClipsSO audioClipsSO;


        private float _volumeMultiplier;

        private Vector3? _cameraPosition;


        public float GetVolume() {
            return _volumeMultiplier;
        }

        public void SetVolume(float value) {
            _volumeMultiplier = value;
            PlayerPrefsManager.SetSoundEffectsVolume(_volumeMultiplier);
        }


        private void Awake() {
            InitializeSingleton();
            CacheReferences();
            UpdateVolumeMultiplier();
        }

        private void Start() {
            SubscribeToEvents();
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
            _cameraPosition = Camera.main?.transform.position;
        }

        private void SubscribeToEvents() {
            Cell.OnAnyAttack += CellOnAnyAttackAction;
        }


        private void CellOnAnyAttackAction(object sender, EventArgs e) {
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
