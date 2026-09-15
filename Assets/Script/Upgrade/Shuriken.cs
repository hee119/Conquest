using UnityEngine;

public class Shuriken : MonoBehaviour
{
    public UpgradeInfo upgradeInfo;

    public void Awake()
    {
        upgradeInfo = GetComponent<UpgradeInfo>();
    }
    
    public void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            Collider2D targetCollider = collision.GetComponent<Collider2D>();
            Vector2 hitPoint = targetCollider.ClosestPoint(transform.position);
            
            EnemyCombat enemyCombat = collision.GetComponent<EnemyCombat>();
            enemyCombat.TakeDamage(DataManager.Instance.UpgradeDatasDic[upgradeInfo.id].damage[DataManager.Instance.UpgradeDatasDic[upgradeInfo.id].UpgradeLevel], hitPoint);
        }
    }
}
