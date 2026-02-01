using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Random = UnityEngine.Random;

public class StampOfApproval : MonoBehaviour
{
    public event System.Action OnSelected;
    
    private XRGrabInteractable _interactable;

    bool isSelected = false;
    bool hasStamped = false;

    [SerializeField] private DecalProjector decalPrefab;

    [Header("Stamp")] [SerializeField] private float size = 0.25f; // width/height in meters
    [SerializeField] private float depth = 0.15f; // projection depth
    [SerializeField] private float surfaceOffset = 0.01f; // avoid z-fighting
    [SerializeField] private float randomSpinDeg = 180f; // rotate around normal

    AudioSource _audioSource;
    [SerializeField] private AudioClip[] _clips;

    [SerializeField] private Transform pivot;
    bool allowMovement = true;
    public float amplitude = 0.05f;
    public float speed = 3f;
    
    private void Start()
    {
        _audioSource = GetComponent<AudioSource>();
        _interactable = GetComponent<XRGrabInteractable>();

        _interactable.selectEntered.AddListener(OnSelectEnter);
        _interactable.selectExited.AddListener(OnDeselect);

        var colliders = GetComponentsInChildren<Collider>();

        foreach (var c in colliders)
        {
            c.gameObject.AddComponent<ColliderHook>().stamp = this;
        }
    }
    
    const int StampActiveLayer = 0;
    const int StampIdleLayer = 7;
    
    private void OnSelectEnter(SelectEnterEventArgs arg0)
    {
        OnSelected?.Invoke();
        allowMovement = false;
        isSelected = true;
        SetLayerRecursive(gameObject, StampActiveLayer);
    }
    
    private void OnDeselect(SelectExitEventArgs arg0)
    {
        allowMovement = true;
        isSelected = false;
        SetLayerRecursive(gameObject, StampIdleLayer);
    }

    private bool isStampingLocked = false;

    void Update()
    {
        if (!allowMovement)
        {
            pivot.localPosition = Vector3.zero;
            return;
        }

        float offset = Mathf.Sin(Time.time * speed) * amplitude;

        Vector3 localUp = pivot.parent
            ? pivot.parent.InverseTransformDirection(Vector3.up)
            : Vector3.up;

        pivot.localPosition = localUp * offset;
    }
    
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

        PlaySFX();
    }

    void PlaySFX()
    {
        if (_audioSource.isPlaying) return;
        _audioSource.pitch = Random.Range(0.8f, 1.2f);
        _audioSource.clip = _clips[Random.Range(0, _clips.Length)];
        _audioSource.Play();
    }

    public static void SetLayerRecursive(GameObject root, int layer)
    {
        if (!root) return;

        root.layer = layer;

        foreach (Transform child in root.transform)
            SetLayerRecursive(child.gameObject, layer);
    }

    class ColliderHook : MonoBehaviour
    {
        public StampOfApproval stamp;
        private void OnCollisionEnter(Collision other) => stamp.ProcessCollision(other);
    }
}