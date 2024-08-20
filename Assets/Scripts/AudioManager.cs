using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public AudioClip Complete;
    public AudioClip Move;
    public AudioClip Scale;
    public AudioClip Select;

    public AudioSource Source;
    public static AudioManager Instance;

    private void Awake()
    {
        Instance = this;
    }

    public void PlaySfx(AudioClip clip){
        Source.PlayOneShot(clip);
    }
}
