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
        yield return new WaitForEndOfFrame();

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

        decal.transform.parent = collision.gameObject.transform;
    }


    public static Vector2 CalculateUVFromMesh(Mesh mesh, Vector3 localPoint)
    {
        Vector3[] vertices = mesh.vertices;
        Vector2[] uvs = mesh.uv;
        int[] triangles = mesh.triangles;

        // Find the closest triangle to the local point
        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 p0 = vertices[triangles[i]];
            Vector3 p1 = vertices[triangles[i + 1]];
            Vector3 p2 = vertices[triangles[i + 2]];

            // Check if the point is inside the triangle
            if (PointInTriangle(localPoint, p0, p1, p2))
            {
                // Calculate barycentric coordinates
                Vector3 barycentric = Barycentric(localPoint, p0, p1, p2);

                // Interpolate UVs
                Vector2 uv0 = uvs[triangles[i]];
                Vector2 uv1 = uvs[triangles[i + 1]];
                Vector2 uv2 = uvs[triangles[i + 2]];

                Vector2 uv = uv0 * barycentric.x + uv1 * barycentric.y + uv2 * barycentric.z;
                return uv;
            }
        }

        // If the point is not inside any triangle, return a default UV (e.g., (0, 0))
        return Vector2.zero;
    }

// Helper function to check if a point is inside a triangle
    private static bool PointInTriangle(Vector3 point, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        Vector3 d1 = Vector3.Cross(p1 - p0, point - p0);
        Vector3 d2 = Vector3.Cross(p2 - p1, point - p1);
        Vector3 d3 = Vector3.Cross(p0 - p2, point - p2);

        bool hasNeg = d1.z < 0 || d2.z < 0 || d3.z < 0;
        bool hasPos = d1.z > 0 || d2.z > 0 || d3.z > 0;

        return !(hasNeg && hasPos);
    }

// Helper function to calculate barycentric coordinates
    private static Vector3 Barycentric(Vector3 point, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        Vector3 v0 = p1 - p0;
        Vector3 v1 = p2 - p0;
        Vector3 v2 = point - p0;

        float denom = v0.x * v1.y - v1.x * v0.y;
        float v = (v2.x * v1.y - v1.x * v2.y) / denom;
        float w = (v0.x * v2.y - v2.x * v0.y) / denom;
        float u = 1.0f - v - w;

        return new Vector3(u, v, w);
    }

    class ColliderHook : MonoBehaviour
    {
        public StampOfApproval stamp;
        private void OnCollisionEnter(Collision other) => stamp.ProcessCollision(other);
    }
}