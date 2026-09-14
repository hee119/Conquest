using UnityEngine;

public class Chain : Lightning
{
    public UpgradeInfo upgradeInfo;

    protected override void Awake()
    {
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
        }

        id = GetComponent<UpgradeInfo>().id;
        lineRenderer = GetComponent<LineRenderer>();
        pooledObject = GetComponent<PooledObject>();
        upgradeInfo = GetComponent<UpgradeInfo>();
    }

    protected override void OnEnable()
    {
        
    }
    
    public void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            StartCoroutine(DetectUpgradeCor(id, true, collision.gameObject));
        }
    }
}
