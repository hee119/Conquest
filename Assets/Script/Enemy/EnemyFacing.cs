using UnityEngine;

// Flip artwork independently of NavMesh movement and boss squash/split scale.
[DisallowMultipleComponent]
[DefaultExecutionOrder(50)]
public sealed class EnemyFacing : MonoBehaviour
{
    [SerializeField] private SpriteRenderer body;
    [SerializeField] private bool facesRightByDefault = true;
    private EnemyMove movement;
    private EnemyCombat combat;
    private Transform fallbackTarget;

    private void Awake()
    {
        if (body == null) body = GetComponentInChildren<SpriteRenderer>(true);
        movement = GetComponent<EnemyMove>();
        combat = GetComponent<EnemyCombat>();
    }

    private void OnEnable()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        fallbackTarget = player != null ? player.transform : null;
        RefreshFacing();
    }

    private void LateUpdate() => RefreshFacing();

    public void RefreshFacing()
    {
        if (body == null || (combat != null && combat.isDead)) return;
        Transform target = movement != null && movement.target != null
            ? movement.target.transform : fallbackTarget;
        if (target == null || !target.gameObject.activeInHierarchy) return;
        float dx = target.position.x - transform.position.x;
        if (Mathf.Abs(dx) < .01f) return;
        bool flip = facesRightByDefault ? dx < 0f : dx > 0f;
        if (body.flipX != flip) body.flipX = flip;
    }
}
