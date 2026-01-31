using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FIMSpace.FProceduralAnimation;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Pool;
using UnityEngine.XR;

[RequireComponent(typeof(RagdollAnimator2))]
public partial class GrabbableRagdoll : MonoBehaviour, IRagdollAnimator2Receiver
{
    public event System.Action OnGotUp;
    public event System.Action<bool> OnGrabStateChanged;
    public event System.Action OnThrown;
    
    
    [SerializeField] private GrabbableRagdollConfig _config;
    public GrabbableRagdollConfig Config => _config;
    private static readonly List<Collider> tmpColliderList = new(16);

    [SerializeField] public Renderer characterRenderer;

    [Space] [SerializeField] private bool canBeDragged = true;
    [SerializeField] private bool allowFallOnCollision = false;
    [SerializeField] private bool canFall = true;


    private float minDragDistanceToFall = 0.5f;

    [Header("Fall on collision")] [SerializeField]
    private float collisionFallForce = 25f;

    [SerializeField] private float collisionIgnoreAfterGetUp = 1.5f;
    [SerializeField] private float collisionImpulseMultiplier = 3f;

    [Header("Auto get up")] [SerializeField]
    private bool allowAutoGetUp = true;

    [SerializeField] private LayerMask groundMask = 1;
    [SerializeField] private float groundCastExtraDist = 0.2f;
    [SerializeField] private float minFallingTime = 0.15f;
    [SerializeField] private float minLyingStableTime = 0.05f;

    [Header("Throwing")] [SerializeField] private float throwUngroundedTime = 0.75f;
    [SerializeField] private float throwVelocityMultiplier = 1.0f;
    [SerializeField] private float throwAngularVelocityMultiplier = 1.0f;
    [SerializeField] private float throwExtraUp = 0.0f;

    public enum ThrowMode
    {
        OverrideVelocities, // your current behavior (sets rb.linearVelocity / angularVelocity)
        AddVelocities, // (2) adds to existing velocities
        AddImpulse, // (3) AddForce impulse (mass-based)
        AddVelocityChange // (3) AddForce VelocityChange (mass-independent)
    }

    [SerializeField] private ThrowMode throwMode = ThrowMode.AddVelocities;

    [SerializeField] private float maxBoneLinearSpeed = 0f; // 0 = no clamp
    [SerializeField] private float maxBoneAngularSpeed = 0f; // 0 = no clamp

    private readonly GrabbableRagdollBones _bones = new();

    private bool _isInitialized = false;

    private float _magnetOrigDragPower;
    private float _magnetOrigRotatePower;
    private float _magnetOrigMotionInfluence;
    private bool _magnetOrigKinematicOnMax;

    private GrabbableRagdollBodypart[] _allGrabInteractables;
    private readonly List<GrabbableRagdollBodypart> _grabbedBodyparts = new();

    private RagdollAnimator2 _ragdoll;
    private RagdollHandler.OptimizationHandler _ragdollLod;
    private bool _hasStarted = false;

    private RagdollChainBone _anchorBone;

    private Transform _mainCamTransform = null;

    private float _lastGetUpTime = 0f;
    private float _lastGrabEnterTime = 0f;
    private float _lastGrabExitTime = 0f;
    private float _lyingStableDuration = 0f;

    private bool _forceActiveRagdoll = false;
    NPCLocomotion _locomotion;

    public RagdollAnimator2 RagdollAnimator => _ragdoll;
    public GrabbableRagdollBones Bones => _bones;
    public bool IsInStandingMode => _ragdoll.Handler.IsInStandingMode;
    public bool IsBeingGrabbed => _grabbedBodyparts is not null && _grabbedBodyparts.Any();

    public bool CanBeDragged
    {
        get => canBeDragged;
        set
        {
            if (canBeDragged == value) return;
            canBeDragged = value;

            for (int i = 0; i < _allGrabInteractables.Length; i++)
            {
                var xrInter = _allGrabInteractables[i];
                if (xrInter != null)
                {
                    xrInter.enabled = value;
                }
            }

            if (!canBeDragged)
            {
                // SetManipulatedByCustomGrabber(null);
            }
        }
    }

    public bool CanFall
    {
        get => canFall;
        set
        {
            if (canFall == value) return;
            canFall = value;

            if (!value && !IsInStandingMode)
            {
                SetRagdollStanding();
            }
        }
    }

