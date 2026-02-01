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
        if (locomotion == null) locomotion = GetComponentInChildren<NPCLocomotion>();
        if (speech == null) speech = GetComponentInChildren<NPCSpeech>();
    }

    private void Start()
    {
        RandomiseLook();
        RandomisePersonality();
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
        OnIncinerated?.Invoke(this);
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
        OnSaved?.Invoke(this);
        markSafeRoutine = StartCoroutine(Routine());

        IEnumerator Routine()
        {
            NPCSpawner.Instance.MarkSafe(this);
            yield return new WaitForSeconds(1f);
            Locomotion.SetTarget(NPCSpawner.Instance.ExitLoc.transform);
            yield return new WaitUntil(() => Locomotion.IsCloseEnoughToTarget());
            Destroy(gameObject);
        }
    }
}