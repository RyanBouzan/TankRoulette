using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileAudioControler : MonoBehaviour
{
    public AudioSource audioSource;

    public List<AudioClip> audioClips;

    public void PlayClip(string name)
    {
        AudioClip clip = audioClips.Find(x => x.name == name);
        if (clip != null)
        {
            GameObject audioObject = new GameObject("TempAudio");
            audioObject.transform.position = transform.position;
            audioObject.transform.SetParent(null);

            AudioSource tempAudioSource = audioObject.AddComponent<AudioSource>();
            tempAudioSource.clip = clip;
            tempAudioSource.Play();

            float destroyDelay = clip.length + 1f;
            Destroy(audioObject, destroyDelay);
        }
        else
        {
            Debug.LogWarning("audio clip not found!");
            return; 
        }
    }
}
