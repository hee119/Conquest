# 미사용 에셋 추가 정리

사용자 요청에 따라 씬, 프리팹, 코드 경로, 런타임 Resources 로딩, 열린 씬의 메모리상 참조를 검사했다. 일반 에셋은 기존 Resources 로딩 대상과 수정 중인 파일을 보존했다. SPUM은 외부 코드의 타입 사용과 외부 에셋의 GUID 참조를 별도로 조사했다.

- 삭제: 378개(24,740,379바이트, 약 23.59 MiB). 각 에셋의 .meta도 함께 삭제.
- SPUM: 342개 삭제. `Assets/GameAssets/SPUM/Sample/Materials/EffectMat.mat`은 Missile 및 SporeWarhead 프리팹이 사용하므로 보존.
- 기타 36개: 미사용 샘플 씬과 전용 리소스, Beam 재질, 미참조 일본어 폰트 에셋.
- 나무 타일 및 관련 리소스 보존. SPUM WoodShield 이미지는 나무 타일이 아니며 미사용이어서 삭제.
- 삭제 실패 0, 보존 의존성 누락 0, 나무 리소스 누락 0.
- 남기는 파일에서 삭제 GUID로 향하는 참조 0. Unity가 임포트하지 않는 Packages의 Samples~ 원본 템플릿은 GUID가 중복되는 배포 원본이므로 별도 처리했다.

복구 ZIP: `/private/tmp/Conquest-unused-resources-2026-11-01-pass2.zip`.
파일별 원본 SHA-256은 `backup-manifest.json`, 삭제 목록은 `deleted.txt`, 의존성 검증은 `verification.json`에 기록했다.

재임포트 후 컴파일 성공. SPUM 빈 폴더 107개도 제거했다. Missile/SporeWarhead 프리팹 모두 누락 스크립트 0개이며 보존 재질 참조가 유지됨을 확인했다. 에디터 Graphs.Edge.WakeUp 예외는 삭제 전부터 기록되어 있던 Unity 에디터 내부 오류로, 이번 삭제와 구분했다.

사용자 요청에 따라 복구 ZIP 및 백업 메타데이터를 삭제했다. 백업은 보관하지 않는다.
