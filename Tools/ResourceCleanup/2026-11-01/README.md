# 미사용 게임 리소스 정리 — 2026-11-01

## 결과
- 삭제: 에셋 510개와 각 .meta 파일, 합계 149,100,018바이트 (142.19 MiB).
- 삭제 목록: `candidates.tsv`, 실제 삭제 목록: `deleted.txt`.
- 복구용 ZIP: `/private/tmp/Conquest-unused-resources-2026-11-01.zip` (에셋과 .meta GUID 원본 포함).
- 백업 파일별 SHA-256: `backup-manifest.json`.

## 삭제 판단
실제 게임의 CutSceen, Lobby, Stage1 씬을 포함하여 보존하는 모든 에셋에서 Unity AssetDatabase 재귀 의존성을 계산했다. 현재 열린 씬의 메모리상 참조, 코드에 명시된 에셋 경로, Resources 전체, 수정 중이거나 아직 추적되지 않은 에셋도 보존했다. 플러그인/폰트/에디터 지원 파일은 삭제 후보에서 제외했다. 데모 씬과 연결된 미사용 이펙트·텍스처·옛 문어 애니메이션 등이 삭제 대상이다.

나무/wood/tree 이름의 리소스 및 Resources/Tiles 전체와 그 의존성을 보존했다. 나무 패키지와 나무 프리팹의 공유 텍스처도 의존성으로 보존했다. 코드 파일은 삭제하거나 수정하지 않았다.

삭제 전 보존 파일과 ProjectSettings의 GUID를 추가 검색했으며 삭제 후보를 참조하는 항목은 0개였다. 삭제 직전 Unity 의존성을 다시 계산해 보호 대상이 목록에 없는지 검사했다. 삭제 후 보존 의존성의 파일 누락 0개, 나무 보호 목록의 파일 누락 0개, 삭제 실패 0개를 확인했다.

## 코드에서 확인한 실행 중 생성/로딩
- `PoolManager`: Enemy/SkillEffect/Exp/Upgrade/Bullet/Boss/BossPattern/UI/Item 프리팹을 Resources.LoadAll로 읽고 풀에서 생성.
- `DataManager`: 캐릭터 프리팹, CSV 데이터, 경험치·투사체·스킬·보스 패턴과 업그레이드 아이콘을 Resources로 읽음.
- `GameManager`: Resources/Tiles를 불러와 맵 생성에 사용.
- `StageManager`: 런타임 카메라 경계, 몬스터 및 보스 스폰 지점을 생성하고 타일맵 구성.
- `InterfaceScaleSettingsPanel`, `SliderPlusMinus`: 설정 스크롤 목록, 크기 슬라이더 복제본, +/- 버튼과 글자를 실행 중 생성.
- `ExpBar`, `HpBar`: 경험치바 그래픽/마스크/텍스처/스프라이트와 HP 지연 표시용 복제본 생성.
- `UpgradeCooldownHud`, `UpgradeManager`: 업그레이드 아이콘/쿨다운/레벨 표시 UI 생성.
- `UpgradeHudRendering`: Resources/UI/UpgradeHud 폰트 및 재질을 문자열 경로로 읽음.
- `GameEndScreenController`, `StagePlanetMenu`: 셰이더/재질을 읽어 런타임 재질 인스턴스 생성.
- `SoundManager`: 재생용 AudioSource 게임 오브젝트를 실행 중 생성.
- SPUM: 문자열 애니메이션 로딩과 캐릭터 생성 코드가 있어 패키지 전체를 보존.

이 경로들은 씬의 직접 참조만으로 미사용 판단할 수 없어 모두 보존했다.

## 위치별 삭제 개수
- Assets/Animation: 6개
- Assets/GameAssets/Cainos: 257개
- Assets/GameAssets/Matthew Guz: 77개
- Assets/GameAssets/Vefects: 86개
- Assets/Materials: 4개
- Assets/Sprite: 73개
- Assets/SpriteSeet: 6개
- Assets/Temp: 1개

## 실행 검증
Unity 재임포트 및 도메인 리로드 완료 후 컴파일 오류 없음(compilationFailed=false)을 확인했다. Stage1 플레이 모드에서 PoolManager, DataManager, StageManager 초기화와 EnemyCombat 80개 활성화를 확인했다. 해당 확인값은 `runtime-smoke.txt`에 저장했다. 검증 종료 후 플레이 모드를 종료했다.

실행 중 기존 `ExpBar.ApplyEditorPreview`가 플레이 모드에서 `EditorSceneManager.MarkSceneDirty`를 호출하는 InvalidOperationException도 관찰했다(Assets/Script/UI/ExpBar.cs:736). 이 코드는 정리 이전에도 동일 오류가 관찰된 부분이며 이번 리소스 정리에서는 수정하지 않았다. 새 삭제 GUID의 참조 누락이나 관련 리소스 로딩 오류는 관찰되지 않았다. Pipeline 통신 시간 초과 로그는 재임포트 중의 도구 연결 문제로 구분했다.

사용자 요청에 따라 복구 ZIP 및 백업 메타데이터를 삭제했다. 백업은 보관하지 않는다.
