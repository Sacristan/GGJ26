using UnityEngine;
using UnityEngine.AI;

public class NPCLocomotion : MonoBehaviour
{
    NavMeshAgent agent;
    [SerializeField] Rigidbody rb;
    [SerializeField] Animator animator;

    [SerializeField] Transform inspectLoc;
    [SerializeField] float stopDistance = 1f;
    [SerializeField] float moveSpeed = 2.5f;

    bool AllowNavigation => true;

    void Awake()
    {
        agent = GetComponentInChildren<NavMeshAgent>();

        agent.updatePosition = false;
        agent.updateRotation = false;
    }

    void Update()
    {
        if (!AllowNavigation)
        {
            agent.ResetPath();
            animator.SetBool("Moving", false);
            return;
        }

        if (inspectLoc)
        {
            float d = Vector3.Distance(transform.position, inspectLoc.position);

            if (d > stopDistance)
                agent.SetDestination(inspectLoc.position);
            else
                agent.ResetPath();
        }

        animator.SetBool(
            "Moving",
            agent.hasPath && agent.remainingDistance > agent.stoppingDistance
        );
    }

    void FixedUpdate()
    {
        if (!agent.hasPath) return;

        Vector3 target = agent.nextPosition;
        Vector3 newPos = Vector3.MoveTowards(
            rb.position,
            target,
            moveSpeed * Time.fixedDeltaTime
        );

        rb.MovePosition(newPos);
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