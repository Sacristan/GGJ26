using System;
using System.Collections;
using Unity.XR.GoogleVr;
using UnityEngine;
using Random = UnityEngine.Random;

public class NPCSpeech : MonoBehaviour
{
    public class SpeechHook : MonoBehaviour
    {
        public void Play(NPCSpeech speech, AudioClip clip, Action callback, float volume = 1.0f, float pitch = 1.0f)
        {
            transform.position = speech.transform.position;

            var ac = GetComponent<AudioSource>();
            ac.pitch = pitch;
            ac.clip = clip;
            ac.volume = volume;
            ac.Play();

            StartCoroutine(Routine());


            IEnumerator Routine()
            {
                yield return new WaitWhile(() => ac.isPlaying);
                callback?.Invoke();
                Kill();
            }
        }

        public void Kill()
        {
            Destroy(gameObject);
        }
    }

    [SerializeField] private AudioSource audioSourcePrefab;
    [SerializeField] CharacterSpeech characterSpeech;
    [SerializeField] private AudioClip deathSFX;

    private NPC _npc;

    private float pitch;

    private void Start()
    {
        _npc = GetComponentInParent<NPC>();

        pitch = Random.Range(0.8f, 1.2f);
        _npc.ragdoll.OnGrabStateChanged += OnNPCGrabbed;
    }

    private void OnNPCGrabbed(bool gotGrabbed)
    {
        if (gotGrabbed) Say(CharacterSpeech.SpeechType.Grabbed, 0.5f);
    }

    public void Say(CharacterSpeech.SpeechType speechType, float delay = 0.01f)
    {
        var clip = characterSpeech.GetSpeechFor(_npc.GetPersonality, speechType);
        Debug.Log(
            $"{nameof(NPCSpeech)}::{nameof(Say)} {_npc.GetPersonality}/{speechType} delay: {delay} clip {clip.name}");

        StartCoroutine(Routine());

        IEnumerator Routine()
        {
            yield return new WaitForSeconds(delay);
            SayStuff(clip);
        }
    }

    public void DieSFX()
    {
        SayStuff(deathSFX, volume: 0.5f);
    }

    private SpeechHook currentHook = null;

    private void SayStuff(AudioClip audioClip, bool doOverride = false, float volume = 1.0f)
    {
        if (doOverride)
        {
            if (currentHook != null) currentHook.Kill();
        }
        else
        {
            if (currentHook != null) return;
        }

        currentHook = Instantiate(audioSourcePrefab).gameObject.AddComponent<SpeechHook>();
        currentHook.Play(this, audioClip, () => currentHook = null, volume, pitch);
    }
}