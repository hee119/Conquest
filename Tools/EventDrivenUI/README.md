# 이벤트 기반 UI 사운드와 SPUM 입력 변경

## 사운드 흐름

`버튼/토글 → UiSoundEmitter → UiSoundEvents → SoundManager → AudioSource`

- `SoundManager.Update()`의 UI 검색, 0.15초 스캔 타이머, 전역 메뉴/패널 탐색을 제거했습니다. 기존 페이드·볼륨·일시정지 처리는 유지합니다.
- `UiSoundEmitter`가 활성화될 때 생성/등록 이벤트를 발행하고 버튼 클릭과 토글 변경을 전달합니다. 비활성화될 때 리스너와 등록을 해제합니다.
- `SoundManager`는 `OnEnable`에서 이벤트를 구독하고, 기존 활성 UI 등록 목록도 받아 생성 순서를 처리합니다. `OnDisable`/`OnDestroy`에서 해제합니다.
- 씬 로드 시 해당 씬의 UI 계층을 한 번 등록합니다. 주기적인 전역 탐색은 없습니다.
- 기존 프리팹 7개에 이벤트 발행 컴포넌트 8개를 등록했고, 동적 버튼 생성 코드에도 등록을 연결했습니다. 템플릿/프리팹 복제는 발행 컴포넌트를 그대로 상속합니다.
- 코드로 완전히 새로운 버튼/토글을 만들 때는 `UiSoundEvents.Register(control)`을 한 번 호출합니다. 새 UI 계층 전체에는 `UiSoundEvents.RegisterHierarchy(root)`를 사용합니다.
- 강화/확정/드롭다운/호버 UI도 이벤트를 발행합니다. 전용 사운드 버튼에는 기본 클릭음을 중복 재생하지 않습니다.

## 캐릭터 입력과 이동

`InputAction.performed → PlayerManager.HandlePointerPress → 선택 / PlayerObj.SetMovePos → 이동 코루틴`

- 변경 대상은 `Assets/GameAssets/SPUM/Sample/Script`에 있는 샘플 캐릭터 관리 코드입니다. 추가 요청에 따라 게임 본편 `PlayerMove`의 마우스 우클릭 대시도 이벤트 콜백으로 변경했습니다.
- `PlayerManager`는 포인터 누름 액션을 활성화/비활성화와 함께 구독·해제하고, 파괴 시 액션을 해제합니다.
- 입력 콜백에서 현재 화면 좌표를 `EventSystem.RaycastAll`로 검사합니다. GraphicRaycaster의 UI 결과가 있으면 캐릭터 선택/이동을 진행하지 않습니다. 입력 콜백의 `IsPointerOverGameObject`는 이전 프레임 UI 상태를 읽을 수 있으므로 사용하지 않았습니다. [Unity 공식 입력/UI 처리 설명](https://github.com/Unity-Technologies/InputSystem/blob/develop/Packages/com.unity.inputsystem/Documentation~/handling-input-target-ambiguity.md)
- 캐릭터 선택, 명령 패널, 애니메이션 미리보기, 생성/정렬, 스크린샷 공개 메서드를 유지했습니다.
- `PlayerObj`는 이동 명령을 전달받으면 코루틴을 시작하고 도착하면 종료합니다. 입력 액션은 Manager가 한 번 처리하며 각 캐릭터가 중복 구독하지 않습니다.
- X/Y 평면에서 MoveTowards로 이동해 목표를 지나치지 않습니다. 좌우 반전과 `localPosition.y * 0.01` 기반 월드 Z 정렬을 유지했습니다.
- 이동/대기 애니메이션은 상태 전환 시 한 번 실행합니다. 이동 중 목표만 바꾸면 이동 애니메이션을 다시 시작하지 않습니다. 수동 미리보기는 명시적으로 같은 애니메이션도 다시 재생할 수 있습니다.
- 선택 표시는 선택 시와 캐릭터의 `PositionChanged` 이벤트에서만 갱신합니다. 캐릭터 비활성화와 Manager 비활성화 시 구독을 해제합니다.
- 외부 코드가 캐릭터 Transform을 직접 변경할 경우 `RefreshPosition()`을 호출하면 Z 정렬과 선택 표시가 갱신됩니다. 기존 생성·정렬 명령에는 이 호출이 연결되어 있습니다.
- `isAction`은 호환 프로퍼티로 유지하며 기존 직렬화 값은 `FormerlySerializedAs`로 이관합니다.
- `PlayerManager`와 `PlayerObj`에는 `Update`, `FixedUpdate`, `LateUpdate`가 없습니다. 이동 중 코루틴만 프레임마다 필요한 위치 계산을 수행합니다.

## 검증

Unity에서 스크립트 컴파일을 확인하고 별도 씬에서 실제 입력 액션과 UI 콜백을 실행했습니다.
`RuntimeVerification.json`에 생성 순서, 동적/복제 UI, 중복 리스너, 토글 양방향, 구독 해제·재구독, 전용 확정음, 입력 선택·이동, UI 클릭 차단, 목표 변경, 좌우 반전, Z 정렬, 이동 종료, 유휴 위치 갱신 중단, 애니메이션 미리보기 결과를 기록했습니다. 모든 항목이 통과했고 실행 예외는 없었습니다.
원래 씬과 실행 시작 씬 설정을 복원했고 임시 검증용 런타임 컴포넌트와 씬은 제거했습니다. 검증 코드 사본은 이 폴더에 있습니다.

## 추가 요청: PlayerMove 마우스 대시

`PlayerMove`는 `<Mouse>/rightButton`에 연결된 `InputAction`의 `performed` 콜백에서 대시를 시작합니다. `OnEnable`에서 구독·활성화하고 `OnDisable`에서 해제·비활성화, `OnDestroy`에서 액션을 폐기합니다. 우클릭을 누르고 있는 동안 반복 발사되지 않고 새로운 누름마다 한 번 처리합니다.

기존 `Move(InputAction.CallbackContext)` 키보드 이동과 실제 Rigidbody 이동, 대시 회복, 타일맵 경계, 발소리는 유지합니다. `Update`, `FixedUpdate`, `LateUpdate`는 없습니다. 대시 입력이 대시 코루틴을 시작하고 종료 시 멈춥니다. 회복 코루틴은 에너지가 100이 되면 종료하며, 재대시 시 회복 대기를 다시 시작합니다. 경계 검사와 발소리는 실제 이동·대시 중의 물리 코루틴에서만 처리하며 정지 시 종료합니다. 컴포넌트 비활성화 시 세 코루틴 모두 중단합니다. 일시정지 상태와 비활성 PlayerInput의 대시 이벤트는 무시합니다. 가상 마우스로 우클릭·길게 누르기·재누르기·비활성화/재활성화·일시정지·대시 에너지 회복을 실행 검증했으며 결과는 `PlayerMoveInputVerification.json`에 기록했습니다.