    public bool AllowAutoGetUp
    {
        get => allowAutoGetUp;
        set => allowAutoGetUp = value;
    }

    private IEnumerator Start()
    {
        _ragdoll = GetComponent<RagdollAnimator2>();
        _locomotion = _ragdoll.GetBaseTransform.GetComponentInChildren<NPCLocomotion>();
        
        yield return new WaitForEndOfFrame();

        if (!_isInitialized)
        {
            Init(_ragdoll.Handler.BaseTransform);
        }
    }

    public float PinnedMultiplier => _grabbedBodyparts.Count > 0 ? 0.5f : 1f;


    public void Init(Transform baseTransform)
    {
        if (_isInitialized) return;
        _isInitialized = true;

        RagdollAnimator2 ragdoll = _ragdoll;
        ragdoll.Handler.BaseTransform = baseTransform;
        if (!ragdoll.Handler.WasInitialized)
        {
            ragdoll.Handler.Initialize(ragdoll, gameObject);
        }

        var dummyRef = ragdoll.Handler.DummyReference;
        dummyRef.transform.parent = transform.parent;

        using (ListPool<GrabbableRagdollBodypart>.Get(
                   out List<GrabbableRagdollBodypart> grabbableRagdollBodyparts))
        {
            foreach (RagdollBonesChain chain in ragdoll.Handler.Chains)
            {
                foreach (RagdollChainBone boneSetup in chain.BoneSetups)
                {
                    Rigidbody body = boneSetup.GameRigidbody;

                    // boneSetup.ForceLimitsAllTheTime = true;

                    var xrRagdollGrab =
                        body.gameObject.AddComponent<GrabbableRagdollBodypart>();

                    xrRagdollGrab.Init(this, boneSetup);
                    grabbableRagdollBodyparts.Add(xrRagdollGrab);

                    _bones.Add(boneSetup.BoneID, new GrabbableRagdollBones.Bone(boneSetup, xrRagdollGrab));
                }
            }

            _allGrabInteractables = grabbableRagdollBodyparts.ToArray();
        }

        {
            _anchorBone = ragdoll.Handler.GetAnchorBoneController;
            // _anchorBoneRadius = _anchorBone.MainBoneCollider.bounds.size.MaxComponent() * 0.5f;
            // _anchorBoneRadius = ((SphereCollider)_anchorBone.MainBoneCollider).radius * 0.5f;
            _anchorBoneRadius = EvalBoundingSphereRadius(_anchorBone.MainBoneCollider) * 0.5f;
        }

        _ragdollLod = new RagdollHandler.OptimizationHandler(ragdoll.Handler);

        SetRagdollStanding();

        _hasStarted = true;
    }

    Dictionary<XRPlayerHand, Vector3> handInfo = new Dictionary<XRPlayerHand, Vector3>();

    public void OnGrabbed(GrabbableRagdollBodypart ragdollBodypart, XRPlayerHand hand)
    {
        handInfo[hand] = hand.transform.position;

        if (_grabbedBodyparts.Count == 0)
        {
            OnGrabStateChanged?.Invoke(true);
        }

        _grabbedBodyparts.Add(ragdollBodypart);
    }

    public void OnHandReleased(GrabbableRagdollBodypart ragdollBodypart, XRPlayerHand hand)
    {
        handInfo.Remove(hand);
    }

    public void ReleaseBodypart(GrabbableRagdollBodypart ragdollBodypart)
    {
        _grabbedBodyparts.Remove(ragdollBodypart);

        if (_grabbedBodyparts.Count == 0) OnGrabStateChanged?.Invoke(false);
    }

    private void Update()
    {
        if (IsBeingGrabbed && IsInStandingMode)
        {
            foreach (var handI in handInfo)
            {
                float handDragDist = Vector3.Distance(handI.Key.transform.position, handI.Value);
                // Debug.Log(handDragDist);

                if (handDragDist > minDragDistanceToFall)
                {
                    SetRagdollFalling();
                    break;
                }
            }
        }
    }

    private void LateUpdate()
    {
        if (!_hasStarted)
            return;

        bool isInStandingMode = IsInStandingMode;
        // if (isInStandingMode && IsBeingGrabbed) SetRagdollFalling();

        if (!isInStandingMode && allowAutoGetUp) TryGetUp();
    }

