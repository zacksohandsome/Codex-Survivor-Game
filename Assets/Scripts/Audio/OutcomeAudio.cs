using UnityEngine;

namespace Survivor.Audio
{
    public sealed class OutcomeAudio : System.IDisposable
    {
        private readonly AudioSource source;
        private readonly AudioClip victory, defeat;
        public OutcomeAudio(GameObject owner)
        {
            source = owner.AddComponent<AudioSource>(); source.playOnAwake = false; source.volume = 0.25f;
            victory = Create("M3 Victory", 440, 880); defeat = Create("M3 Defeat", 330, 110);
        }
        private static AudioClip Create(string name, float from, float to)
        {
            const int sampleRate = 22050; const float duration = 0.3f;
            var samples = new float[(int)(sampleRate * duration)]; float phase = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (float)i / samples.Length;
                phase += 2 * Mathf.PI * Mathf.Lerp(from, to, t) / sampleRate;
                samples[i] = Mathf.Sin(phase) * Mathf.Sin(Mathf.PI * t) * 0.4f;
            }
            var clip = AudioClip.Create(name, samples.Length, 1, sampleRate, false); clip.SetData(samples, 0); return clip;
        }
        public void Play(bool won) { source.PlayOneShot(won ? victory : defeat); }
        public void Dispose() { Object.Destroy(victory); Object.Destroy(defeat); }
    }
}
