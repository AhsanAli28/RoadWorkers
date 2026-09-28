using Spirit604.Extensions;
using System;
using UnityEngine;

namespace Spirit604.DotsCity.Core.Sound
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public class AudioSourceBehaviour : MonoBehaviour
    {
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private Transform _transform;
        [SerializeField] private bool drawGizmos;

        private float baseVolume = 1f;
        private float sourceClipVolume = 1f;
        private float currentClipVolume = 1f;
        private SoundData currentData;
        private AudioClip currentClip;

        public float Length => currentClip?.length ?? 0;
        public float DisableTime { get; set; }

        public Transform Transform => _transform;

        public float BaseVolume
        {
            get => baseVolume;
            set
            {
                baseVolume = value;
                SetVolume(currentClipVolume);
            }
        }

        public event Action<AudioSourceBehaviour> OnDisabled = delegate { };

        private void OnDisable()
        {
            OnDisabled(this);
        }

        public void Play()
        {
            audioSource.Play();
        }

        public void Replay()
        {
            if (!currentData) return;

            if (!audioSource.loop)
            {
                var clip = currentData.GetClip();
                PlayOneShot(clip);
            }
            else
            {
                Play();
            }
        }

        public void Stop()
        {
            audioSource.Stop();
        }

        public void StopFadeout()
        {
            Stop();
        }

        public bool SetSound(SoundData soundData, bool autoPlay = true, bool loop = false)
        {
            audioSource.loop = loop;

            var clip = soundData.GetClip();

            if (clip != null)
            {
                sourceClipVolume = soundData.ClipVolume;
                audioSource.clip = clip;
                currentData = soundData;
                currentClip = audioSource.clip;
                ResetVolume();

                if (autoPlay)
                {
                    Play();
                }

                return true;
            }

            Debug.LogError($"AudioSourceBehaviour. Sound '{soundData.name}' clip is null.");

            return false;
        }

        public bool PlayOneShot(SoundData soundData, Vector3 position)
        {
            var clip = soundData.GetClip();

            if (clip != null)
            {
                sourceClipVolume = soundData.ClipVolume;
                currentData = soundData;
                Transform.position = position;
                ResetVolume();
                PlayOneShot(clip);
                return true;
            }

            Debug.LogError($"AudioSourceBehaviour. Sound '{soundData.name}' clip is null.");
            return false;
        }

        public bool PlayOneShot(AudioClip clip)
        {
            if (clip != null)
            {
                currentClip = clip;
                audioSource.PlayOneShot(clip);
                return true;
            }

            return false;
        }

        public void SetVolume(float volume)
        {
            currentClipVolume = volume;
            audioSource.volume = volume * baseVolume * sourceClipVolume;
        }

        public void SetPitch(float pitch)
        {
            audioSource.pitch = pitch;
        }

        public void SetMute(bool isMute)
        {
            if (audioSource.mute != isMute)
                audioSource.mute = isMute;
        }

        public void Release(bool returnToPool = true)
        {
            Stop();
            currentClip = null;
            currentData = null;
            audioSource.clip = null;

            if (returnToPool)
                gameObject.ReturnToPool();
        }

        private void ResetVolume()
        {
            SetVolume(BaseVolume);
        }

        private void Reset()
        {
            audioSource = GetComponent<AudioSource>();
            EditorSaver.SetObjectDirty(this);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!drawGizmos) return;

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 10f);
        }
#endif
    }
}
