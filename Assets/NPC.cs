using System;
using UnityEngine;

public class NPC : MonoBehaviour
{
    [SerializeField] NPCLocomotion locomotion;
    public NPCLocomotion Locomotion => locomotion;

    private void OnValidate()
    {
        if (locomotion == null) locomotion = GetComponentInChildren<NPCLocomotion>();
    }

    public void MoveTo(InspectLoc inspectLoc)
    {
        Locomotion.SetTarget(inspectLoc.transform);
    }
}