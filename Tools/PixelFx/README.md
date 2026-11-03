# PixelFX → Unity

Node 18 이상이 필요합니다. npm 설치 없이 실행합니다. 라이브러리는 `upstream.json`의 커밋으로 고정되어 있습니다.

## 이펙트 검색

```sh
node Tools/PixelFx/export.mjs --list frost
node Tools/PixelFx/export.mjs --list slash
```

## PNG + JSON 내보내기

```sh
node Tools/PixelFx/export.mjs impacts/hit-spark
node Tools/PixelFx/export.mjs ice/frost-nova --opts '{"light":0.5}'
```

결과는 `Assets/PixelFx/Sheets/<category>/<name>/`에 생성됩니다. 검토용 연락 시트는 `Tools/PixelFx/records/`에 있으며 Git에서 제외합니다.

## Unity로 가져오기

Unity를 편집 모드로 열고 Pipeline 서버가 실행된 상태에서:

```sh
node Tools/PixelFx/export.mjs impacts/hit-spark --import --ppu 100
```

PNG, Aseprite 형식 JSON에서 스프라이트 분할, 피벗, 프레임별 재생 시간, 반복 여부를 가져옵니다. Point 필터·압축 없음·밉맵 없음으로 설정하고 `.anim`, `.controller`, `.prefab`을 생성합니다. 같은 이펙트를 다시 가져오면 기존 에셋과 스프라이트 ID를 유지합니다. 기존 프리팹의 사용자 컴포넌트도 유지합니다.

PPU 기본값은 100입니다. 라이브러리 픽셀 크기와 기존 캐릭터 픽셀 크기가 다를 수 있으므로 씬의 목표 크기에 맞춰 `--ppu`를 지정하세요. 프로젝트 카메라나 기존 스프라이트 설정은 변경하지 않습니다.

`--import`는 프로젝트의 로컬 Pipeline HTTP API를 사용합니다. 인증 토큰은 연결 파일에서 메모리로만 읽으며 출력하거나 저장하지 않습니다. 새 npm 패키지나 Unity 패키지는 필요하지 않습니다.

생성된 프리팹을 씬에 놓거나 Instantiate하면 Animator가 재생합니다. `PixelFxEffect`는 비반복 이펙트가 끝나면 비활성화합니다. 풀에서 사용할 경우 별도로 게임의 ID·풀 등록을 설정해야 합니다. 이번 연결은 기존 공격/데미지 코드를 변경하지 않습니다.

## 새 이펙트 제작

Codex 프로젝트 스킬: `.agents/skills/pixelfx/SKILL.md`.
실제 라이브러리 작업 디렉터리는 `Tools/PixelFx/vendor`입니다. 중복 clone 대신 이 디렉터리에서 도구를 실행하세요. `docs/AUTHORING.md`와 `docs/CATALOG.md`를 먼저 읽고 시안을 렌더링해서 확인합니다. 기본 라이브러리와 사용자 변경은 Git으로 관리합니다. 업데이트할 때는 버전 고정 파일과 라이선스를 함께 검토하세요.

## 라이선스

`Assets/StreamingAssets/PixelFxNotices.txt`에 배포용 고지를 포함합니다. 원문은 `vendor/LICENSE`, `vendor/NOTICE.md`에 보관합니다. `gallery/fonts`와 Patch Pixel 폰트는 가져오지 않았습니다. 작업용 연락 시트의 작은 글꼴은 Node 렌더 도구가 자체적으로 그리는 글자입니다.
