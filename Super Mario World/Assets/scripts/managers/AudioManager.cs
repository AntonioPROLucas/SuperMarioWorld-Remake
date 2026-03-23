using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class Sound
{
    public string name; 
    public AudioClip clip; 
    [Range(0f, 1f)]
    public float volume = 1f; 
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [Header("Tocadores de Áudio")]
    public AudioSource sfxSource;
    public AudioSource bgmSource; 
    public AudioSource yoshiDrumsSource;
    [Header("Lista de Sons")]
    public Sound[] sounds;

    private Dictionary<string, Sound> soundDictionary = new Dictionary<string, Sound>();

    void Awake()
    {
        transform.SetParent(null); // Solta da pasta para o DontDestroy funcionar

        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); 
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        foreach (Sound s in sounds)
        {
            soundDictionary[s.name] = s;
        }
    }

    public void PlaySFX(string soundName)
    {
        if (soundDictionary.TryGetValue(soundName, out Sound s))
        {
            sfxSource.PlayOneShot(s.clip, s.volume);
        }
    }

    // 🎵 NOVAS FUNÇÕES DE MÚSICA
    public void StopBGM()
    {
        if (bgmSource != null) bgmSource.Stop();
    }

    public void RestartBGM()
    {
        if (bgmSource != null)
        {
            bgmSource.Stop(); // Para caso já esteja tocando
            bgmSource.Play(); // Começa do zero
        }
    }

    public void SetYoshiDrums(bool isRiding)
    {
        if (yoshiDrumsSource != null)
        {
            // O volume sobe apenas para 0.15 para acompanhar o piano sem ofuscar!
            yoshiDrumsSource.volume = isRiding ? 0.15f : 0f; 
        }
    }
}