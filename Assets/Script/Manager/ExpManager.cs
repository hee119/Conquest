using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class ExpManager : MonoBehaviour
{
    [Header("구역")]
    public Tilemap tilemap;                 // 구역 기준점(타일맵 좌하단)
    public float chunkSize = 4f;            // 4 x 4 (월드 단위)

    [Header("합치기")]
    public string expTag = "Exp";
    public int mergeCount = 10;             // 몇 개 모이면 합칠지
    public float scanInterval = 0.5f;
    public float moveSpeed = 8f;
    public float arriveDistance = 0.15f;    // 이 거리 안이면 "다 모였다"
    public float maxMergeTime = 3f;         // 이 시간 지나면 그냥 합침
    public int maxId = -1;                  // 이 id 이상은 합치지 않음 (-1이면 제한 없음)

    Vector3 origin;
    readonly HashSet<ExpInfo> merging = new HashSet<ExpInfo>();     // 이미 합치기에 배정된 구슬
    readonly Dictionary<(Vector2Int, int), List<ExpInfo>> groups = new Dictionary<(Vector2Int, int), List<ExpInfo>>();

    void Start()
    {
        origin = tilemap != null ? tilemap.CellToWorld(tilemap.cellBounds.min) : Vector3.zero;
        StartCoroutine(ScanLoop());
    }

    // ───────────── 스캔 ─────────────

    IEnumerator ScanLoop()
    {
        var wait = new WaitForSeconds(scanInterval);
        while (true)
        {
            yield return wait;
            Scan();
        }
    }

    void Scan()
    {
        groups.Clear();

        foreach (GameObject go in GameObject.FindGameObjectsWithTag(expTag))
        {
            ExpInfo info = go.GetComponent<ExpInfo>();
            if (info == null || merging.Contains(info)) continue;   // 진행 중인 구슬은 제외
            if (maxId >= 0 && info.id >= maxId) continue;

            var key = (GetChunk(go.transform.position), info.id);
            if (!groups.TryGetValue(key, out List<ExpInfo> list))
            {
                list = new List<ExpInfo>();
                groups.Add(key, list);
            }
            list.Add(info);
        }

        foreach (var pair in groups)
        {
            List<ExpInfo> list = pair.Value;
            while (list.Count >= mergeCount)
            {
                List<ExpInfo> group = list.GetRange(0, mergeCount);
                list.RemoveRange(0, mergeCount);

                foreach (ExpInfo e in group) merging.Add(e);
                StartCoroutine(MergeRoutine(group, pair.Key.Item2));
            }
        }
    }

    Vector2Int GetChunk(Vector3 pos)
    {
        return new Vector2Int(
            Mathf.FloorToInt((pos.x - origin.x) / chunkSize),
            Mathf.FloorToInt((pos.y - origin.y) / chunkSize));
    }

    // ───────────── 합치기 ─────────────

    IEnumerator MergeRoutine(List<ExpInfo> group, int id)
    {
        // 리더: 무게중심에 가장 가까운 구슬
        Vector3 center = Vector3.zero;
        foreach (ExpInfo e in group) center += e.transform.position;
        center /= group.Count;

        ExpInfo leader = group[0];
        float best = float.MaxValue;
        foreach (ExpInfo e in group)
        {
            float d = (e.transform.position - center).sqrMagnitude;
            if (d < best) { best = d; leader = e; }
        }

        float timer = 0f;
        while (timer < maxMergeTime)
        {
            if (!AllValid(group, id)) { Cancel(group); yield break; }

            Vector3 target = leader.transform.position;
            bool allArrived = true;

            foreach (ExpInfo e in group)
            {
                if (e == leader) continue;
                Transform t = e.transform;
                t.position = Vector3.MoveTowards(t.position, target, moveSpeed * Time.deltaTime);
                if ((t.position - target).sqrMagnitude > arriveDistance * arriveDistance)
                    allArrived = false;
            }

            if (allArrived) break;

            timer += Time.deltaTime;
            yield return null;
        }

        if (!AllValid(group, id)) { Cancel(group); yield break; }

        // 다음 등급 구슬 생성 후 기존 구슬 전부 반환
        GameObject next = PoolManager.Instance.Spawn(id + 1, leader.transform.position);
        if (next == null) { Cancel(group); yield break; }

        foreach (ExpInfo e in group)
        {
            merging.Remove(e);
            PooledObject po = e.GetComponent<PooledObject>();
            if (po != null) po.DestroyObj();
            else e.gameObject.SetActive(false);
        }
    }

    // 도중에 플레이어가 먹어서 사라진 구슬이 있으면 경험치 중복 방지를 위해 합치기 취소
    bool AllValid(List<ExpInfo> group, int id)
    {
        foreach (ExpInfo e in group)
            if (e == null || !e.gameObject.activeInHierarchy || e.id != id)
                return false;
        return true;
    }

    void Cancel(List<ExpInfo> group)
    {
        foreach (ExpInfo e in group) merging.Remove(e);
    }
}