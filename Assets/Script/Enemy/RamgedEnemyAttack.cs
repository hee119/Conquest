using System.Collections;
using UnityEngine;

public class RamgedEnemyAttack : MonoBehaviour
{
    [SerializeField] private int bulletId;
    
    [SerializeField] private GameObject bulleSpawnPos;
    
    private Animator anim;
    private PlayerCombat playerCombat;
    private UnitInfo unitInfo;
    
    public float attackCoolTime = 1f;

    private bool canAttack = true;
    private bool isDead = false;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        playerCombat = GameObject.FindGameObjectWithTag("Player")
            .GetComponent<PlayerCombat>();
        unitInfo = GetComponent<UnitInfo>();
    }
    
    private void OnEnable()
    {
        canAttack = true;
        isDead = false;
        StartCoroutine(AttackCoolTimeCor());
    }
    
    private IEnumerator AttackCoolTimeCor()
    {
        while (true)
        {
            canAttack = false;

            if(anim.GetComponent<AnimationController>() != null) anim.SetTrigger("Attack");
            
            yield return new WaitUntil(() =>
                Vector2.Distance(
                    playerCombat.transform.position,
                    transform.position
                ) <= DataManager.Instance.bulletDatasDic[bulletId].range
            );
                
            GameObject bullet = PoolManager.Instance.Spawn(bulletId, bulleSpawnPos.transform.position);
            bullet.GetComponent<BulletMove>().Target = playerCombat.gameObject;
            yield return new WaitForSeconds(attackCoolTime);

            canAttack = true;
        }
    }
}
