using UnityEngine;

namespace Game.ScriptableObjects {
    /// <summary>
    /// Contains lists of audio clips for different sound effects.
    /// </summary>
    [CreateAssetMenu(menuName = "Scriptable Object/Audio Clips")]
    public class AudioClipsSO : ScriptableObject {
        public AudioClip[] attackAudioClips;
    }
}
