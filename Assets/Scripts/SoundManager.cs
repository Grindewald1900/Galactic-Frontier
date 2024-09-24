using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;
    public AudioClip bgmClip;
    public AudioSource soundEffect;
    public AudioSource backgroundMusic;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    void Start()
    {
        PlaySound(SoundType.BGM, bgmClip);
    }

    public void PlaySound(SoundType type, AudioClip clip)
    {
        AudioSource audioSource = null;
        if (null == clip)
        {
            return;
        }
        if (type == SoundType.BGM)
        {
            audioSource = backgroundMusic;
            audioSource.loop = true;
        }
        if (type == SoundType.SOUND_EFFECT)
        {
            audioSource = soundEffect;
            audioSource.loop = false;
        }
        if (type == SoundType.LOOP_SOUND_EFFECT)
        {
            audioSource = soundEffect;
            audioSource.loop = true;
            audioSource.pitch = 2;
        }

        if (!audioSource.isPlaying)
        {
            audioSource.clip = clip;
            audioSource.Play();
        }
    }

    public void PauseSound(SoundType type)
    {
        AudioSource audioSource = null;

        switch (type)
        {
            case SoundType.BGM:
                audioSource = backgroundMusic;
                break;

            case SoundType.SOUND_EFFECT:
                audioSource = soundEffect;
                break;

            case SoundType.LOOP_SOUND_EFFECT:
                audioSource = soundEffect;
                break;
        }

        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Pause();
        }
    }

    public enum SoundType
    {
        BGM,
        SOUND_EFFECT,
        LOOP_SOUND_EFFECT
    }

}
