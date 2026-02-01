using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class NPC : MonoBehaviour
{
    public enum Personality
    {
        none,
        aggressive,
        collaborative,
        indifferent,
        afraid,
        entitled,
        zombie
    }

    public static event System.Action<NPC> OnIncinerated;
    public static event System.Action<NPC> OnSaved;

    [SerializeField] public GrabbableRagdoll ragdoll;
    [SerializeField] private CharacterPotentialLooks charLooks;
    [SerializeField] NPCLocomotion locomotion;
    [SerializeField] CharacterLook look;
    [SerializeField] public NPCSpeech speech;
    public NPCLocomotion Locomotion => locomotion;
    public bool IsInfected => isInfected;
    public Personality GetPersonality => personality;

    private bool isInfected = false;

    Personality personality = Personality.none;

    private void OnValidate()
    {
        if (ragdoll == null) ragdoll = GetComponentInChildren<GrabbableRagdoll>();
        if (locomotion == null) locomotion = GetComponentInChildren<NPCLocomotion>();
        if (speech == null) speech = GetComponentInChildren<NPCSpeech>();
    }

    private void Start()
    {
        RandomiseLook();
        RandomisePersonality();

        ragdoll.OnGrabStateChanged += (x) => ResetIdle();
        ragdoll.OnGotSlapped += () => ResetIdle();
        ragdoll.OnGotUp += () => ResetIdle();
        ragdoll.OnThrown += () => ResetIdle();
        speech.OnSaidStuff += () => ResetIdle();
    }

    private float idleTimer;
    private float idleSayInterval = 10f;

    void Update()
    {
        if (!atInspection || !ragdoll.IsInStandingMode)
        {
            ResetIdle();
            return;
        }

        idleTimer += Time.deltaTime;

        if (idleTimer >= idleSayInterval)
        {
            idleTimer = 0f;
            DoThing();
        }
    }

    public void ResetIdle()
    {
        idleTimer = 0f;
    }

    void DoThing()
    {
        speech.Say(CharacterSpeech.SpeechType.Reminder);
    }

    public void MoveTo(InspectLoc inspectLoc)
    {
        Locomotion.SetTarget(inspectLoc.transform);
    }

    void RandomisePersonality()
    {
        if (isInfected)
        {
            if (Random.value < 0.25f)
            {
                personality = Personality.zombie;
            }
        }

        var values = (Personality[])Enum.GetValues(typeof(Personality));
        personality = (Personality)Random.Range(1, values.Length - 1);
    }

    void RandomiseLook()
    {
        var data = charLooks.Randomise();
        isInfected = data.Item1;
        look.SetMaterials(data.Item2);
    }

    private Coroutine incinerateRoutine = null;

    public void Incinerate()
    {
        if (incinerateRoutine != null) return;
        atInspection = false;
        OnIncinerated?.Invoke(this);
        speech.DieSFX();
        incinerateRoutine = StartCoroutine(Routine());

        IEnumerator Routine()
        {
            NPCSpawner.Instance.NPCHandled(this);
            yield return null;
        }
    }


    private Coroutine markSafeRoutine = null;

    public void MarkSafe()
    {
        if (markSafeRoutine != null) return;
        atInspection = false;
        OnSaved?.Invoke(this);
        markSafeRoutine = StartCoroutine(Routine());

        IEnumerator Routine()
        {
            NPCSpawner.Instance.MarkSafe(this);
            speech.Say(CharacterSpeech.SpeechType.GotThrough, 1.5f);
            yield return new WaitForSeconds(1f);
            Locomotion.SetTarget(NPCSpawner.Instance.ExitLoc.transform);
            yield return new WaitUntil(() => Locomotion.IsCloseEnoughToTarget());
            Destroy(gameObject);
        }
    }

    private bool atInspection = false;

    public void ArrivedAtInspection()
    {
        atInspection = true;
    }
}