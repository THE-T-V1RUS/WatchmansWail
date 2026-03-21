using UnityEngine;

public class NoSignalController : MonoBehaviour
{
    [SerializeField] private SonarMinigameController sonarMinigameController;
    [SerializeField] private Renderer noSignalRenderer;
    [SerializeField] private Material noSignalMaterial;
    [SerializeField] private float powerChangeSpeed = 2.0f;

    private static readonly int PowerId = Shader.PropertyToID("_Power");
    private Material runtimeNoSignalMaterial;
    private float currentPower;

    private void Awake()
    {
        if (noSignalRenderer != null && noSignalMaterial != null)
        {
            runtimeNoSignalMaterial = new Material(noSignalMaterial);
            noSignalRenderer.material = runtimeNoSignalMaterial;

            currentPower = runtimeNoSignalMaterial.HasProperty(PowerId)
                ? runtimeNoSignalMaterial.GetFloat(PowerId)
                : 0f;
        }
    }

    private void Update()
    {
        if (sonarMinigameController != null && runtimeNoSignalMaterial != null)
        {
            //if isReadyForInput is true, power goes to 0 (no signal off) otherwise it checks isScreenOn
            //if isScreenOn is false, power goes to 0 (no signal off) otherwise power goes to 1 (no signal on)
            float targetPower = (!sonarMinigameController.isReadyForInput)
                ? (sonarMinigameController.MonitorIsActive() ? 1f : 0f)
                : 0f;
            currentPower = Mathf.MoveTowards(currentPower, targetPower, powerChangeSpeed * Time.deltaTime);
            runtimeNoSignalMaterial.SetFloat(PowerId, currentPower);
        }
    }

    private void OnDestroy()
    {
        if (runtimeNoSignalMaterial != null)
        {
            Destroy(runtimeNoSignalMaterial);
        }
    }
}
