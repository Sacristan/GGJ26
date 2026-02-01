using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

public class NPCSpeech : MonoBehaviour
{
    [SerializeField] CharacterSpeech characterSpeech;

    private AudioSource _source;
    private NPC _npc;

    private void Start()
    {
        _source = GetComponent<AudioSource>();
        _npc = GetComponentInParent<NPC>();

        _source.pitch = Random.Range(0.8f, 1.2f);
    }

    public void Say(CharacterSpeech.SpeechType speechType, float delay = 0.01f)
    {
        var clip = characterSpeech.GetSpeechFor(_npc.GetPersonality, speechType);
        Debug.Log($"{nameof(NPCSpeech)}::{nameof(Say)} {_npc.GetPersonality}/{speechType} delay: {delay} clip {clip.name}");

        StartCoroutine(Routine());

        IEnumerator Routine()
        {
            yield return new WaitForSeconds(delay);
            SayStuff(clip);
        }
    }

    private void SayStuff(AudioClip audioClip)
    {
        if (_source.isPlaying || audioClip == null) return;
        _source.clip = audioClip;
        _source.Play();
    }
}