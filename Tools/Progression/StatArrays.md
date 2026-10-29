# 캐릭터 최종 스탯 배열 (최대 5강)

구글 시트 `요원정보` C:I에 Hp, HpRegenRate, Speed, Damage, Defense, Critical Damage, Critical Chance를 직접 편집합니다.
각 셀은 **기본(0강),1강,2강,3강,4강,5강** 순서로 총 6개의 최종 수치입니다.
예: HP `200,210,220,230,240,250`. 증가량/퍼센트가 아닙니다.
캐릭터 강화 탭은 요원정보 최종 수치를 참조합니다. 옛 Enhancement 열은 제거했습니다.

GetPlayerData가 PlayerData.csv를 내려받습니다. DataManager는 첫 원소를 기본 스탯으로 읽고,
CharacterEnhancementTable은 인접 단계 차이를 계산해 기존 스탯 보너스 시스템에 연결합니다.
따라서 5강 HP는 200+50=250으로 적용되며 210을 또 더하는 중복 계산이 없습니다.
Speed와 Critical Damage는 소수 허용, 나머지는 정수입니다. 치명타 피해는 게임 배율 수치 그대로입니다.

CharacterProgressionSettings 기본값과 리소스 maxLevel=5. 기존 5 초과 저장 레벨은 EnsureCharacters의 기존 클램프로 제한됩니다.
검증: Unity 컴파일, 캐릭터 2개/각 5단계 파싱, 검사 1강HP210/5강HP250/5강공격력15, GetData로 받은 CSV와 온라인 배열 일치.
변경 전 코드·CSV·설정·시트 백업: BeforeFinalValues/.

## 기본 공격 추가 항목

요원정보 J:L에 Pierce / AttackRange / AttackSpeed가 추가되었습니다. 캐릭터 강화 탭은 동일 값을 참조합니다.
- Pierce: 기본~5강의 추가 관통 수. 예 `0,0,1,1,2,2`. 마법 탄환의 기존 관통 처리에 연결됩니다.
- AttackRange: 기본~5강의 최종 범위 배율. 예 `1,1.1,1.2,1.3,1.4,1.5`. 검사 검기 크기 / 마법 탄환 사거리.
- AttackSpeed: 기본~5강의 최종 속도 배율. 예 `1,1.05,1.1,1.15,1.2,1.25`. 기본 공격 쿨타임을 이 값으로 나누고 모션 속도에 곱합니다.
모든 배열 길이는 6입니다. 현 입력값은 기존 성능 유지를 위해 Pierce=0 / 범위·속도=1로 채웠습니다.
추가 항목의 0강 값도 적용됩니다. 기존 7개 스탯은 인접 단계 차이로 처리합니다.

검증: Unity 컴파일 및 GetData 다운로드, VerifyCombatArrays.cs로 가상 5강 관통2 / 범위1.5 / 공격속도1.25 / 1초 쿨타임→0.8초 계산, 기존 누적HP+50 유지 확인. 검증의 임시 정적 상태는 finally로 원복합니다.
이 변경 전 시트/코드/CSV 백업: BeforeCombatArrays/.
