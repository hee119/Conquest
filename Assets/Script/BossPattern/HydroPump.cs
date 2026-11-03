using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(200)]
public class HydroPump : MonoBehaviour
{
    [SerializeField] private float beamBodyTagetScaleY = 3f;
    [SerializeField] private float beamBodyScaleSpeed = 12f;
    [SerializeField] private float beamLengthScale = 4f;
    private BossPatternInfo info;
    private Transform mouth;
    private Collider2D damageCollider;
    private SpriteRenderer visual;

    private void Awake()
    {
        info = GetComponent<BossPatternInfo>();
        damageCollider = GetComponent<Collider2D>();
        visual = GetComponentInChildren<SpriteRenderer>();
    }

    private void OnEnable()
    {
        mouth = null;
        transform.localScale = new Vector3(beamLengthScale, 0, 1);
        if (damageCollider != null) damageCollider.enabled = false;
    }

    private void OnDisable() { StopAllCoroutines(); mouth = null; }
    public void FollowMouth(Transform anchor) { mouth = anchor; LateUpdate(); }

    private void LateUpdate()
    {
        if (mouth == null) return;
        transform.SetPositionAndRotation(mouth.position, mouth.rotation);
        if (visual != null) visual.sortingOrder = Mathf.Sin(mouth.eulerAngles.z * Mathf.Deg2Rad) > .15f ? -1 : 10;
    }

    public IEnumerator BeamBodyScale(int direction)
    {
        float target = direction >= 1 ? beamBodyTagetScaleY : 0f;
        while (!Mathf.Approximately(transform.localScale.y, target))
        {
            float y = Mathf.MoveTowards(transform.localScale.y, target, Mathf.Max(.1f, beamBodyScaleSpeed) * Time.deltaTime);
            transform.localScale = new Vector3(beamLengthScale, y, 1);
            if (damageCollider != null) damageCollider.enabled = y > .05f;
            yield return null;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || info == null) return;
        other.GetComponentInParent<PlayerCombat>()?.TakeDamage(info.damage);
    }
}
