using System;
using UnityEngine;

public class Incinerator : MonoBehaviour
{
    AudioSource _audioSource;

    private void Start()
    {
        _audioSource = GetComponent<AudioSource>();
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log(other.gameObject.name, other.gameObject);

        if (other.gameObject.TryGetComponent(out GrabbableRagdollBodypart bodypart))
        {
            var ragdoll = bodypart.Ragdoll;

            if (!ragdoll.IsInStandingMode)
            {
                bodypart.Ragdoll.NPC.Incinerate();
                _audioSource.Play();
            }
        }
    }
}