using UnityEngine;

namespace VN.Controllers
{
    public class AudioController : MonoBehaviour
    {
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource voiceSource;

        private void Awake()
        {
            EnsureSources();
        }

        public void PlayBgm(AudioClip clip)
        {
            EnsureSources();
            if (bgmSource == null || clip == null) return;
            bgmSource.clip = clip;
            bgmSource.loop = true;
            bgmSource.Play();
        }

        public void StopBgm()
        {
            EnsureSources();
            bgmSource?.Stop();
        }

        public void PlaySfx(AudioClip clip)
        {
            EnsureSources();
            if (sfxSource == null || clip == null) return;
            sfxSource.PlayOneShot(clip);
        }

        public void PlayVoice(AudioClip clip)
        {
            EnsureSources();
            if (voiceSource == null || clip == null) return;
            voiceSource.Stop();
            voiceSource.clip = clip;
            voiceSource.loop = false;
            voiceSource.Play();
        }

        private void EnsureSources()
        {
            if (bgmSource == null)
            {
                bgmSource = gameObject.GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
                bgmSource.playOnAwake = false;
            }

            if (sfxSource == null)
            {
                var go = new GameObject("SfxSource");
                go.transform.SetParent(transform, false);
                sfxSource = go.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
            }

            if (voiceSource == null)
            {
                var go = new GameObject("VoiceSource");
                go.transform.SetParent(transform, false);
                voiceSource = go.AddComponent<AudioSource>();
                voiceSource.playOnAwake = false;
            }
        }
    }
}