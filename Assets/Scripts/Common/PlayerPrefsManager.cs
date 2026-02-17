using UnityEngine;

namespace Common {
    /// <summary>
    /// This class is responsible for accessing player preferences.
    /// </summary>
    /// <seealso cref="PlayerPrefs"/>
    public static class PlayerPrefsManager {
        private const string PLAYER_PREFS_MUSIC_VOLUME = "MusicVolume";
        private const string PLAYER_PREFS_SOUND_EFFECTS_VOLUME = "SoundEffectsVolume";


        public static float GetMusicVolume(float defaultValue) {
            return PlayerPrefs.GetFloat(PLAYER_PREFS_MUSIC_VOLUME, defaultValue);
        }

        public static void SetMusicVolume(float volume) {
            PlayerPrefs.SetFloat(PLAYER_PREFS_MUSIC_VOLUME, volume);
            PlayerPrefs.Save();
        }


        public static float GetSoundEffectsVolume(float defaultValue) {
            return PlayerPrefs.GetFloat(PLAYER_PREFS_SOUND_EFFECTS_VOLUME, defaultValue);
        }

        public static void SetSoundEffectsVolume(float volume) {
            PlayerPrefs.SetFloat(PLAYER_PREFS_SOUND_EFFECTS_VOLUME, volume);
            PlayerPrefs.Save();
        }
    }
}
