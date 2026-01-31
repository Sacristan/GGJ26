using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public class NPC : MonoBehaviour
{
    public static event System.Action<NPC> OnIncinerated;
    public static event System.Action<NPC> OnSaved;

    [SerializeField] private CharacterPotentialLooks charLooks;
    [SerializeField] NPCLocomotion locomotion;
    [SerializeField] CharacterLook look;
    public NPCLocomotion Locomotion => locomotion;
    public bool IsInfected => isInfected;

    private bool isInfected = false;

    private void OnValidate()
    {
        if (locomotion == null) locomotion = GetComponentInChildren<NPCLocomotion>();
    }

    private void Start()
    {
        RandomiseLook();
    }

    public void MoveTo(InspectLoc inspectLoc)
    {
        Locomotion.SetTarget(inspectLoc.transform);
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