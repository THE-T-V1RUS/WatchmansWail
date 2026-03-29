using System;
using System.Collections;
using UnityEngine;

public class CameraMover : MonoBehaviour
{
    [SerializeField] private Transform cameraFollowPoint;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private float moveDuration = 2.0f;
    [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private float lookDuration = 1.0f;
    [SerializeField] private AnimationCurve lookCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private bool notifyDialogueOnComplete = true;

    private Coroutine moveRoutine;
    private Coroutine forceLookRoutine;
    private Vector3 defaultLocalPosition;
    private Quaternion defaultLocalRotation;
    [SerializeField] private Transform _destination;
    [SerializeField] private Transform _forceLookTarget;
    private Transform originalParent;
    private Transform _lastKnownDestination;
    private bool _followPointInitialized;
    private bool _isAtDestination;
    private bool _isForceLooking;

    /// <summary>True while the camera is moving to or parked at a destination.</summary>
    public bool IsActive => moveRoutine != null || _isAtDestination;

    /// <summary>True while force look is active.</summary>
    public bool IsForceLooking => _isForceLooking;

    /// <summary>Invoked when the camera finishes returning to the player.</summary>
    public event Action OnReturnedToPlayer;

    /// <summary>Invoked when the camera arrives at the destination.</summary>
    public event Action OnArrivedAtDestination;

    /// <summary>Invoked when force look finishes or is disabled.</summary>
    public event Action OnForceLookComplete;

    public Transform ForceLookTarget => _forceLookTarget;

    public void SetForceLookTarget(Transform target)
    {
        _forceLookTarget = target;

        if (target != null)
        {
            _isForceLooking = true;
            StartForceLookRoutine();
        }
        else
        {
            StopForceLookRoutine();
            _isForceLooking = false;
        }
    }

    public void DisableForceLook()
    {
        StopForceLookRoutine();
        if (_isForceLooking)
        {
            _isForceLooking = false;
            OnForceLookComplete?.Invoke();
        }
    }

    private void StartForceLookRoutine()
    {
        StopForceLookRoutine();
        forceLookRoutine = StartCoroutine(ForceLookCoroutine());
    }

    private void StopForceLookRoutine()
    {
        if (forceLookRoutine != null)
        {
            StopCoroutine(forceLookRoutine);
            forceLookRoutine = null;
        }
    }

    public Transform Destination
    {
        get => _destination;
        set
        {
            if (_destination == value)
            {
                return;
            }

            _destination = value;

            if (_destination != null)
            {
                BeginMove(_destination.position, _destination.rotation);
            }
            else
            {
                BeginReturnToPlayer();
            }
        }
    }

    private void Awake()
    {
        TryInitializeFollowPoint();
        _lastKnownDestination = _destination;
    }

    private void TryInitializeFollowPoint()
    {
        if (_followPointInitialized || cameraFollowPoint == null)
        {
            return;
        }

        defaultLocalPosition = cameraFollowPoint.localPosition;
        defaultLocalRotation = cameraFollowPoint.localRotation;
        originalParent = cameraFollowPoint.parent;
        _followPointInitialized = true;
    }

    private void Update()
    {
        if (_destination != _lastKnownDestination)
        {
            _lastKnownDestination = _destination;

            if (_destination != null)
            {
                BeginMove(_destination.position, _destination.rotation);
            }
            else
            {
                BeginReturnToPlayer();
            }
        }
    }

    private IEnumerator ForceLookCoroutine()
    {
        // Wait for any active move to finish first.
        while (moveRoutine != null)
        {
            yield return null;
        }

        if (_forceLookTarget == null || cameraFollowPoint == null)
        {
            forceLookRoutine = null;
            yield break;
        }

        if (!_isAtDestination && playerTransform != null)
        {
            // Player-attached: simultaneous yaw on player body + pitch on camera.
            // Pre-compute fixed start and target values so interpolation is clean.
            float startYaw = playerTransform.eulerAngles.y;
            float startPitch = cameraFollowPoint.localEulerAngles.x;
            if (startPitch > 180f) startPitch -= 360f;

            Vector3 direction = _forceLookTarget.position - cameraFollowPoint.position;
            if (direction.sqrMagnitude < 0.0001f)
            {
                forceLookRoutine = null;
                yield break;
            }

            // Target yaw from horizontal direction.
            Vector3 planarDir = new Vector3(direction.x, 0f, direction.z);
            float targetYaw = startYaw;
            if (planarDir.sqrMagnitude > 0.0001f)
            {
                targetYaw = Quaternion.LookRotation(planarDir.normalized, Vector3.up).eulerAngles.y;
            }

            // Target pitch computed from the final yaw orientation.
            Quaternion finalPlayerRot = Quaternion.Euler(0f, targetYaw, 0f);
            Vector3 localDir = Quaternion.Inverse(finalPlayerRot) * direction.normalized;
            float targetPitch = -Mathf.Atan2(localDir.y, localDir.z) * Mathf.Rad2Deg;
            targetPitch = Mathf.Clamp(targetPitch, -90f, 90f);

            float elapsed = 0f;

            while (elapsed < lookDuration)
            {
                elapsed += Time.deltaTime;
                float t = lookCurve.Evaluate(Mathf.Clamp01(elapsed / lookDuration));

                float newYaw = Mathf.LerpAngle(startYaw, targetYaw, t);
                playerTransform.rotation = Quaternion.Euler(0f, newYaw, 0f);

                float newPitch = Mathf.LerpAngle(startPitch, targetPitch, t);
                cameraFollowPoint.localRotation = Quaternion.Euler(newPitch, 0f, 0f);

                yield return null;
            }

            // Snap to final.
            playerTransform.rotation = Quaternion.Euler(0f, targetYaw, 0f);
            cameraFollowPoint.localRotation = Quaternion.Euler(targetPitch, 0f, 0f);
        }
        else
        {
            // Destination-attached: rotate follow point directly.
            Quaternion startCamWorldRot = cameraFollowPoint.rotation;
            float elapsed = 0f;

            while (elapsed < lookDuration)
            {
                elapsed += Time.deltaTime;
                float t = lookCurve.Evaluate(Mathf.Clamp01(elapsed / lookDuration));

                Vector3 direction = _forceLookTarget.position - cameraFollowPoint.position;
                if (direction.sqrMagnitude < 0.0001f) { yield return null; continue; }

                Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
                cameraFollowPoint.rotation = Quaternion.Slerp(startCamWorldRot, targetRotation, t);

                yield return null;
            }

            // Snap to final.
            Vector3 finalDir = _forceLookTarget.position - cameraFollowPoint.position;
            if (finalDir.sqrMagnitude >= 0.0001f)
            {
                cameraFollowPoint.rotation = Quaternion.LookRotation(finalDir.normalized);
            }
        }

        _isForceLooking = false;
        OnForceLookComplete?.Invoke();
        NotifyDialogueComplete();
        forceLookRoutine = null;
    }

    [ContextMenu("Move To Destination")]
    public void MoveToDestination()
    {
        if (_destination != null)
        {
            BeginMove(_destination.position, _destination.rotation);
        }
    }

    [ContextMenu("Return To Player")]
    public void ReturnToPlayer()
    {
        BeginReturnToPlayer();
    }

    private void BeginMove(Vector3 targetPos, Quaternion targetRot)
    {
        StopForceLookRoutine();
        _isForceLooking = false;
        TryInitializeFollowPoint();

        if (cameraFollowPoint == null)
        {
            return;
        }

        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
        }

        // Unparent so the player's movement doesn't drag the camera along.
        cameraFollowPoint.SetParent(null, true);
        _isAtDestination = true;
        moveRoutine = StartCoroutine(SmoothMove(targetPos, targetRot));
    }

