using UnityEngine;
using System.Collections;

public class FaderManager : MonoBehaviour
{
   [SerializeField] private CanvasGroup faderCanvasGroup;
   public float fadeDuration = 1.0f;

    public void StartFadeOut()
    {
         StartCoroutine(FadeOut());
    }

    public void StartFadeIn()
    {
         StartCoroutine(FadeIn());
    }

    public void InstantFadeOut()
    {
        if (faderCanvasGroup != null)
        {
            faderCanvasGroup.alpha = 1.0f;
        }
        
        if (DialogueManager.Instance != null)
       {
           DialogueManager.Instance.CompleteDialogueEvent();
       }
    }

    public void InstantFadeIn()
    {
        if (faderCanvasGroup != null)
        {
            faderCanvasGroup.alpha = 0.0f;
        }

        if (DialogueManager.Instance != null)
       {
           DialogueManager.Instance.CompleteDialogueEvent();
       }
    }

   IEnumerator FadeOut()
   {
       float timer = 0.0f;
       while (timer < fadeDuration)
       {
           timer += Time.deltaTime;
           faderCanvasGroup.alpha = Mathf.Clamp01(timer / fadeDuration);
           yield return null;
       }
       
       if (DialogueManager.Instance != null)
       {
           DialogueManager.Instance.CompleteDialogueEvent();
       }
   }

   IEnumerator FadeIn()
   {
       float timer = 0.0f;
       while (timer < fadeDuration)
       {
           timer += Time.deltaTime;
           faderCanvasGroup.alpha = 1.0f - Mathf.Clamp01(timer / fadeDuration);
           yield return null;
       }
       
       if (DialogueManager.Instance != null)
       {
           DialogueManager.Instance.CompleteDialogueEvent();
       }
   }
}
