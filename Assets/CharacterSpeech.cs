using System;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
[CreateAssetMenu(fileName = "Speech", menuName = "ScriptableObjects/Speech", order = 1)]
public class CharacterSpeech : ScriptableObject
{
    public enum SpeechType
    {
        Arrive,
        Grabbed,
        Slapped,
        Reminder
    }

    [Serializable]
    public class SpeechData
    {
        [SerializeField] AudioClip[] audioClip;

        public AudioClip GetAudioClip() => audioClip[Random.Range(0, audioClip.Length)];
    }

    [SerializeField] private SpeechData arriveAggressive;
    [SerializeField] private SpeechData arriveCollab;
    [SerializeField] private SpeechData arriveIndiff;
    [SerializeField] private SpeechData arriveAfraid;
    [SerializeField] private SpeechData arriveEntitled;
    [SerializeField] private SpeechData arriveZombie;
    
    [SerializeField] private SpeechData grabAggressive;
    [SerializeField] private SpeechData grabCollab;
    [SerializeField] private SpeechData grabIndiff;
    [SerializeField] private SpeechData grabAfraid;
    [SerializeField] private SpeechData grabEntitled;
    [SerializeField] private SpeechData grabZombie;
    
    [SerializeField] private SpeechData slapAggressive;
    [SerializeField] private SpeechData slapCollab;
    [SerializeField] private SpeechData slapIndiff;
    [SerializeField] private SpeechData slapAfraid;
    [SerializeField] private SpeechData slapEntitled;
    [SerializeField] private SpeechData slapZombie;
    
    [SerializeField] private SpeechData remindAggressive;
    [SerializeField] private SpeechData remindCollab;
    [SerializeField] private SpeechData remindIndiff;
    [SerializeField] private SpeechData remindAfraid;
    [SerializeField] private SpeechData remindEntitled;
    [SerializeField] private SpeechData remindZombie;

    public AudioClip GetSpeechFor(NPC.Personality personality, SpeechType speechType)
    {
        switch (personality)
        {
            case NPC.Personality.aggressive:
                switch (speechType)
                {
                    case SpeechType.Arrive:
                        return arriveAggressive.GetAudioClip();
                    case SpeechType.Grabbed:
                        return grabAggressive.GetAudioClip();
                    case SpeechType.Slapped:
                        return slapAggressive.GetAudioClip();
                    case SpeechType.Reminder:
                        return remindAggressive.GetAudioClip();
                }

                break;
            case NPC.Personality.collaborative:
                switch (speechType)
                {
                    case SpeechType.Arrive:
                        return arriveCollab.GetAudioClip();
                    case SpeechType.Grabbed:
                        return grabCollab.GetAudioClip();
                    case SpeechType.Slapped:
                        return slapCollab.GetAudioClip();
                    case SpeechType.Reminder:
                        return remindCollab.GetAudioClip();
                }

                break;

            case NPC.Personality.indifferent:
                switch (speechType)
                {
                    case SpeechType.Arrive:
                        return arriveIndiff.GetAudioClip();
                    case SpeechType.Grabbed:
                        return grabIndiff.GetAudioClip();
                    case SpeechType.Slapped:
                        return slapIndiff.GetAudioClip();
                    case SpeechType.Reminder:
                        return remindIndiff.GetAudioClip();
                }

                break;

            case NPC.Personality.afraid:
                switch (speechType)
                {
                    case SpeechType.Arrive:
                        return arriveAfraid.GetAudioClip();
                    case SpeechType.Grabbed:
                        return grabAfraid.GetAudioClip();
                    case SpeechType.Slapped:
                        return slapAfraid.GetAudioClip();
                    case SpeechType.Reminder:
                        return remindAfraid.GetAudioClip();
                }

                break;

            case NPC.Personality.entitled:
                switch (speechType)
                {
                    case SpeechType.Arrive:
                        return arriveEntitled.GetAudioClip();
                    case SpeechType.Grabbed:
                        return grabEntitled.GetAudioClip();
                    case SpeechType.Slapped:
                        return slapEntitled.GetAudioClip();
                    case SpeechType.Reminder:
                        return remindEntitled.GetAudioClip();
                }

                break;

            case NPC.Personality.zombie:
                switch (speechType)
                {
                    case SpeechType.Arrive:
                        return arriveZombie.GetAudioClip();
                    case SpeechType.Grabbed:
                        return grabZombie.GetAudioClip();
                    case SpeechType.Slapped:
                        return slapZombie.GetAudioClip();
                    case SpeechType.Reminder:
                        return remindZombie.GetAudioClip();
                }

                break;
        }

        return null;
    }
}