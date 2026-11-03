# 증강 시스템 스크립트 안내

이 문서는 증강 관련 스크립트가 담당하는 큰 역할을 설명합니다. 경로는 `Assets` 폴더 기준입니다.

## 전체 관리

| 스크립트 | 역할 |
| --- | --- |
| `Script/Manager/UpgradeManager.cs` | 증강 획득·레벨·발동·쿨타임과 플레이어 능력치 보너스를 관리합니다. 기본 공격의 연쇄 효과와 획득 아이콘 표시 코드도 포함합니다. |
| `Script/Upgrade/DetectUpgrade.cs` | 증강이 공격하거나 추적할 적을 감지하는 공통 기능입니다. |
| `Script/Upgrade/HomingProjectile.cs` | 적을 추적하는 증강 투사체의 공통 동작과 피해 처리를 담당합니다. |
| `Script/Upgrade/DotDamage.cs` | 적에게 일정 간격으로 들어가는 지속 피해의 공통 동작을 담당합니다. |
| `Script/Upgrade/Flooring.cs` | 바닥 장판의 지속 시간, 감속 적용과 해제를 관리합니다. |

## 개별 증강

| 스크립트 | 역할 |
| --- | --- |
| `Script/Upgrade/Lightning.cs` | 낙뢰 증강의 대상과 피해를 처리합니다. |
| `Script/Upgrade/FireBall.cs` | 추적형 파이어볼을 처리합니다. |
| `Script/Upgrade/FireStorm.cs` | 파이어스톰 투사체와 적중 효과를 처리합니다. |
| `Script/Upgrade/HeatWave.cs` | 열파 투사체와 적중 효과를 처리합니다. |
| `Script/Upgrade/Burn.cs` | 화상 상태와 지속 피해를 처리합니다. |
| `Script/Upgrade/Poison.cs` | 독 지속 피해를 처리합니다. |
| `Script/Upgrade/Mold.cs` | 곰팡이 효과와 적에게 적용되는 동작을 처리합니다. |
| `Script/Upgrade/RottingSpore.cs` | 부패 포자 관련 효과를 처리합니다. |
| `Script/Upgrade/SporeWarhead.cs` | 포자 탄두 효과를 처리합니다. |
| `Script/Upgrade/Boom.cs` | 폭발 범위와 피해를 처리합니다. |
| `Script/Upgrade/Chain.cs` | 연쇄 효과의 감지와 적중을 처리합니다. |
| `Script/Upgrade/Snail.cs` | 공통 장판 기능을 이용해 달팽이 감속 장판을 제공합니다. |
| `Script/Upgrade/LavaField.cs` | 용암 지대에 닿은 적에게 화상을 적용합니다. |
| `Script/Upgrade/Shuriken.cs` | 수리검이 적에게 닿았을 때의 피해를 처리합니다. |
| `Script/Upgrade/Orbit.cs` | 플레이어 주변을 도는 증강 오브젝트를 관리합니다. |
| `Script/Upgrade/Misslie.cs` | 미사일 증강의 대상 감지와 발사 동작을 처리합니다. |
| `Script/Upgrade/MisslieProjectile.cs` | 발사된 미사일 투사체의 추적과 효과를 처리합니다. |

## 선택과 합성

| 스크립트 | 역할 |
| --- | --- |
| `Script/Upgrade/SelectUpgrade.cs` | 획득할 증강의 선택과 UI 표시를 관리합니다. |
| `Script/Upgrade/SelectIcon.cs` | 선택 화면의 아이콘 동작을 처리합니다. |
| `Script/Upgrade/UpgreadeSelectConfirm.cs` | 증강 선택 확인 동작을 처리합니다. |
| `Script/Upgrade/UpgradeImageSet.cs` | 선택 또는 합성 슬롯의 증강 이미지와 선택 상태를 관리합니다. |
| `Script/Upgrade/UpgradeCombine.cs` | 증강 합성 화면의 조합 동작을 처리합니다. |
| `Script/Upgrade/UpgradeCombineResult.cs` | 합성 결과 정보를 표시합니다. |
| `Script/Upgrade/UpgradeFusionRules.cs` | 합성 가능 여부와 결과 증강을 판정합니다. |

## 화면 표시

| 스크립트 | 역할 |
| --- | --- |
| `Script/UI/UpgradeAppeared.cs` | 증강 선택 화면이 나타날 때의 표시 흐름을 처리합니다. |
| `Script/UI/UpgradeCombineAvailability.cs` | 합성 가능한 증강이 있는지 확인하고 UI에 반영합니다. |
| `Script/UI/UpgradeCooldownHud.cs` | 획득한 증강의 아이콘, 레벨, 쿨타임을 화면에 표시합니다. |
| `Script/UI/UpgradeFusionBurst.cs` | 합성 화면의 시각 효과를 그립니다. |
| `Script/UI/UpgradeFusionController.cs` | 합성 슬롯 선택, 합성 진행과 결과 표시를 관리합니다. |
| `Script/UI/UpgradeHover.cs` | 증강에 마우스를 올렸을 때의 정보와 시각 표시를 관리합니다. |
| `Script/UI/UpgradeHudRendering.cs` | 증강 HUD의 아이콘·텍스트 스타일과 캔버스 구성을 담당합니다. |

## 적중과 피해 계산

- `EnemyCombat`이 Skill/Upgrade 태그의 충돌을 받아 공격 정보를 조회합니다. 투사체의 적 충돌 콜백은 몬스터가 호출하며, 투사체 스스로 피해를 적용하지 않습니다.
- 피해는 `(기본 피해 + 플레이어 공격력 × 0.5) × 치명타 배율 × (1 - 적 방어력 / 100)` 순서로 계산합니다. 치명타 배율은 치명타 판정에 성공했을 때만 곱합니다.
- 무적 중에는 체력을 깎지 않지만 투사체의 관통 횟수는 소비합니다. 장판과 회전 수리검은 유지됩니다.
- 지속 피해와 범위 폭발은 각 효과의 타이머에서 `EnemyCombat.TakeDamage`로 피해를 요청하며, 동일한 능력치·치명타·방어력 계산을 사용합니다.
