using UnityEngine;

public class SnapToTerrain : MonoBehaviour
{
    [Header("Snapping")]
    [SerializeField] private float heightOffset = 0f;
    [SerializeField] private bool smoothSnap = true;
    [SerializeField] private float snapSpeed = 12f;
    [SerializeField] private bool useRaycast = true;
    [SerializeField] private float rayStartHeight = 10f;
    [SerializeField] private float rayDistance = 200f;
    [SerializeField] private LayerMask groundMask = ~0;

    [Header("Rotation")]
    [SerializeField] private bool alignToGroundNormal = false;
    [SerializeField] private float rotationLerpSpeed = 10f;

    private void LateUpdate()
    {
        if (!TryGetGround(transform.position, out Vector3 groundPoint, out Vector3 groundNormal))
        {
            return;
        }

        Vector3 position = transform.position;
        float targetY = groundPoint.y + heightOffset;

        if (smoothSnap)
        {
            float lerpT = 1f - Mathf.Exp(-snapSpeed * Time.deltaTime);
            position.y = Mathf.Lerp(position.y, targetY, lerpT);
        }
        else
        {
            position.y = targetY;
        }

        transform.position = position;

        if (alignToGroundNormal)
        {
            Quaternion current = transform.rotation;
            Vector3 forwardOnPlane = Vector3.ProjectOnPlane(transform.forward, groundNormal);
            if (forwardOnPlane.sqrMagnitude < 0.0001f)
            {
                forwardOnPlane = Vector3.ProjectOnPlane(transform.up, groundNormal);
            }

            if (forwardOnPlane.sqrMagnitude > 0.0001f)
            {
                Quaternion target = Quaternion.LookRotation(forwardOnPlane.normalized, groundNormal);
                float rotT = 1f - Mathf.Exp(-rotationLerpSpeed * Time.deltaTime);
                transform.rotation = Quaternion.Slerp(current, target, rotT);
            }
        }
    }

    private bool TryGetGround(Vector3 currentPosition, out Vector3 groundPoint, out Vector3 groundNormal)
    {
        if (useRaycast)
        {
            Vector3 rayOrigin = currentPosition + Vector3.up * rayStartHeight;
            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, rayDistance, groundMask, QueryTriggerInteraction.Ignore))
            {
                groundPoint = hit.point;
                groundNormal = hit.normal;
                return true;
            }
        }

        Terrain terrain = Terrain.activeTerrain;
        if (terrain != null)
        {
            float sampledHeight = terrain.SampleHeight(currentPosition) + terrain.transform.position.y;
            groundPoint = new Vector3(currentPosition.x, sampledHeight, currentPosition.z);
            Vector3 terrainPos = currentPosition - terrain.transform.position;
            Vector3 terrainNormal = terrain.terrainData.GetInterpolatedNormal(
                Mathf.InverseLerp(0f, terrain.terrainData.size.x, terrainPos.x),
                Mathf.InverseLerp(0f, terrain.terrainData.size.z, terrainPos.z)
            );
            groundNormal = terrain.transform.TransformDirection(terrainNormal).normalized;
            return true;
        }

        groundPoint = currentPosition;
        groundNormal = Vector3.up;
        return false;
    }
}
