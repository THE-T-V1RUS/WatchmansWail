using System.Collections.Generic;
using UnityEngine;

public class npcController : MonoBehaviour
{
    [SerializeField] private Animator npcAnimator;
    [SerializeField] private Transform destinationPoint;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float stoppingDistance = 0.15f;
    [SerializeField] private bool notifyDialogueOnArrival = true;
    [SerializeField] List<AudioClip> footstepClips;

    [SerializeField] private float settleSpeed = 3f;

    private static readonly int IsWalking = Animator.StringToHash("IsWalking");
    private bool isMoving;
    private bool isSettling;

    void Awake()
    {
        npcAnimator = GetComponent<Animator>();
    }

    void Update()
    {
        if (destinationPoint == null) return;

        if (isSettling)
        {
            Settle();
            return;
        }

        if (!isMoving) return;

        Vector3 target = destinationPoint.position;
        Vector3 direction = target - transform.position;
        direction.y = 0f;
        float distance = direction.magnitude;

        if (distance <= stoppingDistance)
        {
            isMoving = false;
            isSettling = true;
            npcAnimator.SetBool(IsWalking, false);
            return;
        }

        Vector3 moveDir = direction.normalized;
        Quaternion targetRotation = Quaternion.LookRotation(moveDir);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        transform.position += moveDir * (moveSpeed * Time.deltaTime);
    }

    private void Settle()
    {
        float t = settleSpeed * Time.deltaTime;

        Vector3 targetPos = destinationPoint.position;
        targetPos.y = transform.position.y;
        transform.position = Vector3.Lerp(transform.position, targetPos, t);
        transform.rotation = Quaternion.Slerp(transform.rotation, destinationPoint.rotation, t);

        float posDelta = Vector3.Distance(transform.position, targetPos);
        float rotDelta = Quaternion.Angle(transform.rotation, destinationPoint.rotation);

        if (posDelta < 0.005f && rotDelta < 0.5f)
        {
            transform.position = targetPos;
            transform.rotation = destinationPoint.rotation;
            isSettling = false;
            if (notifyDialogueOnArrival)
                NotifyDialogueComplete();
        }
    }

    public void SetDestination(Transform destination)
    {
        destinationPoint = destination;
        isMoving = true;
        npcAnimator.SetBool(IsWalking, true);
    }

    public void MoveToDestination()
    {
        if (destinationPoint != null)
        {
            isMoving = true;
            npcAnimator.SetBool(IsWalking, true);
        }
    }

    public void Stop()
    {
        isMoving = false;
        isSettling = false;
        npcAnimator.SetBool(IsWalking, false);
    }

    private void NotifyDialogueComplete()
    {
        if (DialogueManager.Instance != null)
            DialogueManager.Instance.CompleteDialogueEvent();
    }

    public void PlayFootstepSound()
    {
        if (footstepClips.Count == 0) return;

        int index = Random.Range(0, footstepClips.Count);
        AudioManager.Instance.PlaySfx(footstepClips[index]);
    }
}
