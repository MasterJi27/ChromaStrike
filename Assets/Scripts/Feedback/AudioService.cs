using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChromaStrike.Feedback
{
    public enum SfxId
    {
        UiClick, UiHover, Pop, Spawn, Batch, GameStart, GameEnd, Tick, EmptyShot
    }

    [Serializable]
    public struct SfxEntry
    {
        public SfxId id;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume;
        public Vector2 pitchRange;
    }

    /// <summary>
    /// Plays one shot sound effects through a small pool of AudioSources, with a little random
    /// pitch so repeated pops do not sound identical. Also owns the looping ambience.
    /// </summary>
    public sealed class AudioService : MonoBehaviour
    {
        [SerializeField] private SfxEntry[] effects;
        [SerializeField] private AudioClip ambience;
        [SerializeField, Range(0f, 1f)] private float ambienceVolume = 0.25f;
        [SerializeField, Min(2)] private int voiceCount = 8;

        private readonly Dictionary<SfxId, SfxEntry> lookup = new Dictionary<SfxId, SfxEntry>();
        private AudioSource[] voices;
        private int nextVoice;

        private void Awake()
        {
            foreach (SfxEntry entry in effects) lookup[entry.id] = entry;

            voices = new AudioSource[voiceCount];
            for (int i = 0; i < voices.Length; i++)
            {
                voices[i] = gameObject.AddComponent<AudioSource>();
                voices[i].playOnAwake = false;
                voices[i].spatialBlend = 0f; // 2D: UI and hit sounds should not be positional
            }

            if (ambience != null)
            {
                AudioSource loop = gameObject.AddComponent<AudioSource>();
                loop.clip = ambience;
                loop.loop = true;
                loop.volume = ambienceVolume;
                loop.spatialBlend = 0f;
                loop.ignoreListenerPause = true;
                loop.Play();
            }
        }

        public void Play(SfxId id)
        {
            if (!lookup.TryGetValue(id, out SfxEntry entry) || entry.clip == null) return;

            AudioSource voice = voices[nextVoice];
            nextVoice = (nextVoice + 1) % voices.Length;
            voice.pitch = UnityEngine.Random.Range(entry.pitchRange.x, entry.pitchRange.y);
            voice.PlayOneShot(entry.clip, entry.volume);
        }
    }
}
