using System;
using System.Collections;
using StarterAssets;
using UnityEngine;

public class ExitLighthouse : MonoBehaviour
{
    [SerializeField] FirstPersonController playerController;
    [SerializeField] AudioClip useDoorSound;

    [SerializeField] Transform lighthouseInteriorSpawnPoint;

    [SerializeField] CanvasGroup faderCanvasGroup;

    public float fadeDuration = 1.0f;
    public float newAmbientVolume = 0.01f;

    private bool _isTransitioning;


    public void transitionToLighthouseInterior()
    {
        if (_isTransitioning)
        {
            return;
        }
        StartCoroutine(TransitionCoroutine());
    }

    IEnumerator TransitionCoroutine()
    {
        _isTransitioning = true;
        FirstPersonController controller = playerController;
        CanvasGroup fader = faderCanvasGroup;
        Transform spawnPoint = lighthouseInteriorSpawnPoint;
        if (controller == null || fader == null || spawnPoint == null)
        {
            _isTransitioning = false;
            yield break;
        }

        controller.canLook = false;
        controller.canMove = false;
        AudioManager.Instance.PlaySfx(useDoorSound);
        
        // Fade to black by turnding canvas group alpha to 1
        float timer = 0.0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            fader.alpha = Mathf.Clamp01(timer / fadeDuration);
            yield return null;
        }

        AudioManager.Instance.SetAmbientVolume(newAmbientVolume);

        // Move player to lighthouse interior spawn point
        CharacterController characterController = playerController.characterController;
        if (characterController != null)
        {
            characterController.enabled = false;
        }
        controller.transform.position = spawnPoint.position;
        controller.transform.rotation = spawnPoint.rotation;
        if (characterController != null)
        {
            characterController.enabled = true;
        }

        controller.canLook = true;
        controller.canMove = true;

        // Fade back in by turning canvas group alpha to 0
        timer = 0.0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            fader.alpha = 1.0f - Mathf.Clamp01(timer / fadeDuration);
            yield return null;
        }

        _isTransitioning = false;
        yield return null;
    }

    private void OnDisable()
    {
        _isTransitioning = false;
        StopAllCoroutines();
    }
}
