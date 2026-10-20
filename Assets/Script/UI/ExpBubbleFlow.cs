using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ExpBubbleFlow : MonoBehaviour
{
    // =========================================================
    // Pool
    // =========================================================

    [Header("거품 Pool ID")]
    [SerializeField]
    private int id;


    // =========================================================
    // 생성 설정
    // =========================================================

    [Header("거품 생성 간격")]

    [SerializeField]
    private float minSpawnInterval = 0.08f;

    [SerializeField]
    private float maxSpawnInterval = 0.2f;


    [Header("묶음 생성")]

    [Tooltip("한 묶음에서 생성할 최소 거품 개수")]
    [SerializeField, Min(1)]
    private int minSpawnCount = 4;

    [Tooltip("한 묶음에서 생성할 최대 거품 개수")]
    [SerializeField, Min(1)]
    private int maxSpawnCount = 8;


    [Header("묶음 생성 후 대기 시간")]

    [Tooltip("한 묶음 생성 후 최소 대기 시간")]
    [SerializeField, Min(0f)]
    private float minSpawnCooldown = 0.8f;

    [Tooltip("한 묶음 생성 후 최대 대기 시간")]
    [SerializeField, Min(0f)]
    private float maxSpawnCooldown = 1.8f;


    // =========================================================
    // 크기
    // =========================================================

    [Header("거품 크기")]

    [SerializeField]
    private float minBubbleSize = 4f;

    [SerializeField]
    private float maxBubbleSize = 10f;


    // =========================================================
    // 이동
    // =========================================================

    [Header("이동 속도")]

    [SerializeField]
    private float minMoveSpeed = 40f;

    [SerializeField]
    private float maxMoveSpeed = 80f;


    // =========================================================
    // 뽀글거림
    // =========================================================

    [Header("뽀글거림")]

    [Tooltip("위아래로 흔들리는 거리")]
    [SerializeField]
    private float minWaveAmount = 1f;

    [SerializeField]
    private float maxWaveAmount = 4f;


    [Tooltip("위아래로 흔들리는 속도")]
    [SerializeField]
    private float minWaveSpeed = 4f;

    [SerializeField]
    private float maxWaveSpeed = 9f;


    // =========================================================
    // 투명도
    // =========================================================

    [Header("투명도")]

    [SerializeField, Range(0f, 1f)]
    private float maxAlpha = 0.8f;


    [Header("끝부분 페이드")]

    [Tooltip("Fill 끝 몇 픽셀 전부터 사라질지")]
    [SerializeField]
    private float fadeDistance = 10f;


    // =========================================================
    // 내부
    // =========================================================

    private RectTransform rect;

    private Coroutine spawnCoroutine;


    // 현재 사용 중인 거품
    private readonly List<PooledObject> activeBubbles =
        new List<PooledObject>();


    // =========================================================
    // Awake
    // =========================================================

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
    }


    // =========================================================
    // Enable
    // =========================================================

    private void OnEnable()
    {
        spawnCoroutine =
            StartCoroutine(
                SpawnLoop()
            );
    }


    // =========================================================
    // Disable
    // =========================================================

    private void OnDisable()
    {
        // 생성 Coroutine 중지
        if (spawnCoroutine != null)
        {
            StopCoroutine(
                spawnCoroutine
            );

            spawnCoroutine = null;
        }


        // =====================================================
        // UI가 꺼질 경우
        // 현재 남아있는 거품 모두 Pool 반환
        // =====================================================

        for (
            int i = activeBubbles.Count - 1;
            i >= 0;
            i--
        )
        {
            PooledObject pooledObject =
                activeBubbles[i];


            if (
                pooledObject != null &&
                pooledObject.gameObject.activeSelf
            )
            {
                pooledObject.DestroyObj();
            }
        }


        activeBubbles.Clear();
    }


    // =========================================================
    // Spawn Loop
    //
    // 예:
    //
    // ○ ○ ○ ○ ○ ○
    //       ↓
    //    1.5초 대기
    //       ↓
    // ○ ○ ○ ○ ○ ○
    //
    // =========================================================

    private IEnumerator SpawnLoop()
    {
        // PoolManager 초기화 대기
        while (
            PoolManager.Instance == null ||
            !PoolManager.Instance.prefabIsLoaded
        )
        {
            yield return null;
        }


        while (true)
        {
            // =========================================
            // 이번 묶음에서 생성할 개수 랜덤
            // int Random.Range는 max가 포함되지 않으므로 +1
            // =========================================

            int targetSpawnCount =
                Random.Range(
                    minSpawnCount,
                    maxSpawnCount + 1
                );


            int currentSpawnCount = 0;


            // =========================================
            // 랜덤으로 뽑힌 개수만큼 생성
            // =========================================

            while (
                currentSpawnCount <
                targetSpawnCount
            )
            {
                bool spawned =
                    SpawnBubble();


                if (spawned)
                {
                    currentSpawnCount++;
                }


                // 거품 하나하나 생성 간격도 랜덤
                float waitTime =
                    Random.Range(
                        minSpawnInterval,
                        maxSpawnInterval
                    );


                yield return
                    new WaitForSeconds(
                        waitTime
                    );
            }


            // =========================================
            // 이번 묶음이 끝난 뒤
            // 다음 묶음까지 대기 시간 랜덤
            // =========================================

            float cooldown =
                Random.Range(
                    minSpawnCooldown,
                    maxSpawnCooldown
                );


            if (cooldown > 0f)
            {
                yield return
                    new WaitForSeconds(
                        cooldown
                    );
            }
        }
    }

    // =========================================================
    // Bubble 생성
    // =========================================================

    private bool SpawnBubble()
    {
        if (PoolManager.Instance == null)
            return false;


        // =====================================================
        // Fill 폭이 거의 없으면 생성하지 않음
        // =====================================================

        if (rect.rect.width <= 3f)
            return false;


        // =====================================================
        // Pool에서 거품 가져오기
        // =====================================================

        GameObject bubbleObject =
            PoolManager.Instance.SpawnUI(
                id,
                transform
            );


        if (bubbleObject == null)
            return false;


        // =====================================================
        // Components
        // =====================================================

        RectTransform bubbleRect =
            bubbleObject.GetComponent<
                RectTransform
            >();


        Image bubbleImage =
            bubbleObject.GetComponent<Image>();


        PooledObject pooledObject =
            bubbleObject.GetComponent<
                PooledObject
            >();


        // =====================================================
        // 검사
        // =====================================================

        if (
            bubbleRect == null ||
            bubbleImage == null ||
            pooledObject == null
        )
        {
            Debug.LogWarning(
                "[ExpBubbleFlow] Bubble Prefab에 " +
                "RectTransform / Image / PooledObject 중 " +
                "하나가 없습니다."
            );


            if (pooledObject != null)
            {
                pooledObject.DestroyObj();
            }


            return false;
        }


        // =====================================================
        // 사용중 목록 등록
        // =====================================================

        activeBubbles.Add(
            pooledObject
        );


        // =====================================================
        // Image
        // =====================================================

        bubbleImage.enabled = true;

        bubbleImage.raycastTarget = false;

        bubbleImage.preserveAspect = true;

        bubbleImage.maskable = true;


        // =====================================================
        // 랜덤 크기
        // =====================================================

        float size =
            Random.Range(
                minBubbleSize,
                maxBubbleSize
            );


        bubbleRect.sizeDelta =
            new Vector2(
                size,
                size
            );


        bubbleRect.localScale =
            Vector3.one;


        bubbleRect.localRotation =
            Quaternion.identity;


        // =====================================================
        // Anchor
        // =====================================================

        bubbleRect.anchorMin =
            new Vector2(
                0f,
                0.5f
            );


        bubbleRect.anchorMax =
            new Vector2(
                0f,
                0.5f
            );


        bubbleRect.pivot =
            new Vector2(
                0.5f,
                0.5f
            );


        // =====================================================
        // Y 위치 랜덤
        // =====================================================

        float halfHeight =
            rect.rect.height *
            0.5f;


        float yRange =
            Mathf.Max(
                0f,
                halfHeight -
                size * 0.5f -
                maxWaveAmount
            );


        float y =
            Random.Range(
                -yRange,
                yRange
            );


        // =====================================================
        // 왼쪽에서 시작
        // =====================================================

        bubbleRect.anchoredPosition =
            new Vector2(
                size * 0.5f,
                y
            );


        // 항상 UI에서 위쪽에 표시
        bubbleRect.SetAsLastSibling();


        // =====================================================
        // 색 초기화
        // =====================================================

        Color color =
            Color.white;


        color.a =
            maxAlpha;


        bubbleImage.color =
            color;


        // =====================================================
        // 이동 시작
        // =====================================================

        StartCoroutine(
            MoveBubble(
                bubbleObject,
                bubbleRect,
                bubbleImage,
                pooledObject
            )
        );


        return true;
    }


    // =========================================================
    // Bubble 이동
    // =========================================================

    private IEnumerator MoveBubble(
        GameObject bubbleObject,
        RectTransform bubble,
        Image image,
        PooledObject pooledObject
    )
    {
        // =====================================================
        // 이동 속도
        // =====================================================

        float speed =
            Random.Range(
                minMoveSpeed,
                maxMoveSpeed
            );


        // =====================================================
        // 흔들리는 크기
        // =====================================================

        float waveAmount =
            Random.Range(
                minWaveAmount,
                maxWaveAmount
            );


        // =====================================================
        // 흔들리는 속도
        // =====================================================

        float waveSpeed =
            Random.Range(
                minWaveSpeed,
                maxWaveSpeed
            );


        // =====================================================
        // 거품마다 파형 시작 위치 랜덤
        //
        // 모든 거품이 똑같이 흔들리는 것 방지
        // =====================================================

        float waveOffset =
            Random.Range(
                0f,
                Mathf.PI * 2f
            );


        float time = 0f;


        float startY =
            bubble.anchoredPosition.y;


        // =====================================================
        // Fill 끝까지 이동
        // =====================================================

        while (
            bubbleObject != null &&
            bubbleObject.activeInHierarchy
        )
        {
            time +=
                Time.deltaTime;


            Vector2 position =
                bubble.anchoredPosition;


            // =================================================
            // 오른쪽 이동
            // =================================================

            position.x +=
                speed *
                Time.deltaTime;


            // =================================================
            // 위아래 뽀글뽀글
            // =================================================

            float verticalLimit =
                Mathf.Max(
                    0f,
                    rect.rect.height * 0.5f -
                    bubble.rect.height * 0.5f
                );


            position.y = Mathf.Clamp(
                startY +
                Mathf.Sin(
                    time *
                    waveSpeed +
                    waveOffset
                ) *
                waveAmount,
                -verticalLimit,
                verticalLimit
            );


            bubble.anchoredPosition =
                position;


            // =================================================
            // 현재 Fill 끝
            //
            // ExpBubbleFlow가
            // Galaxy Fill Mask 내부의
            // Bubble Layer에 붙어 있다면
            //
            // rect.rect.width =
            // 현재 Fill의 가로 길이
            // =================================================

            float fillEnd =
                rect.rect.width;


            float bubbleRadius =
                bubble.rect.width *
                0.5f;


            float bubbleRight =
                position.x +
                bubbleRadius;


            // =================================================
            // 끝에 가까워지면 Fade
            // =================================================

            float distanceToEnd =
                fillEnd -
                bubbleRight;


            float actualFadeDistance =
                Mathf.Max(
                    0.01f,
                    fadeDistance
                );


            float alpha =
                Mathf.Clamp01(
                    distanceToEnd /
                    actualFadeDistance
                );


            alpha *=
                maxAlpha;


            Color color =
                Color.white;


            color.a =
                alpha;


            image.color =
                color;


            // =================================================
            // Fill 끝 도착
            // =================================================

            if (bubbleRight >= fillEnd)
            {
                activeBubbles.Remove(
                    pooledObject
                );


                // =============================================
                // Destroy가 아니라
                // 자기 ObjectPool에 반환
                // =============================================

                pooledObject.DestroyObj();


                yield break;
            }


            yield return null;
        }


        // =====================================================
        // 다른 이유로 비활성화됐다면
        // List 정리
        // =====================================================

        activeBubbles.Remove(
            pooledObject
        );
    }
}
