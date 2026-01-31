using System;
using UnityEngine;

public class Incinerator : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log(other.gameObject.name, other.gameObject);

        if (other.gameObject.TryGetComponent(out GrabbableRagdollBodypart bodypart))
        {
            var ragdoll = bodypart.Ragdoll;

            if (!ragdoll.IsInStandingMode)
            {
                NPCSpawner.Instance.NPCHandled(bodypart.Ragdoll.NPC);
            }
        }
    }
}