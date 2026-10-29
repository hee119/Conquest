# Conquest 성능 최적화 조사

작성: 2026-10-26 / Unity 6000.3.10f1

## 조사 범위와 한계

- 사용자 영상: [Unity AI와 함께하는 성능 프로파일링](https://www.youtube.com/watch?v=WRM7avaCuD4), Unity Korea, 36분 21초.
- 영상의 제목·공개 설명과 [Unity Korea 공식 소개 글](https://unitysquare.co.kr/growwith/unityblog/webinarView?id=787)을 확인했다. 영상 자막 본문 요청은 실패했다. 따라서 영상 전체를 시청하거나 세부 발언을 검증한 것으로 간주하지 않는다.
- 추가 글 [AI에게 Unity 성능 실험 루프 맡기기: 로그 수집부터 패치 후보까지](https://kr-gamedev.tistory.com/52)의 본문을 읽었다.
- 프로젝트 C#·프리팹·설정을 읽고, Editor API로 현재 충돌 레이어 설정을 확인했다.
- 이번 조사는 실행 코드나 물리 설정을 변경하지 않았다. 새 성능 캡처와 수정 전후 측정은 수행하지 않았다. 아래 우선순위는 실험 순서이며, 측정으로 확정된 비용 순위가 아니다.

## 자료에서 적용할 원칙

영상의 공식 소개는 수집한 프로파일링 정보를 AI로 해석하고, 다음 점검 대상을 찾는 과정을 설명한다. 추가 글은 계측 위치·로그 형식·수정 범위·검증 조건을 먼저 고정하는 방법을 제안한다. 프로젝트에서는 이를 다음 순서로 적용한다.

1. 적 수·투사체 수·경험치 수·장비·웨이브·플레이어 동선을 고정한다.
2. CPU와 Physics 2D 캡처로 기준값을 저장한다.
3. 병목 가설 하나만 바꾼다.
4. 같은 상황을 다시 실행해 비용과 게임플레이를 비교한다.

## 프로젝트에서 확인한 사실과 실험 후보

| 순서 | 확인한 사실 | 첫 실험 | 확인할 결과 |
|---|---|---|---|
| 1 | 현재 Enemy↔Enemy는 이미 비활성화되어 있다. Enemy↔Exp, Exp↔Exp, Skill↔Skill, Default↔Enemy/Exp/Default 등은 허용되어 있다. FireBall·Missile·Heatwave·Poison 프리팹 루트는 Default 레이어다. | 실제 공격 콜라이더의 레이어를 정리하고, 공격 주체·대상별로 필요한 접촉만 남기는 실험을 한다. Default 전체 차단은 하지 않는다. | Broadphase Pairs, Added Contacts, Total Contacts, FindNewContacts의 ms. 적 피격·플레이어 피격·벽 충돌·경험치 획득 보존. |
| 2 | DetectUpgrade.DetectEnemies와 DetectEnemiesFromPosition은 OverlapCircleAll 반환 배열을 다시 새 List로 복사한다. 적이 없으면 DetectUpgradeCor가 다음 프레임에 같은 과정을 반복한다. HasEnemyInRange도 전체 목록을 만든 뒤 Count만 확인한다. | 충분한 용량의 호출자 소유 재사용 버퍼와 Physics2D.OverlapCircle 오버로드를 사용한다. 존재 확인은 단일 결과 조회로 분리한다. | 해당 마커의 GC Alloc, 호출 횟수, 프레임별 누적 시간. 감지 대상·순서·공격 수 동일. |
| 3 | EnemyMove는 기본 0.35초 간격과 타깃 이동 임계값을 이미 사용한다. OnEnable에서 모든 개체의 nextPathUpdateTime을 0으로 둔다. 적 프리팹 9개는 Dynamic Rigidbody2D와 NavMeshAgent 이동을 함께 사용한다. | 생성이 몰릴 때 경로 갱신 시점이 겹치는지 측정한다. NavMesh 이동과 물리 위치 갱신의 역할을 점검한다. | 경로 계산 p95/최댓값, Transform Sync Bodies/Colliders. 추적·넉백·장애물 회피 결과 보존. |
| 4 | 경험치 프리팹 3개에는 Rigidbody2D가 없고, Exp.Update가 Transform을 직접 이동시킨다. | 경험치 밀집 장면에서 이동하는 콜라이더와 불필요한 레이어 접촉의 비용을 분리 측정한다. 필요하면 물리 이동 방식 변경을 별도 실험한다. | Transform Sync Colliders, Broadphase Updates/Pairs, 자석 흡수·경험치 총량 보존. |
| 5 | ObjectPool은 이미 도입되어 있다. SetPool의 defaultCapacity:10은 선생성이 아니다. 풀 객체 활성화는 물리 접촉 생성 비용을 여전히 발생시킬 수 있다. | 스폰이 몰리는 프레임의 신규 생성 수와 콜라이더 활성화 수를 각각 계측한다. 첫 사용 생성이 원인일 때만 사전 준비를 추가한다. | Instantiate/Spawn 시간, Added Contacts, 활성 수, 메모리. 스폰 수·시점 보존. |

### 1. 충돌 레이어를 우선 점검할 이유

이전에 받은 스크린샷은 Physics2D.FindNewContacts의 비중 46.1%만 보여 준다. 이것만으로 절대 비용이나 원인 오브젝트를 확정할 수 없다. 다만 충돌 후보 탐색과 접촉 수를 확인해야 한다는 직접적인 단서다.

현재 Enemy↔Enemy는 이미 꺼져 있으므로 이를 다시 끄는 제안은 실효가 없다. 공격 프리팹의 Default 레이어 사용과 경험치 관련 접촉을 먼저 확인해야 한다. Default에는 벽이나 다른 필수 오브젝트가 섞일 수 있으므로, 레이어를 먼저 구분한 다음 필요한 조합만 허용한다.

Unity 문서상 Broadphase Pairs에는 실제 접촉으로 이어지는 쌍과 필터로 무시되는 후보가 포함된다. 레이어를 정리해도 모든 broadphase 탐색이 사라진다고 가정하면 안 된다. 콜라이더 수·크기·겹침과 활성화 빈도를 함께 측정한다. [Physics 2D Profiler 문서](https://docs.unity.com/en-us/engine/6000.3/manual/unity2d/2d-physics/physics-2d-profiler/module-reference)

### 2. 탐색 버퍼 재사용 시 주의할 점

관련 위치:

- `Assets/Script/Upgrade/DetectUpgrade.cs:132` — 플레이어 주변 전체 탐색
- `Assets/Script/Upgrade/DetectUpgrade.cs:152` — 특정 위치 주변 전체 탐색
- `Assets/Script/Upgrade/DetectUpgrade.cs:283` — 존재 확인을 위해 전체 목록 생성
- `Assets/Script/Upgrade/DetectUpgrade.cs:69` — 적이 없으면 다음 프레임 재탐색

여러 코루틴이 공용 DetectUpgrade를 호출하므로 하나의 전역 List를 무조건 공유하면 안 된다. 코루틴이 목록을 보관하거나 제거하는 동안 다른 탐색이 같은 버퍼를 덮어쓸 수 있다. 호출자별 버퍼 또는 명확한 대여·반환 수명으로 설계한다. 고정 배열은 결과 수가 용량과 같을 때 잘린 결과인지 확인하고, List는 최대 밀집 상황을 감당할 용량을 준비한다.

2D에서는 새 NonAlloc 접미사 API를 추가하기보다 배열/List를 받는 OverlapCircle 오버로드를 사용한다. 충분한 List 용량을 재사용하면 결과 저장을 위한 반복 할당을 줄일 수 있다. [Unity 공식 API](https://docs.unity.cn/6000.1/Documentation/ScriptReference/Physics2D.OverlapCircle.html)

이 변경은 탐색·GC 비용을 줄이는 후보이며, FindNewContacts 비용까지 직접 해결한다고 단정하지 않는다. 탐색 주기를 늦추는 변경은 공격 반응성을 바꾸므로 버퍼 재사용 실험과 분리한다.

### 3. 이미 적용된 사항

- `m_AutoSyncTransforms`는 이미 꺼져 있다.
- 적 경로 갱신에는 간격과 타깃 이동 임계값이 있다.
- ObjectPool과 중복 반환 방어가 있다.
- 적 스프라이트 아틀라스 생성용 prebuild 스크립트가 있다.

이 항목들을 새 최적화처럼 반복 도입하지 않는다. 물리 병목과 별개로 렌더링 시간이 높게 측정될 때 실제 배칭·오버드로를 확인한다.

## 계측 설계

추가 후보 마커:

- `Conquest.Upgrade.DetectEnemies`
- `Conquest.Upgrade.DetectEnemiesFromPosition`
- `Conquest.Enemy.PathUpdate`
- `Conquest.Pool.Spawn`

ProfilerMarker.Auto 범위는 동기 작업 안에서 끝낸다. 코루틴 전체에 범위를 걸어 yield를 넘기지 않는다. [Unity 공식 마커 지침](https://docs.unity.cn/6000.0/Documentation/Manual/profiler-add-markers-code.html)

저장할 지표:

| 항목 | 지표 |
|---|---|
| 전체 프레임 | CPU 프레임 시간 중앙값·p95·최댓값, 목표 프레임 초과 비율 |
| 접촉 탐색 | FindNewContacts 시간, Broadphase Pairs, Added Contacts, Total Contacts |
| 탐색 코드 | 호출당 시간, 프레임당 호출 수와 누적 시간, GC Alloc |
| 물리 위치 갱신 | Transform Sync Bodies/Colliders, 부모 변경 관련 Sync 수 |
| 부하 조건 | 적·투사체·경험치 활성 수, 해당 프레임 스폰 수 |

추가 글의 Stopwatch 예제에서 avgMs는 측정 호출 1회의 평균이다. 한 프레임에 여러 번 호출되면 프레임 누적 비용도 봐야 한다. 평균만으로는 순간 멈춤을 판단할 수 없으므로 p95와 최댓값을 함께 보관한다. 다른 label을 한 sampler의 합계에 섞지 않는다. 매 프레임 Debug.Log를 추가하지 않고, 일정 프레임 구간별 집계만 남긴다.

기준값은 목표 플랫폼의 Development Build에서 우선 수집한다. 처음에는 Deep Profiling을 끄고, 필요한 범위에만 마커를 추가한다. 같은 장면을 여러 번 실행해 측정 변동보다 큰 개선인지 확인한다. [Unity 프로파일링 가이드](https://unity.com/how-to/best-practices-for-profiling-game-performance)

## 첫 패치의 범위와 합격 조건

실행 후보는 DetectUpgrade의 결과 배열·List 중복 할당 제거와 존재 확인 경로 분리다. 동시에 충돌 레이어 실험용 기준 캡처를 준비한다.

- 유지: 감지 반경, 탐색 주기, 공격 대상 규칙, 공격 횟수와 연쇄 규칙.
- 변경: 결과 저장 버퍼의 소유권·재사용, 존재 확인에 필요한 조회량.
- 합격: 반복 탐색의 결과 저장용 할당이 충분한 용량 확보 후 제거되고, 게임플레이 결과가 동일하며, 전체 CPU p95가 악화되지 않을 것.
- 별도 실험: 레이어 변경, 경로 갱신 분산, 물리 이동 방식 변경, 스폰 분산.

새 기준 캡처가 없으므로 개선율을 예측하거나 달성했다고 기록하지 않는다.
