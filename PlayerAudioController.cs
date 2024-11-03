using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using FishNet;
using Unity.VisualScripting;

public class PlayerAudioController : NetworkBehaviour
{
    public AudioSource audioSource;


    public AudioSource engineAudioSource;
    public AudioSource turretAudioSource;

    public List<AudioClip> audioClips;

    public string gunshot_name;
    // Start is called before the first frame update

    public static float maxPitch = 1.0f;
    public static float minPitch = 0.6f;
    public static float maxVolume = .30f;
    public static float minVolume = 0.05f;

    private Coroutine fadeOutCoroutine;
    private Coroutine fadeInCoroutine;


    public override void OnStartClient()
    {
        base.OnStartClient();

        if(!base.IsOwner)
        this.enabled = false;
    }

    public void PlayEngine(float moving, float turning)
    {
        float intensity = Mathf.Max(moving/20f, turning/100f);


        if(intensity > 0.05f)
        {
            if(!engineAudioSource.isPlaying)
            engineAudioSource.Play();
//            Debug.Log("playing audio at: " + moving);
            // Adjust pitch and volume based on speed
            engineAudioSource.pitch = Mathf.Lerp(minPitch, maxPitch, intensity);
            engineAudioSource.volume = Mathf.Lerp(minVolume, maxVolume, intensity);
        }
        else
        {
            engineAudioSource.Stop();
            return;
        }
        
    }

    public void PlayTurret(bool play)
    {
    //    Debug.Log("Playing turret? " + play);
        if (play)
        {
            if (!turretAudioSource.isPlaying)
            {
                turretAudioSource.volume = 0; // Start from 0 volume for fade-in
                turretAudioSource.Play();
                if (fadeInCoroutine != null)
                {
                    StopCoroutine(fadeInCoroutine); // Stop any existing fade-in
                }
                fadeInCoroutine = StartCoroutine(FadeInTurretAudio(10)); // Start fade-in
            }
        }
        else
        {
            if (fadeOutCoroutine != null)
            {
                StopCoroutine(fadeOutCoroutine); // Stop any existing fade-out
            }
            fadeOutCoroutine = StartCoroutine(FadeOutTurretAudio(10)); // Start fade-out
        }
    }

    private IEnumerator FadeOutTurretAudio(int steps)
    {
        float initialVolume = turretAudioSource.volume;
        float fadeOutStep = initialVolume / steps;

        for (int i = 0; i < steps; i++)
        {
            turretAudioSource.volume -= fadeOutStep;
            yield return new WaitForSeconds(.01f); // Wait for 1 second between steps
        }

        turretAudioSource.volume = 0; // Ensure volume is set to 0
        turretAudioSource.Stop(); // Stop the audio
    }

    private IEnumerator FadeInTurretAudio(int steps)
    {
        float targetVolume = 0.3f; // Set your desired target volume
        float fadeInStep = targetVolume / steps;

        for (int i = 0; i < steps; i++)
        {
            turretAudioSource.volume += fadeInStep;
            yield return new WaitForSeconds(.01f); // Wait for 1 second between steps
        }

        turretAudioSource.volume = targetVolume; // Ensure volume is set to target volume
    }

    public IEnumerator PlayClipDelayed(string name, float delay, bool asServer = false)
    {
        yield return new WaitForSeconds(delay);
        Debug.LogWarning("PLAYING "+ name);
        PlayClip(name, asServer);
    }

    public void PlayClip(string name, bool asServer = false)
    {
        Debug.LogWarning("PLAYING " + name);

        AudioClip clip = audioClips.Find(x => x.name == name);
        if (clip != null)
        {
            audioSource.PlayOneShot(clip);
            if(!base.IsOwner && !asServer)
            ServerAudioClip(name);
        }
        else
        {
            Debug.LogWarning("audio clip not found!");
            return;
        }
    }
    void Start()
    {
        engineAudioSource.loop = true; // Enable looping
        turretAudioSource.loop = true;
    }

    [ServerRpc(RequireOwnership = false, RunLocally = false)]
    private void ServerAudioClip(string name)
    {
        ObserverAudioClip(name);
    }
    [ObserversRpc(RunLocally = false)]
    private void ObserverAudioClip(string name)
    {
        PlayClip(name, true);
    }

}
