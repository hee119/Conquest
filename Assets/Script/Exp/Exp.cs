using UnityEngine;

public class Exp : MonoBehaviour
{
    [Header("플레이어 자석")]
    [SerializeField, Min(0f)] private float attractionDistance = 3f;
    [SerializeField, Min(0f)] private float attractionSpeed = 14f;
    [SerializeField, Min(0f)] private float magnetSpeed = 30f;

    private ExpInfo expInfo;
    private PooledObject pooledObject;
    private Transform player;
    private bool magnetized;

    private void Awake()
    {
        expInfo = GetComponent<ExpInfo>();
        pooledObject = GetComponent<PooledObject>();
    }

    private void OnEnable()
    {
        player = null;
        magnetized = false;
    }

    public void ActivateMagnet(Transform target)
    {
        player = target;
        magnetized = true;
    }

    private void Update()
    {
        if (player == null || !player.gameObject.activeInHierarchy)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (player == null)
            return;

        Vector2 offset = (Vector2)player.position - (Vector2)transform.position;
        if (!magnetized &&
            (attractionDistance <= 0f || offset.sqrMagnitude > attractionDistance * attractionDistance))
            return;

        float speed = magnetized ? magnetSpeed : attractionSpeed;
        transform.position = Vector2.MoveTowards(
            transform.position,
            player.position,
            speed * Time.deltaTime);

        // A fast magnetized orb can reach the player between physics steps.
        if (magnetized && offset.sqrMagnitude <= 0.04f)
            Collect();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player"))
            return;

        Collect();
    }

    private void Collect()
    {
        if (LevelManager.Instance != null && expInfo != null)
            LevelManager.Instance.AddExp(expInfo.exp);

        if (pooledObject != null)
            pooledObject.DestroyObj();
        else
            gameObject.SetActive(false);
    }
}
