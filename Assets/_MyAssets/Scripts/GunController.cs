using UnityEngine;

public class GunController : MonoBehaviour
{
    private Animator animator;
    public bool IsEquipped = false;
    public bool canUseGun = true;
    public bool canShoot = false;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void EquipGun()
    {
        animator.SetBool("Equipped", true);
    }

    public void UnequipGun()
    {
        animator.SetBool("Equipped", false);
    }

    public void SetSatusAsEquipped()
    {
        IsEquipped = true;
    }

    public void SetStatusAsUnequipped()
    {
        IsEquipped = false;
    }

    public void EnableGunUsage()
    {
        canUseGun = true;
    }

    public void DisableGunUsage()
    {
        canUseGun = false;
    }

    public void ShootGun()
    {
        if (animator.GetBool("Equipped") && canUseGun && canShoot)
        {
            canShoot = false;
            animator.SetTrigger("Shoot");
        }
    }

    public void EnableShooting()
    {
        canShoot = true;
    }

}
