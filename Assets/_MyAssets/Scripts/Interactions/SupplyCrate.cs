using Unity.VisualScripting;
using UnityEngine;

public class SupplyCrate : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private AudioClip openSound, closeSound;

    public void OpenCrate()
    {
        if (animator != null)
        {
            AudioManager.Instance.PlaySfx(openSound);
            animator.SetBool("Open", true);
        }
    }

    public void CloseCrate()
    {
        if (animator != null)
        {
            AudioManager.Instance.PlaySfx(closeSound);
            animator.SetBool("Open", false);
        }
    }
}
