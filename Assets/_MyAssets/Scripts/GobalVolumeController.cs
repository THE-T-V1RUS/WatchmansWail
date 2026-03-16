using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public class GobalVolumeController : MonoBehaviour
{
    [SerializeField] private Volume volume;

    private Fog _fog;
    public float defaultFogAttenuationDistance = 60f;

    private void Awake()
    {
        if (volume != null && volume.profile.TryGet<Fog>(out Fog fog))
        {
            _fog = fog;
        }
        else
        {
            Debug.LogWarning("GobalVolumeController: No Volume assigned or profile has no Fog override.", this);
        }
    }

    public void SetFogAttenuationDistance(float distance)
    {
        if (_fog != null)
        {
            _fog.meanFreePath.Override(distance);
        }
    }

    public void ResetFogAttenuationDistance()
    {
        if (_fog != null)
        {
            _fog.meanFreePath.Override(defaultFogAttenuationDistance); // Default value
        }
    }
}
