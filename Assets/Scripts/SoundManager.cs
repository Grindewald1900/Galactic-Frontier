using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager instance { get; private set; }
    [SerializeField] private AudioClip[] clips;
    public AudioClip bgmClip;
    // private AudioSource audioSource;
    private AudioSource backgroundMusic;


    void Awake()
    {
        instance = this;
        // audioSource = GetComponent<AudioSource>();
        backgroundMusic = GetComponent<AudioSource>();
    }

    void Start()
    {
        if (bgmClip != null)
        {
            backgroundMusic.clip = bgmClip;
        }
        backgroundMusic.loop = true;
        PlayBGM();
    }

    public void PlaySound(AudioClip clip)
    {
        backgroundMusic.PlayOneShot(clip);
    }

    public void PlayBGM()
    {
        if (!backgroundMusic.isPlaying)
        {
            backgroundMusic.Play();
        }
    }

    public void PauseBGM()
    {
        if (backgroundMusic.isPlaying)
        {
            backgroundMusic.Pause();
        }
    }

}
