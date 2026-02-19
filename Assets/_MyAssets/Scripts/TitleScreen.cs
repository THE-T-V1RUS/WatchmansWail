using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class TitleScreen : MonoBehaviour
{
    [SerializeField] private FaderManager faderManager;
    [SerializeField] private Button startButton, quitButton;
    [SerializeField] private DialogueTrigger openingDialogue;
    [SerializeField] private AudioClip audioStartGame;
    [SerializeField] private GameObject titleScreenCamera, player;

    public void StartGame()
    {
        StartCoroutine(StartGameCoroutine());
    }

    IEnumerator StartGameCoroutine()
    {
        AudioManager.Instance.PlaySfx(audioStartGame);
        startButton.interactable = false;
        quitButton.interactable = false;
        faderManager.StartFadeOut();
        yield return new WaitForSeconds(faderManager.fadeDuration);
        AudioManager.Instance.FadeAmbientVolume(1, 1);
        player.SetActive(true);
        openingDialogue.TriggerDialogue();
        titleScreenCamera.SetActive(false); 
        this.gameObject.SetActive(false);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
