using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public AudioSource audioSource; // El componente AudioSource
    public AudioClip goalSound;     // Sonido al juntar una caja con la meta
    public AudioClip playSound;     // Sonido al darle a Play
    public AudioClip levelCompleteSound; // Sonido al completar el nivel

    // Reproduce un sonido específico
    public void PlaySound(AudioClip clip)
    {
        if (clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}
