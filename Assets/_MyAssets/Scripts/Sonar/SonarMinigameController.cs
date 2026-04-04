using System;
using System.Collections;
using StarterAssets;
using Unity.VisualScripting;
using UnityEngine;

public class SonarMinigameController : MonoBehaviour
{
    [SerializeField] private bool isMiniGameActive = false;
    public bool isReadyForInput = false;
    [SerializeField] private bool isSonarPingEnabled = false;
    [SerializeField] private bool isScreenOn = false;
    private const string UnlitColorProperty = "_UnlitColor";

    [Header("Sonar Material")]
    [SerializeField] private Renderer targetRendererA;

    [Header("Boat Material")]
    [SerializeField] private Renderer targetRendererB;

    [Header("Screen State")]
    [SerializeField] private float screenFadeDuration = 0.35f;

    [Header("Boat Controller")]
    [SerializeField] private BoatController boatController;

    [Header("Sonar Ping Controller")]
    [SerializeField] private SonarPingController sonarPingController;

    private Material runtimeMaterialA;
    private Material runtimeMaterialB;
    private Coroutine screenFadeRoutine;
    private bool previousScreenOnState;
    private bool previousSonarPingEnabledState;


    [SerializeField] private GameObject sonarScreen, boatScreen;

    [SerializeField] private AudioClip steerShipMusic, stingSuccess, stingFailure;
    [SerializeField] private CameraMover playerCameraMover;

    public DialogueTrigger BadResultsDialogueTrigger, GoodResultsDialogueTrigger;

    private void Awake()
    {
        ApplyRuntimeMaterialCopies();
        previousScreenOnState = isScreenOn;
        previousSonarPingEnabledState = isSonarPingEnabled;
        if (sonarPingController != null)
        {
            sonarPingController.PingEnabled = isSonarPingEnabled;
        }
    }

    private void Start()
    {
        SetMaterialsToColor(isScreenOn ? Color.white : Color.black);
    }

    private void Update()
    {
        if (previousScreenOnState != isScreenOn)
        {
            BeginScreenFade(isScreenOn);
            previousScreenOnState = isScreenOn;
        }

        if (sonarPingController != null)
        {
            if (previousSonarPingEnabledState != isSonarPingEnabled)
            {
                sonarPingController.PingEnabled = isSonarPingEnabled;
            }
            else if (sonarPingController.PingEnabled != isSonarPingEnabled)
            {
                isSonarPingEnabled = sonarPingController.PingEnabled;
            }
            previousSonarPingEnabledState = isSonarPingEnabled;
        }

        if (isMiniGameActive)
        {
            if (boatController.HasReachedEnd || boatController.boatIsDead())
            {
                if(boatController.boatIsDead())
                {
                    AudioManager.Instance.PlaySfx(stingFailure);
                }
                else
                {
                    AudioManager.Instance.PlaySfx(stingSuccess);
                }

                isMiniGameActive = false;
                EndMinigame();
            }
        }
    }

    private void OnDestroy()
    {
        if (screenFadeRoutine != null)
        {
            StopCoroutine(screenFadeRoutine);
            screenFadeRoutine = null;
        }

        if (runtimeMaterialA != null)
        {
            Destroy(runtimeMaterialA);
        }

        if (runtimeMaterialB != null)
        {
            Destroy(runtimeMaterialB);
        }
    }

    public void ApplyRuntimeMaterialCopies()
    {
        runtimeMaterialA = GetRuntimeMaterialFromRenderer(targetRendererA, runtimeMaterialA);
        runtimeMaterialB = GetRuntimeMaterialFromRenderer(targetRendererB, runtimeMaterialB);
    }

    public Material GetRuntimeMaterialA()
    {
        return runtimeMaterialA;
    }

    public Material GetRuntimeMaterialB()
    {
        return runtimeMaterialB;
    }

    public void SetScreenOn(bool value)
    {
        if (isScreenOn == value)
        {
            return;
        }

        isScreenOn = value;
        BeginScreenFade(isScreenOn);
        previousScreenOnState = isScreenOn;
    }

    private Material GetRuntimeMaterialFromRenderer(Renderer targetRenderer, Material existingRuntimeMaterial)
    {
        if (targetRenderer == null)
        {
            return null;
        }

        if (existingRuntimeMaterial != null)
        {
            Destroy(existingRuntimeMaterial);
        }

        Material sourceMaterial = targetRenderer.sharedMaterial;
        if (sourceMaterial == null)
        {
            return null;
        }

        Material runtimeCopy = new Material(sourceMaterial);
        runtimeCopy.name = sourceMaterial.name + " (Runtime)";
        targetRenderer.sharedMaterial = runtimeCopy;
        return runtimeCopy;
    }

