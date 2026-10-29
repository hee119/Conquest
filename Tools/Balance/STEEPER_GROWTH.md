> 2026-10-27 사용자 요청으로 밸런스 패치 전체 취소. 아래 내용은 적용되지 않는 과거 기록입니다.

> 복원 완료: 사용자의 요청으로 이 변경은 취소되었습니다. 현재 데이터는 full_balance_results.json 기준입니다. 아래는 취소된 변경의 기록입니다.

# 경험치 곡선 재조정 (2026-10-27)

이 문서와 steeper_growth_results.json이 기존 FULL_BALANCE.md의 레벨/후반 슬라임 스케줄을 대체한다.

- 요구량: 50 + 5n + 0.75n², n=0..60, 5 단위 반올림. 기존 후반 포화 곡선을 가속 곡선으로 교체.
- 30번째 요구량 825, 50번째 2095, 마지막 3050. 누적 30레벨 10080 / 만렙 67535.
- 첫 보스 뒤 슬라임 구간: Stage1 행9=25초/4마리, 행11=20초/3마리, 행13·15=25초/4마리.
- 첫 보스 이전 예산13010 유지, 마지막 보스 이전40240→80740, 전체47340→87840.
- 예약 소환1330→1420 (분열 자식 제외); 최대 일반 웨이브 HP/초108→103.68. 웨이브 대기1066→1531초.
- 80% 경험치 수집 시 첫 보스30레벨; 85% 수집 시 마지막 보스 전61레벨. 전투 생존/클리어 검증은 아님.

## 코드 근거와 수정
SplitEnemy.Die는 EnemySplit 후 EnemyCombat.Die를 호출하며, 부모와 자식의 죽음마다 오브 하나를 드롭한다.
Slime.prefab splitCount=3, splitSpawnCount=2: 1+2+4+8=15회 드롭, BlueOrb30 기준450XP.
SlimeKing 마지막 세대는 일반 슬라임8마리로 이어져 총 일반 슬라임120회와 보스7회 드롭. 보스 소환 패턴의 추가 수입은 모델에서 제외.
풀 재사용 때 일반 슬라임의 splitCount가 복원되지 않던 문제를 수정했다. 최초 설정값을 캐시해 OnEnable 때 복원하고 분열 자식은 Spawn 직후 부모가 횟수를 덮어쓴다.

## 재현과 복구
`python3 Tools/Balance/steeper_growth.py`
입력/변경 전 백업: steeper_growth_parameters.json의 costs_before와 stage_before.
결과/수집률 시나리오/곡률 후보: steeper_growth_results.json.
온라인 Level과 OnlyLevel 61개 셀 일치, Stage1 보상 수식 결과 재확인 완료. 서식/수식은 유지.

GetLevelData/GetStageData의 별도 저장 경로를 확인하고 다운로드 완료. 로컬 CSV를 결과와 대조해 일치 확인. Unity 컴파일 완료 및 initialSplitCount 필드 반영 확인.