    private void TryGetUp()
    {
        if (IsBeingGrabbed)
            return;

        RagdollHandler handler = _ragdoll.Handler;

        float time = Time.time;
        float fallingDuration = time - _lastFallTime;
        if (fallingDuration < minFallingTime)
            return;

        // The velocity of core bones are in move, so not ready for getup
        float avgTranslation = handler
            .User_GetChainBonesAverageTranslation(ERagdollChainType.Core).magnitude;
        const float noTranslationThreshold = 0.075f;
        if (avgTranslation > noTranslationThreshold)
        {
            _lyingStableDuration = 0f;
            return;
        }

        // The velocity of core bones are in move, so not ready for getup
        const float maxAvgTorqSq = 1f;
        float coreLowTransFactor = handler.User_CoreLowTranslationFactor(avgTranslation);
        float chainAngularVelocitySq = handler.User_GetChainAngularVelocity(ERagdollChainType.Core).sqrMagnitude;
        if (chainAngularVelocitySq >
            maxAvgTorqSq * coreLowTransFactor * coreLowTransFactor)
        {
            _lyingStableDuration = 0f;
            return;
        }

        // Let's be in static pose for a small amount of time
        _lyingStableDuration += Time.deltaTime;
        if (_lyingStableDuration < minLyingStableTime)
            return;

        // Check if there's ground below to stand up.
        if (groundMask != 0)
        {
            // RagdollChainBone bone = _anchorBone;
            // float distance = _anchorBoneRadius + groundCastExtraDist;
            // Ray ray = new Ray( bone.PhysicalDummyBone.position, Vector3.down);
            // Physics.Raycast(ray, out RaycastHit groundHit, distance, groundMask, QueryTriggerInteraction.Ignore);
            bool hasHitGround = TryGroundCastFromHips(out RaycastHit groundHit);

            // if (groundHit.transform is null)
            if (!hasHitGround)
            {
                _lyingStableDuration = 0f;
                return;
            }
        }
        
        OnGotUp?.Invoke();
        SetRagdollStanding();
    }


    private Coroutine _throwRoutine;


    public void ThrowRagdoll(Vector3 linearVel)
    {
        Debug.Log($"ThrowRagdoll mode={throwMode} lin={linearVel}", this);

        // Make sure it becomes a full ragdoll right now
        // ForceActiveRagdoll(resetTimer: throwUngroundedTime);

        // Temporarily prevent get-up while flying
        if (_throwRoutine != null) StopCoroutine(_throwRoutine);
        _throwRoutine = StartCoroutine(ThrowCooldown());

        // Apply multipliers
        linearVel *= throwVelocityMultiplier;

        if (throwExtraUp != 0f) linearVel += Vector3.up * throwExtraUp;

        // Apply to all bones
        var handler = _ragdoll.Handler;
        handler.CallOnAllRagdollBones(b =>
        {
            var rb = b.GameRigidbody;
            if (rb == null || rb.isKinematic) return;

            switch (throwMode)
            {
                // 1) your current behavior
                case ThrowMode.OverrideVelocities:
                    rb.linearVelocity = linearVel;
                    break;

                // (2) add to existing velocities (keeps any momentum from dragging)
                case ThrowMode.AddVelocities:
                    rb.linearVelocity += linearVel;
                    break;

                // (3) force-based: mass-based impulse (feels “heavier” on heavier bones)
                case ThrowMode.AddImpulse:
                    rb.AddForce(linearVel * rb.mass, ForceMode.Impulse); // approximate: impulse = m * dv
                    break;

                // (3) force-based: mass-independent "velocity change" (consistent throw across bones)
                case ThrowMode.AddVelocityChange:
                    rb.AddForce(linearVel, ForceMode.VelocityChange);
                    break;
            }

            // Optional clamps
            if (maxBoneLinearSpeed > 0f)
                rb.linearVelocity = Vector3.ClampMagnitude(rb.linearVelocity, maxBoneLinearSpeed);

            if (maxBoneAngularSpeed > 0f)
                rb.angularVelocity = Vector3.ClampMagnitude(rb.angularVelocity, maxBoneAngularSpeed);
        });
        
        OnThrown?.Invoke();

        IEnumerator ThrowCooldown()
        {
            bool prev = allowAutoGetUp;
            allowAutoGetUp = false;
            yield return new WaitForSeconds(throwUngroundedTime);
            allowAutoGetUp = prev;
            _throwRoutine = null;
        }
    }

