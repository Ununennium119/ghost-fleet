using Game.Audio;
using Game.Manager;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI {
    public class PauseMenuUI : MonoBehaviour {
        [SerializeField, Tooltip("The music slider")] [Required]
        private Slider musicSlider;
        [SerializeField, Tooltip("The sound effects slider")] [Required]
        private Slider sfxSlider;

        [SerializeField, Tooltip("The back button")] [Required]
        private Button backButton;


        private GameManager _gameManager;
        private MusicManager _musicManager;
        private SoundEffectsManager _soundEffectsManager;


        private void Awake() {
            musicSlider.onValueChanged.AddListener(ChangeMusicVolume);
            sfxSlider.onValueChanged.AddListener(ChangeSoundEffectsVolume);

            backButton.onClick.AddListener(Back);
        }

        private void Start() {
            _gameManager = GameManager.Instance;
            _musicManager = MusicManager.Instance;
            _soundEffectsManager = SoundEffectsManager.Instance;

            _gameManager.OnPauseToggled += OnPauseToggledAction;

            musicSlider.value = _musicManager.GetVolume();
            sfxSlider.value = _soundEffectsManager.GetVolume();
            
            gameObject.SetActive(false);
        }

        
        private void OnPauseToggledAction(object sender, GameManager.OnPauseToggledArgs e) {
            gameObject.SetActive(e.IsGamePaused);
        }

        private void ChangeMusicVolume(float value) {
            _musicManager.SetVolume(value);
        }

        private void ChangeSoundEffectsVolume(float value) {
            _soundEffectsManager.SetVolume(value);
        }

        private void Back() {
            _gameManager.TogglePause();
            gameObject.SetActive(false);
        }
    }
}
