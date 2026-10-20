# 캐릭터 전용 스킬 강화 데이터

캐릭터 전용 기본 공격 강화는 `StatUpgrade`와 분리된 `CharacterSkillUpgradeData` 시트에서 관리한다.

| 열 | 용도 |
| --- | --- |
| `Id` | 강화 고유 ID |
| `NameId` | 이름 로컬라이제이션 키 |
| `Type` | `CharacterSkill` 고정 |
| `Function` | 현재는 `DataDriven` 사용 |
| `CharacterId` | `0` 공통, `1` 검사, `2` 마법사 |
| `RangePercent` | 레벨별 공격 범위 증가율 |
| `AttackCount` | 레벨별 추가 공격 또는 발사체 수 |
| `Pierce` | 레벨별 추가 관통 수 |
| `ChainCount` | 레벨별 연쇄 대상 수 |
| `ChainRange` | 레벨별 연쇄 탐색 거리 |
| `DamagePercent` | 레벨별 피해 증가율 |
| `IconId` | 기존 `SkillUpgradeIcon` ID |
| `Tier` | 최초 선택 목록은 `0` |
| `DesId` | 설명 로컬라이제이션 키 |

레벨별 값은 쉼표로 구분하며 누적 최종값으로 작성한다. 범위와 연쇄 공격 행은 `CharacterId=0`, 마법사 관통 행은 `CharacterId=2`로 둔다.

설명 인자 순서는 범위, 추가 공격 수, 관통 수, 연쇄 대상 수, 연쇄 범위, 피해 증가율이다.

런타임 선택, 캐릭터 필터, 수치 합산은 `UpgradeManager`가 담당한다.