    void IRagdollAnimator2Receiver.RagdollAnimator2_OnCollisionEnterEvent(RA2BoneCollisionHandler bone,
        Collision collision)
    {
        TryFallOnCollision(bone, collision);
    }

    // Make fall on strong collisions.
    private void TryFallOnCollision(RA2BoneCollisionHandler bone, Collision collision)
    {
        if (!allowFallOnCollision)
            return;

        // // Instantly enable ragdoll.
        // _ragdollLod.TurnOnTick(float.MaxValue);

        if (!IsInStandingMode)
            return;

        if (!canFall)
            return;

        float timeSinceLastGetUp = Time.time - _lastGetUpTime;
        if (timeSinceLastGetUp < collisionIgnoreAfterGetUp)
            return;

        Vector3 impulse = collision.impulse;
        float impactForce = impulse.magnitude;
        if (impactForce > collisionFallForce)
        {
            // Debug.Log("impact force: " + impactForce);
            SetRagdollFalling();
            bone.DummyBoneRigidbody.AddForce(impulse * collisionImpulseMultiplier, ForceMode.Impulse);
        }
    }


    public void SetRagdollFalling()
    {
        Debug.Log($"SetRagdollFalling", this);

        if (!IsInStandingMode)
            return;

        // bool isRagdollFullyEnabled = Mathf.Approximately(1f, _ragdoll.Handler.GetTotalBlend());
        // if (!isRagdollFullyEnabled)
        // {
        //     // Make sure to instantly match all bones to animation before ragdoll activation.
        //     _ragdollLod.TurnOnTick(1f);
        //     SyncRagdollWithAnimation(_ragdoll.Handler);
        // }

        _ragdollLod.TurnOnTick(Time.fixedDeltaTime);

        _ragdoll.User_SwitchFallState(standing: false);
        _lyingStableDuration = 0f;
    }

    public void SetRagdollStanding()
    {
        if (IsInStandingMode) return;

        if (TryGroundCastFromHips(out RaycastHit groundHit))
        {
            _locomotion.Warp(groundHit.point);
        }

        _ragdoll.User_TransitionToStandingMode(0.5f, 0f, 0.1f, 0.2f);
        _lastGetUpTime = Time.time;
    }

    private Coroutine forceActiveRagdollRoutine = null;
    private float _anchorBoneRadius;
    private float _lastFallTime;

    public void ForceActiveRagdoll(float resetTimer = 5f)
    {
        Assert.IsTrue(resetTimer > 0f, "Reset timer must be a positive number.");

        _forceActiveRagdoll = true;

        if (forceActiveRagdollRoutine is not null) StopCoroutine(forceActiveRagdollRoutine);
        forceActiveRagdollRoutine = StartCoroutine(Routine());

        IEnumerator Routine()
        {
            yield return new WaitForSeconds(resetTimer);
            _forceActiveRagdoll = false;
            forceActiveRagdollRoutine = null;
        }
    }

    private static void SyncRagdollWithAnimation(RagdollHandler handler)
    {
        handler.CallOnAllRagdollBones(b =>
        {
            b.BoneProcessor.ResetPoseParameters();
            b.GameRigidbody.rotation = b.SourceBone.rotation;
            b.GameRigidbody.transform.rotation = b.SourceBone.rotation;
            b.GameRigidbody.position = b.SourceBone.position;
            b.GameRigidbody.transform.position = b.SourceBone.position;
        });
        handler.CallOnAllInBetweenBones(b => { b.DummyBone.rotation = b.SourceBone.rotation; });
    }

    private bool TryGroundCastFromHips(out RaycastHit groundHit)
    {
        RagdollChainBone bone = _anchorBone;
        float distance = _anchorBoneRadius + groundCastExtraDist;
        Ray ray = new Ray(bone.PhysicalDummyBone.position, Vector3.down);

        return Physics.Raycast(ray, out groundHit, distance, groundMask, QueryTriggerInteraction.Ignore);
    }


    private static float EvalBoundingSphereRadius(Collider collider)
    {
        if (collider is SphereCollider sphere)
            return sphere.radius;

        if (collider is CapsuleCollider capsule)
        {
            float halfHeight = capsule.height * 0.5f;
            float r = capsule.radius;
            return Mathf.Sqrt(halfHeight * halfHeight + r * r);
        }

        Debug.LogWarning("Unexpected collider type. Fallback to universal radius computation method.", collider);
        return collider.bounds.size.MaxComponent() * 0.5f;
    }
}