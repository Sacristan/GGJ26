using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Random = UnityEngine.Random;

public class StampOfApproval : MonoBehaviour
{
    private XRGrabInteractable _interactable;

    bool isSelected = false;
    bool hasStamped = false;

    [SerializeField] private DecalProjector decalPrefab;

    [Header("Stamp")] [SerializeField] private float size = 0.25f; // width/height in meters
    [SerializeField] private float depth = 0.15f; // projection depth
    [SerializeField] private float surfaceOffset = 0.01f; // avoid z-fighting
    [SerializeField] private float randomSpinDeg = 180f; // rotate around normal

    private void Start()
    {
        _interactable = GetComponent<XRGrabInteractable>();

        _interactable.selectEntered.AddListener((x) => isSelected = true);
        _interactable.selectExited.AddListener((x) => isSelected = false);

        var colliders = GetComponentsInChildren<Collider>();

        foreach (var c in colliders)
        {
            c.gameObject.AddComponent<ColliderHook>().stamp = this;
        }
    }

    private bool isStampingLocked = false;

    private IEnumerator OnCollisionEnter(Collision other)
    {
        if (isStampingLocked) yield break;

        ProcessCollision(other);
        yield return new WaitForSeconds(0.5f);

        isStampingLocked = false;
    }

    public void ProcessCollision(Collision other)
    {
        // Debug.Log(other.gameObject.name, other.gameObject);
        if (!isSelected) return;

        if (other.gameObject.TryGetComponent(out GrabbableRagdollBodypart bodypart))
        {
            isStampingLocked = true;
            bodypart.Ragdoll.NPC.MarkSafe();
            AddStamp(other);
        }
    }

    void AddStamp(Collision collision)
    {
        var contact = collision.GetContact(0);

        var pos = contact.point + contact.normal * surfaceOffset;

        // Project into the surface: projector forward should face opposite the hit normal
        var rot = Quaternion.LookRotation(-contact.normal, Vector3.up);

        // Random spin so stamps don't look identical
        rot = Quaternion.AngleAxis(Random.Range(-randomSpinDeg, randomSpinDeg), contact.normal) * rot;

        var decal = Instantiate(decalPrefab, pos, rot);
        decal.size = new Vector3(size, size, depth);

        Debug.Log("Set parent to: " + collision.gameObject.name);

        decal.transform.SetParent(collision.gameObject.transform, true);
        // decal.transform.parent = collision.gameObject.transform;
    }

    class ColliderHook : MonoBehaviour
    {
        public StampOfApproval stamp;
        private void OnCollisionEnter(Collision other) => stamp.ProcessCollision(other);
    }
}