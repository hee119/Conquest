
using UnityEngine;

public class Exp : MonoBehaviour
{
    [Header("플레이어 자석")]
    [SerializeField, Min(0f)] private float attractionDistance = 3f;
    [SerializeField, Min(0f)] private float attractionSpeed = 14f;
    [SerializeField, Min(0f)] private float magnetSpeed = 30f;

    [Header("추적 가속")]
    [SerializeField, Min(0f)] private float acceleration = 20f;
    [SerializeField, Min(0f)] private float distanceAcceleration = 5f;

    private ExpInfo expInfo;
    private PooledObject pooledObject;
    private Transform player;
    private bool magnetized;
    private bool attracted;
    private bool collected;

    private float currentSpeed;

    public bool IsAttracted => attracted || magnetized;

    private void Awake()
    {
        expInfo = GetComponent<ExpInfo>();
        pooledObject = GetComponent<PooledObject>();
    }

    private void OnEnable()
    {
        player = null;
        magnetized = false;
        attracted = false;
        collected = false;
        currentSpeed = 0f;
    }

    public void ActivateMagnet(Transform target)
    {
        if (collected || !gameObject.activeInHierarchy)
            return;

        player = target;
        magnetized = true;
    }

    private void Update()
    {
        if (collected)
            return;

        if (player == null || !player.gameObject.activeInHierarchy)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (player == null)
            return;

        Vector2 offset =
            (Vector2)player.position - (Vector2)transform.position;

        if (!IsAttracted &&
            (attractionDistance <= 0f ||
             offset.sqrMagnitude > attractionDistance * attractionDistance))
            return;

        attracted = true;

        float baseSpeed = magnetized ? magnetSpeed : attractionSpeed;

        // 최초 추적 속도 설정
        currentSpeed = Mathf.Max(currentSpeed, baseSpeed);

        // 시간이 지날수록 가속 + 거리가 멀수록 더 빠르게 가속
        currentSpeed +=
            (acceleration + offset.magnitude * distanceAcceleration)
            * Time.deltaTime;

        transform.position = Vector2.MoveTowards(
            transform.position,
            player.position,
            currentSpeed * Time.deltaTime
        );

        // 플레이어에게 충분히 가까워지면 바로 획득
        Vector2 remaining =
            (Vector2)player.position - (Vector2)transform.position;

        if (remaining.sqrMagnitude <= 0.04f)
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
        if (collected || !gameObject.activeInHierarchy ||
            LevelManager.Instance == null || expInfo == null)
            return;

        collected = true;
        LevelManager.Instance.AddExp(expInfo.exp);

        if (pooledObject != null)
            pooledObject.DestroyObj();
        else
            gameObject.SetActive(false);
    }
}
