using System;
using UnityEngine;

namespace Suika
{
    /// <summary>
    /// 효과음을 코드로 직접 합성한다(외부 사운드 파일 없이 동작).
    /// 소리마다 피치를 따로 주기 위해 AudioSource 여러 개를 돌려 쓴다.
    /// </summary>
    public class SuikaAudio : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] float volume = 0.6f;

        const int SampleRate = 44100;
        const int VoiceCount = 6;

        AudioSource[] voices;
        int nextVoice;
        AudioClip dropClip;
        AudioClip popClip;
        AudioClip bigPopClip;
        AudioClip gameOverClip;

        void Awake()
        {
            voices = new AudioSource[VoiceCount];
            for (int i = 0; i < VoiceCount; i++)
            {
                var src = gameObject.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = 0f;
                voices[i] = src;
            }

            // 떨어뜨릴 때: 낮게 '툭'
            dropClip = Synth("drop", 0.09f, t => Mathf.Lerp(230f, 120f, t / 0.09f), t => Mathf.Exp(-t * 38f), 0.15f);
            // 합쳐질 때: 위로 올라가는 '뽁'
            popClip = Synth("pop", 0.14f, t => Mathf.Lerp(480f, 920f, t / 0.14f), t => Attack(t, 0.004f) * Mathf.Exp(-t * 24f), 0f);
            // 큰 과일: 더 낮고 길게
            bigPopClip = Synth("bigpop", 0.28f, t => Mathf.Lerp(260f, 620f, t / 0.28f), t => Attack(t, 0.006f) * Mathf.Exp(-t * 11f), 0.05f);
            // 게임 오버: 내려가는 소리
            gameOverClip = Synth("gameover", 0.8f, t => Mathf.Lerp(440f, 110f, t / 0.8f), t => 1f - t / 0.8f, 0f);
        }

        static float Attack(float t, float time) => Mathf.Clamp01(t / time);

        static AudioClip Synth(string clipName, float duration, Func<float, float> freq, Func<float, float> env, float noise)
        {
            int n = Mathf.CeilToInt(duration * SampleRate);
            var data = new float[n];
            var rng = new System.Random(clipName.GetHashCode());
            double phase = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                // 주파수가 변해도 끊기지 않게 위상을 누적한다.
                phase += 2.0 * Math.PI * freq(t) / SampleRate;
                float s = (float)(Math.Sin(phase) + 0.3 * Math.Sin(phase * 2.0));
                s += noise * ((float)rng.NextDouble() * 2f - 1f);
                data[i] = s * env(t) * 0.6f;
            }
            var clip = AudioClip.Create(clipName, n, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        void Play(AudioClip clip, float pitch, float vol)
        {
            if (clip == null || voices == null) return;
            var src = voices[nextVoice];
            nextVoice = (nextVoice + 1) % voices.Length;
            src.pitch = pitch;
            src.PlayOneShot(clip, vol * volume);
        }

        public void PlayDrop() => Play(dropClip, UnityEngine.Random.Range(0.95f, 1.05f), 0.8f);

        public void PlayMerge(int level)
        {
            // 큰 과일일수록 낮은 소리
            float pitch = Mathf.Clamp(1.35f - level * 0.07f, 0.6f, 1.5f);
            Play(level >= 7 ? bigPopClip : popClip, pitch, 1f);
        }

        public void PlayGameOverPop() => Play(popClip, UnityEngine.Random.Range(0.8f, 1.2f), 0.45f);

        public void PlayGameOver() => Play(gameOverClip, 1f, 0.9f);
    }
}
