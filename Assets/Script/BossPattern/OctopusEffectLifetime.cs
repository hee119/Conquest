using UnityEngine;

public sealed class OctopusEffectLifetime : MonoBehaviour
{
    [SerializeField] private float duration = 2.268f;
    private float elapsed;
    private PooledObject pooled;
    private void Awake() => pooled = GetComponent<PooledObject>();
    private void OnEnable() => elapsed = 0f;
    private void Update()
    {
        elapsed += Time.deltaTime;
        if (elapsed < duration) return;
        if (pooled != null) pooled.DestroyObj();
        else gameObject.SetActive(false);
    }
}
