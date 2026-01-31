using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public class NPC : MonoBehaviour
{
    [SerializeField] private CharacterPotentialLooks charLooks;
    [SerializeField] NPCLocomotion locomotion;
    [SerializeField] CharacterLook look;
    public NPCLocomotion Locomotion => locomotion;

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
        Material[] mats = charLooks.Randomise();
        look.SetMaterials(mats);
    }

    private Coroutine markSafeRoutine = null;
    
    public void MarkSafe()
    {
        if (markSafeRoutine != null) return;
        markSafeRoutine = StartCoroutine(Routine());
        
        IEnumerator Routine()
        {
            NPCSpawner.Instance.MarkSafe(this);
            Locomotion.SetTarget(NPCSpawner.Instance.ExitLoc.transform);
            yield return new WaitUntil(() => Locomotion.IsCloseEnoughToTarget());
            Destroy(gameObject);
        }
    }
}