using System.Collections.Generic;
using UnityEngine;

public class GroundCrack : MonoBehaviour
{
    [SerializeField] private Transform damageArea;
    [SerializeField] private Transform visual;
    [SerializeField, Min(.1f)] private float radius = 4f;
    private BossPatternInfo info;
    private float elapsed;
    private readonly HashSet<PlayerCombat> damaged = new HashSet<PlayerCombat>();
    private void Awake() => info = GetComponent<BossPatternInfo>();
    private void OnEnable() { elapsed = 0; damaged.Clear(); UpdateArea(); }
    private void Update() { elapsed += Time.deltaTime; UpdateArea(); }
    public void SetRadius(float value) { radius = Mathf.Max(.1f, value); UpdateArea(); }
    private void UpdateArea()
    {
        if (visual != null) visual.localScale = Vector3.one * (radius / 1.3f);
        if (damageArea == null) return;
        float growth = Mathf.SmoothStep(0, 1, Mathf.Clamp01((elapsed - .08f) / .5f));
        damageArea.localScale = new Vector3(radius * growth, radius * .7f * growth, 1);
        var collider = damageArea.GetComponent<Collider2D>();
        if (collider != null) collider.enabled = elapsed >= .1f && elapsed < 1.35f;
    }
    private void OnTriggerEnter2D(Collider2D hit)
    {
        if (!hit.CompareTag("Player") || info == null) return;
        var player = hit.GetComponentInParent<PlayerCombat>();
        if (player != null && damaged.Add(player)) player.TakeDamage(info.damage);
    }
}
