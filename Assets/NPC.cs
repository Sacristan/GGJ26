using System;
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
}