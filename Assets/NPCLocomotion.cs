using System;
using UnityEngine;
using UnityEngine.AI;

public class NPCLocomotion : MonoBehaviour
{
    [Header("Movement")] public float maxSpeed = 3.5f;
    public float acceleration = 20f;
    public float turnSpeedDeg = 720f;

    [Header("Stopping")] public float stopDistance = 0.2f;
    public bool rotateTowardsVelocity = true;

    [Header("Animation")] [SerializeField] Animator animator;
    string movingBoolName = "Moving";
    [SerializeField] float movingSpeedThreshold = 0.05f; // meters/sec
    [SerializeField] bool useActualVelocityForAnim = true;

    [SerializeField] Rigidbody rb;
    NavMeshAgent agent;

    Vector3 desiredVel;
    int movingBoolHash;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        agent.updatePosition = false;
        agent.updateRotation = false;
        agent.autoBraking = true;

        rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (animator == null) animator = GetComponentInChildren<Animator>();
        movingBoolHash = Animator.StringToHash(movingBoolName);
    }

    private void Start()
    {
        InspectLoc loc = FindAnyObjectByType<InspectLoc>();
        SetDestination(loc.transform.position);
    }

    void OnEnable()
    {
        agent.Warp(rb.position);
    }

    public bool SetDestination(Vector3 worldPos)
    {
        if (!agent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(rb.position, out var hit, 2f, NavMesh.AllAreas))
                agent.Warp(hit.position);
            else
                return false;
        }

        return agent.SetDestination(worldPos);
    }

    void FixedUpdate()
    {
        if (agent.isOnNavMesh)
            agent.nextPosition = rb.position;

        if (!agent.hasPath || agent.pathPending)
        {
            desiredVel = Vector3.zero;
            ApplyMovement(desiredVel);
            UpdateAnimatorMoving();
            return;
        }

        float remaining = agent.remainingDistance;
        bool shouldStop = remaining <= Mathf.Max(agent.stoppingDistance, stopDistance);

        if (shouldStop)
        {
            desiredVel = Vector3.zero;
            ApplyMovement(desiredVel);
            UpdateAnimatorMoving();
            return;
        }

        desiredVel = agent.desiredVelocity;

        if (desiredVel.sqrMagnitude > 0.0001f)
            desiredVel = Vector3.ClampMagnitude(desiredVel, maxSpeed);

        ApplyMovement(desiredVel);

        if (rotateTowardsVelocity)
            ApplyRotation(desiredVel);

        UpdateAnimatorMoving();
    }

    void UpdateAnimatorMoving()
    {
        if (!animator) return;

        // "intent" velocity vs "actual" velocity
        float speed = useActualVelocityForAnim
            ? new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z).magnitude
            : new Vector3(desiredVel.x, 0f, desiredVel.z).magnitude;

        // Optional: also require agent not stopped
        bool moving = speed > movingSpeedThreshold && agent.hasPath && !agent.pathPending;

        animator.SetBool(movingBoolHash, moving);
        // If you prefer speed blend trees:
        // animator.SetFloat("Speed", speed);
    }

    void ApplyMovement(Vector3 targetVelocity)
    {
        Vector3 currentVel = rb.linearVelocity;
        Vector3 velChange = targetVelocity - currentVel;

        float maxDelta = acceleration * Time.fixedDeltaTime;
        velChange = Vector3.ClampMagnitude(velChange, maxDelta);

        rb.linearVelocity = currentVel + velChange;
    }

    void ApplyRotation(Vector3 vel)
    {
        vel.y = 0f;
        if (vel.sqrMagnitude < 0.001f) return;

        Quaternion targetRot = Quaternion.LookRotation(vel.normalized, Vector3.up);
        Quaternion newRot = Quaternion.RotateTowards(rb.rotation, targetRot, turnSpeedDeg * Time.fixedDeltaTime);
        rb.MoveRotation(newRot);
    }

    void OnDrawGizmosSelected()
    {
        var a = GetComponent<NavMeshAgent>();
        if (a && a.hasPath)
        {
            Gizmos.color = Color.green;
            var corners = a.path.corners;
            for (int i = 0; i < corners.Length - 1; i++)
                Gizmos.DrawLine(corners[i], corners[i + 1]);
        }
    }
}