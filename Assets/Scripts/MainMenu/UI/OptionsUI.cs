using Game.Audio;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MainMenu.UI {
    public class OptionsUI : MonoBehaviour {
        [Header("Sliders")]
        [SerializeField, Tooltip("The music slider")] [Required]
        private Slider musicSlider;
        [SerializeField, Tooltip("The sound effects slider")] [Required]
        private Slider sfxSlider;

        [Header("Buttons")]
        [SerializeField, Tooltip("The back button")] [Required]
        private Button backButton;


        private MusicManager _musicManager;
        private SoundEffectsManager _soundEffectsManager;


        public void Show() {
            gameObject.SetActive(true);
        }


        private void Awake() {
            AddSliderListeners();
            AddButtonListeners();
        }

        private void Start() {
            ResolveSingletons();
            UpdateSliderValues();
            Hide();
        }


        private void AddSliderListeners() {
            musicSlider.onValueChanged.AddListener(ChangeMusicVolume);
            sfxSlider.onValueChanged.AddListener(ChangeSoundEffectsVolume);
        }

        private void AddButtonListeners() {
            backButton.onClick.AddListener(() => {
                EventSystem.current.SetSelectedGameObject(null);
                Hide();
            });
        }
        
        private void ResolveSingletons() {
            _musicManager = MusicManager.Instance;
            _soundEffectsManager = SoundEffectsManager.Instance;
        }

        private void UpdateSliderValues() {
            musicSlider.value = _musicManager.GetVolume();
            sfxSlider.value = _soundEffectsManager.GetVolume();
        }

        private void Hide() {
            gameObject.SetActive(false);
        }

        private void ChangeMusicVolume(float value) {
            _musicManager.SetVolume(value);
        }

        private void ChangeSoundEffectsVolume(float value) {
            _soundEffectsManager.SetVolume(value);
        }
    }
}
