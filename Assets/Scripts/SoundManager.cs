using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;
    public AudioClip bgmClip;
    public AudioSource soundEffect;
    public AudioSource soundEffectLoop;
    public AudioSource backgroundMusic;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    void Start()
    {
        PlaySound(SoundType.BGM, bgmClip);
        backgroundMusic.loop = true;
        soundEffectLoop.loop = true;
        soundEffect.loop = false;
    }

    public void PlaySound(SoundType type, AudioClip clip)
    {
        if (null == clip)
        {
            Debug.Log("SoundManager: PlaySound: clip is null");
            return;
        }
        AudioSource audioSource = GetAudioSource(type);
        audioSource.clip = clip;
        if (type == SoundType.SOUND_EFFECT)
        {
            audioSource.Play();
        }
        else
        {
            if (!audioSource.isPlaying)
            {
                audioSource.Play();
            }
        }
    }

    public void PauseSound(SoundType type)
    {
        AudioSource audioSource = GetAudioSource(type);
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Pause();
        }
    }

    private AudioSource GetAudioSource(SoundType type)
    {
        return type switch
        {
            SoundType.BGM => backgroundMusic,
            SoundType.SOUND_EFFECT => soundEffect,
            SoundType.LOOP_SOUND_EFFECT => soundEffectLoop,
            _ => soundEffect,
        };
    }

    public enum SoundType
    {
        BGM,
        SOUND_EFFECT,
        LOOP_SOUND_EFFECT,
    }

}