    private void BeginScreenFade(bool turnOn)
    {
        if (screenFadeRoutine != null)
        {
            StopCoroutine(screenFadeRoutine);
            screenFadeRoutine = null;
        }

        Color targetColor = turnOn ? Color.white : Color.black;
        Color startColorA = GetMaterialColor(runtimeMaterialA);
        Color startColorB = GetMaterialColor(runtimeMaterialB);
        screenFadeRoutine = StartCoroutine(FadeScreenColors(startColorA, startColorB, targetColor));
    }

    private System.Collections.IEnumerator FadeScreenColors(Color startColorA, Color startColorB, Color targetColor)
    {
        if (screenFadeDuration <= 0f)
        {
            SetMaterialsToColor(targetColor);
            screenFadeRoutine = null;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < screenFadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / screenFadeDuration);

            if (runtimeMaterialA != null)
            {
                SetMaterialColor(runtimeMaterialA, Color.Lerp(startColorA, targetColor, t));
            }

            if (runtimeMaterialB != null)
            {
                SetMaterialColor(runtimeMaterialB, Color.Lerp(startColorB, targetColor, t));
            }

            yield return null;
        }

        SetMaterialsToColor(targetColor);
        screenFadeRoutine = null;
    }

    private void SetMaterialsToColor(Color color)
    {
        SetMaterialColor(runtimeMaterialA, color);
        SetMaterialColor(runtimeMaterialB, color);
    }

    private Color GetMaterialColor(Material material)
    {
        if (material == null)
        {
            return Color.black;
        }

        if (material.HasProperty(UnlitColorProperty))
        {
            return material.GetColor(UnlitColorProperty);
        }

        Debug.LogWarning($"SonarMinigameController: Material '{material.name}' on shader '{material.shader.name}' has no '{UnlitColorProperty}' property.");

        return Color.black;
    }

    private void SetMaterialColor(Material material, Color color)
    {
        if (material == null)
        {
            return;
        }

        if (material.HasProperty(UnlitColorProperty))
        {
            material.SetColor(UnlitColorProperty, color);
            return;
        }

        Debug.LogWarning($"SonarMinigameController: Could not set '{UnlitColorProperty}' for material '{material.name}' on shader '{material.shader.name}'.");
    }

    public bool MonitorIsActive()
    {
        return isScreenOn;
    }

    public bool SonarIsActive()
    {
        return isSonarPingEnabled;
    }

    public bool MiniGameIsActive()
    {
        return isMiniGameActive;
    }

    public void ToggleInputReady(bool value)
    {
        isReadyForInput = value;
        sonarScreen.SetActive(value);
        boatScreen.SetActive(value);
    }

    public void StartMinigame()
    {
        if (boatController != null)
        {
            isMiniGameActive = true;
            boatController.SetDriftForwardEnabled(true);
            AudioManager.Instance.ChangeMusicClip(steerShipMusic);
            AudioManager.Instance.FadeInMusic(1);
        }
        else
        {
            Debug.LogWarning("SonarMinigameController: Cannot start minigame because BoatController reference is missing.");
        }
    }

    public void SetGoodResultsDialogueTrigger(DialogueTrigger trigger)
    {
        GoodResultsDialogueTrigger = trigger;
    }

    public void SetBadResultsDialogueTrigger(DialogueTrigger trigger)
    {
        BadResultsDialogueTrigger = trigger;
    }

    public void EndMinigame()
    {
        isReadyForInput = false;
        sonarScreen.SetActive(false);
        boatScreen.SetActive(false);
        boatController.SetDriftForwardEnabled(false);
        AudioManager.Instance.FadeOutMusic(1);
        StartCoroutine(EndMinigameCoroutine());
    }

    IEnumerator EndMinigameCoroutine()
    {
        // Slight delay before returning camera to player...
        yield return new WaitForSeconds(1f);
        playerCameraMover.ReturnToPlayer();

        if (boatController.boatIsDead())
        {
            BadResultsDialogueTrigger.TriggerDialogue();
        }
        else
        {
            GoodResultsDialogueTrigger.TriggerDialogue();
        }

        boatController.ResetBoatState();
    }
}