    private void BeginReturnToPlayer()
    {
        StopForceLookRoutine();
        _isForceLooking = false;
        TryInitializeFollowPoint();

        if (cameraFollowPoint == null)
        {
            return;
        }

        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
        }

        Vector3 homeWorld = originalParent != null
            ? originalParent.TransformPoint(defaultLocalPosition)
            : defaultLocalPosition;

        Quaternion homeRot = originalParent != null
            ? originalParent.rotation * defaultLocalRotation
            : defaultLocalRotation;

        moveRoutine = StartCoroutine(SmoothMove(homeWorld, homeRot, true));
    }

    private IEnumerator SmoothMove(Vector3 targetPos, Quaternion targetRot, bool reparentOnComplete = false)
    {
        Vector3 startPos = cameraFollowPoint.position;
        Quaternion startRot = cameraFollowPoint.rotation;
        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = moveCurve.Evaluate(Mathf.Clamp01(elapsed / moveDuration));
            cameraFollowPoint.position = Vector3.Lerp(startPos, targetPos, t);
            cameraFollowPoint.rotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }

        cameraFollowPoint.position = targetPos;
        cameraFollowPoint.rotation = targetRot;

        if (reparentOnComplete && originalParent != null)
        {
            cameraFollowPoint.SetParent(originalParent, true);
            cameraFollowPoint.localPosition = defaultLocalPosition;
            cameraFollowPoint.localRotation = defaultLocalRotation;
            _isAtDestination = false;
            OnReturnedToPlayer?.Invoke();
            NotifyDialogueComplete();
        }
        else
        {
            OnArrivedAtDestination?.Invoke();
            NotifyDialogueComplete();
        }

        moveRoutine = null;
    }

    public void setFollowTarget(Transform target)
    {
        Destination = target;
    }

    private void NotifyDialogueComplete()
    {
        if (notifyDialogueOnComplete && DialogueManager.Instance != null)
        {
            DialogueManager.Instance.CompleteDialogueEvent();
        }
    }

    public void SetMoveDuration(float duration)
    {
        moveDuration = duration;
    }

    public void SetLookDuration(float duration)
    {
        lookDuration = duration;
    }
}
