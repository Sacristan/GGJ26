using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class StampOfApproval : MonoBehaviour
{
    private XRGrabInteractable _interactable;

    bool isSelected = false;

    private void Start()
    {
        _interactable = GetComponent<XRGrabInteractable>();
        
        _interactable.selectEntered.AddListener((x) => isSelected = true);
        _interactable.selectExited.AddListener((x) => isSelected = false);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Debug.Log(other.gameObject.name, other.gameObject);
        if (!isSelected) return;
        
        
        if (other.gameObject.TryGetComponent(out GrabbableRagdollBodypart bodypart))
        {
            bodypart.Ragdoll.NPC.MarkSafe();
        }
    }
}