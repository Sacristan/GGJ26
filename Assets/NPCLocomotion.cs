using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;


public class CoroutineSingleton
{
    private readonly MonoBehaviour _owner;
    private Coroutine _currentCoroutine;

    public CoroutineSingleton(MonoBehaviour owner)
    {
        _owner = owner;
    }

    public void Run(IEnumerator routine)
    {
        // Stop previous coroutine if it exists
        if (_currentCoroutine != null)
        {
            _owner.StopCoroutine(_currentCoroutine);
        }

        // Start new coroutine
        _currentCoroutine = _owner.StartCoroutine(WrapRoutine(routine));
    }

    private IEnumerator WrapRoutine(IEnumerator routine)
    {
        yield return routine;
        _currentCoroutine = null;
    }

    public void Stop()
    {
        if (_currentCoroutine != null)
        {
            _owner.StopCoroutine(_currentCoroutine);
            _currentCoroutine = null;
        }
    }

    public bool IsRunning => _currentCoroutine != null;
}

public class NPCLocomotion : MonoBehaviour
{
    NavMeshAgent agent;
    [SerializeField] Rigidbody rb;
    [SerializeField] Animator animator;
    [SerializeField] GrabbableRagdoll ragdoll;

    [SerializeField] float stopDistance = 1f;
    [SerializeField] float moveSpeed = 2.5f;
    [SerializeField] float rotateSpeed = 10f;
    [SerializeField] float navDelayTimer = 1f;

    Transform targetLoc;

    [SerializeField] float finalFaceAngleDeg = 3f; // considered "aligned"
    [SerializeField] float finalFaceSpeed = 12f; // can reuse rotateSpeed if you want
    [SerializeField] bool useTargetRotation = true;

    public void SetTarget(Transform target)
    {
        targetLoc = target;
    }

    private bool allowNavigation = true;
    bool AllowNavigation => allowNavigation;

    void Awake()
    {
        agent = GetComponentInChildren<NavMeshAgent>();

        agent.updatePosition = false;
        agent.updateRotation = false;
    }

    private CoroutineSingleton setNavDelayedRunner;

    private void Start()
    {
        ragdoll.OnGrabStateChanged += RagdollGrabStateChanged;
        ragdoll.OnGotUp += GotUp;
        ragdoll.OnThrown += RagdollOnThrown;
    }

    private void RagdollOnThrown()
    {
        SetNavFlagImmediate(false);
    }

    private void GotUp()
    {
        SetNavFlagDelayed(true);
    }

    public void RagdollGrabStateChanged(bool isGrabbed)
    {
        if (isGrabbed) SetNavFlagImmediate(false);
        else
        {
            if (ragdoll.IsInStandingMode) SetNavFlagDelayed(true); //else wait for get up
        }
    }

    void SetNavFlagDelayed(bool flag)
    {
        setNavDelayedRunner ??= new(this);
        setNavDelayedRunner.Run(Routine());

        IEnumerator Routine()
        {
            yield return new WaitForSeconds(navDelayTimer);
            SetNavFlagImmediate(flag);
        }
    }

    void SetNavFlagImmediate(bool flag)
    {
        allowNavigation = flag;
        setNavDelayedRunner?.Stop();
    }

    void Update()
    {
        if (!AllowNavigation)
        {
            agent.ResetPath();
            animator.SetBool("Moving", false);
            return;
        }

        if (targetLoc)
        {
            if (IsCloseEnoughToTarget())
                agent.ResetPath();
            else
                agent.SetDestination(targetLoc.position);
        }

        animator.SetBool(
            "Moving",
            agent.hasPath && agent.remainingDistance > agent.stoppingDistance
        );
    }

    public bool IsCloseEnoughToTarget()
    {
        if (!targetLoc) return true;

        Vector3 a = rb.position;
        a.y = 0;
        Vector3 b = targetLoc.position;
        b.y = 0;

        return Vector3.Distance(a, b) < stopDistance;
    }

    void FixedUpdate()
    {
        if (!AllowNavigation) return;

        // Are we actively moving along a nav path?
        bool movingAlongPath = agent.hasPath && agent.remainingDistance > agent.stoppingDistance;

        // Are we at/near the destination and should "finish facing"?
        bool shouldFinalFace = targetLoc != null && IsCloseEnoughToTarget();

        // ---------------- ROTATION ----------------
        if (movingAlongPath)
        {
            // Rotate towards movement direction (same feel as before)
            Vector3 target = agent.nextPosition;
            Vector3 dir = target - rb.position;
            dir.y = 0f;

            if (dir.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir);
                rb.MoveRotation(
                    Quaternion.Slerp(rb.rotation, targetRot, rotateSpeed * Time.fixedDeltaTime)
                );
            }
        }
        else if (shouldFinalFace)
        {
            // Rotate to match the destination transform's Z-forward (its world forward)
            Vector3 fwd = targetLoc.forward;
            fwd.y = 0f;

            if (fwd.sqrMagnitude > 0.0001f)
            {
                Quaternion desiredRot = Quaternion.LookRotation(fwd);
                rb.MoveRotation(
                    Quaternion.Slerp(rb.rotation, desiredRot, rotateSpeed * Time.fixedDeltaTime)
                );
            }
        }

        // ---------------- POSITION ----------------
        // Only push position while we're actually following a path.
        if (!movingAlongPath) return;

        Vector3 newPos = Vector3.MoveTowards(
            rb.position,
            agent.nextPosition,
            moveSpeed * Time.fixedDeltaTime
        );

        rb.MovePosition(newPos);

        // Keep the agent synced to the rigidbody
        agent.nextPosition = rb.position;
    }


    public void Warp(Vector3 position)
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        rb.position = position;
        agent.Warp(position);
        agent.ResetPath();
    }

    public void StopMovement()
    {
        agent.ResetPath();
    }
}