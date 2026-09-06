# Swarm Survivor - Feature Guide

현재 구현된 기능을 코드 흐름, 선택 이유, 한계까지 함께 정리한 문서입니다. 성능 수치의 원본 조건은 [PERF_LOG.md](PERF_LOG.md), 설계 판단의 전체 근거는 [DECISIONS.md](DECISIONS.md)를 기준으로 합니다.

## 목차

- [1. Gameplay Loop](#1-gameplay-loop)
- [2. Player & Camera](#2-player--camera)
- [3. Weapon System](#3-weapon-system)
- [4. Upgrade System](#4-upgrade-system)
- [5. Upgrade Summary UI](#5-upgrade-summary-ui)
- [6. Enemy System](#6-enemy-system)
- [7. Spawn & Final Rush](#7-spawn--final-rush)
- [8. EXP Collection](#8-exp-collection)
- [9. Object Pooling](#9-object-pooling)
- [10. UI / Settings / Audio](#10-ui--settings--audio)
- [11. Benchmark Tools](#11-benchmark-tools)
- [12. Balance Iteration](#12-balance-iteration)
- [13. Related Documentation](#13-related-documentation)

## 1. Gameplay Loop

**기능과 목적**: 300초 생존을 목표로 자동 전투, 성장 선택, 난이도 상승을 하나의 짧은 플레이 루프로 묶습니다. HP가 0이면 실패하고 `GameManager`가 300초 도달 시 기존 승리 경로를 실행합니다.

**동작 방식**: 게임 경과 시간은 `Time.deltaTime`으로 누적합니다. Enemy 처치 → EXP Gem 회수 → `LevelSystem` 레벨업 → Upgrade 카드 3장 선택 순서로 진행되며, Upgrade 선택 중에는 `Time.timeScale = 0`으로 게임 진행이 멈춥니다.

**장점과 한계**: 게임 시간과 성능 측정 시간을 분리해 Pause가 플레이 시간에 포함되지 않습니다. 반면 5분이라는 짧은 루프에서 모든 성장 항목을 볼 수 있는지는 밸런스에 크게 의존합니다.

**대안**: `Time.unscaledDeltaTime`으로 게임 시간을 재면 UI 중에도 시간이 흐르지만, 현재는 카드 선택이 전술적 판단 시간이므로 채택하지 않았습니다.

## 2. Player & Camera

**기능과 목적**: Player는 2D 물리 이동과 Idle/Run 시각 피드백을 제공하고, Camera는 Player를 안정적으로 추적합니다.

**동작 방식**: `PlayerController`는 `Update`에서 입력을 읽고 Animator `IsMoving`을 갱신한 뒤, `FixedUpdate`에서 `Rigidbody2D.linearVelocity`를 설정합니다. 최종 Inspector 기준 Max HP는 200, EXP Pickup Radius는 2.5이며 Rigidbody2D는 `Interpolate`, `Never Sleep`을 사용합니다. `CameraFollow`는 `LateUpdate`에서 `Vector3.SmoothDamp`로 목표 X/Y를 추적하고 Awake에 캐싱한 Z를 유지합니다.

**장점과 한계**: 물리 갱신 뒤 카메라가 따라가므로 시각적 떨림을 줄이고, Z 캐싱으로 2D 카메라가 Player Z=0을 따라가는 오류를 막습니다. SmoothDamp는 정확한 고정 거리 추적이 아니라 부드러운 지연을 의도한 방식입니다.

**대안**: Update 이동·MovePosition·`Lerp(a, b, t * deltaTime)`은 물리 또는 프레임 의존 문제가 있어 쓰지 않았습니다. Cinemachine은 이 규모에서 직접 구현보다 설정·패키지 의존성이 늘어 도입하지 않았습니다.

## 3. Weapon System

### Basic Shotgun

**기능과 목적**: 기본 무기는 최근접 Enemy를 자동 조준해 초반 전투를 유지합니다.

**동작 방식**: `BulletWeapon`은 fire accumulator가 간격에 도달할 때 최근접 Enemy를 Burst당 한 번 찾습니다. 중앙 방향을 기준으로 기본 3발을 Spread 20도에 균등 배치하고, `PoolManager.Spawn` 뒤 `Projectile.Launch(direction, damage)`를 호출합니다. 기본 간격은 0.8초, 기본 피해는 10이며 Fire SFX는 실제 발사된 Burst당 한 번입니다.

**장점과 한계**: Pellet마다 탐색하지 않아 타겟 검색 비용을 늘리지 않고, 가까운 적 주변 대응력을 높입니다. 다만 단일 최근접 목표만 기준으로 삼으므로 군집 전체를 최적으로 겨냥하지는 않습니다.

**대안**: 매 Pellet마다 Enemy를 다시 탐색하거나 광역 Physics Query를 쓰는 방식은 현재 필요 이상으로 검색 비용과 복잡도를 키워 채택하지 않았습니다.

### Radial Weapon and Damage

**기능과 목적**: RadialWeapon은 포위 상황을 위한 1회 획득 무기이며, 피해 강화는 두 무기에 독립적인 성장 보상을 제공합니다.

**동작 방식**: 시작 시 잠긴 `RadialWeapon`은 Upgrade로 해금되면 2초마다 8방향을 360도 균등 분할해 Projectile Pool에서 발사합니다. Enemy 검색은 하지 않습니다. 기본/방사형 무기 피해 강화는 각각 최대 3 Stack이며, 발사 시 전달되는 피해값은 10 → 12 → 14 → 16입니다.

**장점과 한계**: 같은 Projectile Prefab을 재사용하면서도 무기별 피해 배율이 섞이지 않습니다. 반면 8발 Burst는 화면 내 Projectile 수를 늘리며, 별도의 무기 공통 프레임워크는 아직 없습니다.

**대안**: WeaponBase·ScriptableObject 기반 데이터 구조는 무기가 크게 늘어날 때 유리하지만, 현재 두 무기에는 직접 참조와 작은 API가 더 설명 가능하고 단순합니다.

## 4. Upgrade System

**기능과 목적**: 레벨업의 즉시 보상을 제공하고, 이동·회수·생존·공격 중 선택하도록 합니다.

**동작 방식**: `UpgradeManager`는 Inspector 순서의 `UpgradeOption` 목록에서 가능한 후보를 모아 무작위 3장을 선택합니다. 일반 항목은 최대 3 Stack, `RadialShot`은 아직 해금되지 않았을 때만 제시되는 최대 1 Stack입니다. 실제 적용 성공 후에만 선택 SFX를 재생합니다.

| UpgradeType | 실제 효과 | 최대 Stack |
| --- | --- | ---: |
| `MoveSpeed` | 기본 이동 속도 기준 Stack당 +15% | 3 |
| `FireInterval` | 기본 무기 간격 기준 Stack당 -12% | 3 |
| `PickupRadius` | 기본 회수 반경 기준 Stack당 +30% | 3 |
| `MaxHealth` | Stack당 최대 HP +20 | 3 |
| `RadialShot` | 2초마다 8방향 발사 무기 해금 | 1 |
| `BasicWeaponDamage` | 기본 무기 피해 Stack당 +20% | 3 |
| `RadialWeaponDamage` | 방사형 무기 피해 Stack당 +20% | 3 |

**장점과 한계**: 최대 Stack과 해금 조건을 후보 단계에서 제외해 중복 카드를 줄입니다. 반면 랜덤 3장 구조는 특정 빌드를 보장하지 않으며, 선택 순서 History는 기록하지 않습니다.

**대안**: 모든 카드를 순서대로 제시하면 성장 재현성은 높아지지만, 현재의 선택형 Survivor 루프와 맞지 않아 사용하지 않았습니다.

## 5. Upgrade Summary UI

**기능과 목적**: `U` 키로 현재 적용된 강화와 Stack을 확인하되 플레이를 멈추지 않습니다.

**동작 방식**: `UpgradeSummaryUI`는 Panel을 열 때만 `UpgradeManager.GetAppliedUpgrades`로 `currentStack > 0` 항목을 받아 문자열을 만듭니다. 다중 Stack은 `이름 xN`, 1회 획득은 `이름 획득`으로 표시합니다. Panel이 열린 뒤 Pause·Upgrade·Result로 `Time.timeScale == 0`이 되면 자동으로 닫힙니다.

**장점과 한계**: 매 프레임 목록을 순회하지 않고 기존 UI와의 겹침을 막습니다. 대신 획득 순서나 다음 후보 확률은 보여주지 않습니다.

**대안**: HUD에 상시 표시하는 방식은 빠른 확인에는 유리하지만 화면 정보를 늘리므로, 필요할 때만 여는 Overlay를 사용했습니다.

## 6. Enemy System

**기능과 목적**: Enemy는 Player를 추적하고 접촉 피해를 주며, 피격·사망 시각 피드백과 EXP Gem 생성을 담당합니다.

**동작 방식**: Enemy는 Rigidbody2D로 목표와 접근 Offset 방향으로 이동합니다. 생존 피해만 Animator `Hit` Trigger를 재생합니다. HP 0에서는 게임 로직상 즉시 사망 확정, Kill/EXP Gem 생성, velocity 0, Collider 비활성화, `Dead` Bool 설정을 실행하고 0.3초 뒤 `PoolManager.Despawn`합니다. 재사용 상태는 `OnEnable`에서 복구합니다.

**장점과 한계**: Exp/Kill 보상은 즉시 주면서 Dead 애니메이션은 볼 수 있고, 사망 중 추가 이동·접촉 피해를 막습니다. Enemy↔Enemy 물리 밀어내기는 없으므로 군집에서 겹쳐 보일 수 있습니다.

**대안**: 일반 Collider의 물리 분리는 갇힘과 solver 비용을 만들었고, Physics Query 기반 separation은 현재 구현·검증 범위를 넘어 사용하지 않았습니다.

## 7. Spawn & Final Rush

**기능과 목적**: 초반에는 성장 기회를 만들고 마지막 1분에 생존 압박을 집중합니다.

**동작 방식**: 자동 Spawn은 Player 주변 180도 섹터에서 발생하며 5초마다 시작 방향이 90도 회전합니다. `GameManager.ElapsedTime` 기준 0~240초는 `Mathf.Lerp(3, 7)`, 240~300초는 `Mathf.Lerp(10, 18)`을 사용합니다. Spawn은 accumulator + `while`로 처리하고 활성 Enemy는 400으로 제한합니다.

**장점과 한계**: 빈 방향을 만들어 회피 공간을 주고, 초반 성장과 Final Rush를 분리합니다. 240초 지점의 7/s → 10/s 전환은 의도적인 난이도 점프이며, 체감 난이도는 Spawn 수치뿐 아니라 무기·회수·플레이 방식의 영향을 함께 받습니다.

**대안**: 360도 전체 Spawn과 과거 4/8/14 단계식은 각각 포위 압박과 중반 급격한 밀집을 키웠습니다. F1은 성능 비교 재현성을 위해 여전히 360도 Burst Spawn을 유지합니다.

## 8. EXP Collection

**기능과 목적**: Enemy가 남긴 EXP Gem을 회수해 레벨업으로 연결합니다.

**동작 방식**: Gem은 Collider/Rigidbody 없이 활성 Gem 목록에 등록되고, `ExpCollector`가 Update에서 Player와의 거리 제곱을 비교합니다. 실제 지급된 Gem의 EXP는 한 프레임에 합산해 이벤트로 전달하며, 지급이 있었다면 EXP SFX를 한 번 재생합니다.

**장점과 한계**: Gem마다 Physics Collider를 만들지 않으며, 한 프레임 다수 획득에서 이벤트·SFX 중복을 줄입니다. 활성 Gem 수가 늘면 거리 비교 순회 비용도 함께 증가합니다.

**대안**: Trigger Collider는 물리 엔진이 수집 이벤트를 제공하지만, 다수 Gem에 Collider/Rigidbody를 추가해야 하므로 이 규모에서는 거리 비교를 선택했습니다.

## 9. Object Pooling

**기능과 목적**: Enemy, Projectile, EXP Gem의 반복 생성·반환 비용을 줄이고, 같은 Build에서 Pooling ON/OFF를 비교할 수 있게 합니다.

**동작 방식**: `PoolManager`는 Prefab별 `Stack` Pool을 관리합니다. `usePooling` ON에서는 Spawn/Despawn이 Pool을 사용하고 OFF에서는 기존 Instantiate/Destroy 경로를 사용합니다. 재사용 객체는 `Awake`에서 참조를 캐싱하고 `OnEnable`에서 HP, 명중, Collider, Animator 등 사용 상태를 초기화합니다.

**장점과 한계**: 원본 PERF_LOG에서 100 Enemy 생성과 대량 제거·생성 worst 비용 일부가 줄었습니다. 반면 활성 Enemy의 이동·Physics 비용을 없애지는 못했고, SetActive 및 상태 초기화 책임은 여전히 남습니다.

**대안**: Queue도 재사용은 가능하지만, 최근 반환 객체를 다시 꺼내는 Stack이 이 프로젝트의 단순한 LIFO 캐시에 적합하다고 판단했습니다. 측정 수치는 [PERF_LOG.md](PERF_LOG.md)만 사용합니다.

## 10. UI / Settings / Audio

**기능과 목적**: HUD·결과·Pause·설정·오디오로 플레이 상태와 기본 조작 피드백을 제공합니다.

**동작 방식**: HUD는 HP, EXP, Level, 시간을 표시하고 ResultPanel은 승패·재시작을 제공합니다. ESC PauseMenu는 Resume, Settings, Quit 흐름을 관리합니다. `DisplaySettings`는 Windowed/FullScreenWindow와 지원 해상도를 적용하며, Audio Settings Slider는 BGM/SFX AudioSource volume을 즉시 바꾸고 SFX Preview는 `Time.unscaledTime` 기준 0.2초 간격으로 재생합니다. `GameAudio`는 BGM/SFX Source를 분리하고 Player 사망음 뒤 Lose SFX를 realtime 0.4초 뒤 재생합니다.

**장점과 한계**: Pause 중에도 설정 Slider가 동작하고, SFX Source가 지속되므로 비활성화되는 Gem의 수집음이 끊기지 않습니다. AudioMixer·설정 저장·Mute·Master Volume은 구현하지 않았습니다.

**대안**: AudioMixer와 PlayerPrefs는 장기 설정 기능에는 적합하지만, 현재 요구 범위를 넘어 별도 Mixer 구조와 저장 정책이 필요해 추가하지 않았습니다.

## 11. Benchmark Tools

**기능과 목적**: 일반 배포와 성능 실험을 분리하면서 F1 Burst, Overlay, Reset, 전체 처치, 시간대별 로직 검증을 재현합니다.

**동작 방식**: `PerfMonitor`의 Inspector `Benchmark Tools Enabled`가 ON일 때만 F1/F3/F4/F5/F6/F7과 1280×720·VSync OFF 강제가 활성화됩니다. F6/F7은 GameManager의 game clock에 각각 30/60초를 더하며 Pause·Upgrade 중에는 작동하지 않습니다.

**장점과 한계**: 일반 프로토타입의 화면·VSync 설정을 침범하지 않고, 공식 성능 측정용 Windows non-development Build 경로를 보존합니다. F6/F7은 simulation 가속이 아니라 건너뛴 시간의 Enemy·EXP·Upgrade 상태를 재현하지 않습니다.

**대안**: `Time.timeScale`을 올리면 Physics, Animation, accumulator 등 전체 simulation이 같이 변합니다. 특정 시간대 Spawn·Final Rush·승리 조건만 빠르게 확인하려는 목적에 맞지 않아 game-clock jump를 사용했습니다.

## 12. Balance Iteration

초기 프로토타입의 느린 성장, 어려운 Gem 회수, 낮은 기본 화력·생존 여유, 빠른 중반 압박을 기준으로 현재 설정을 조정했습니다.

| 항목 | 과거 설정 | 현재 설정 | 조정 목적 |
| --- | --- | --- | --- |
| EXP 기준값 | 100 | 10 | 초반 레벨업과 카드 선택 경험 |
| Player Max HP | 100 | 200 | 접촉 피해 누적의 생존 여유 |
| Pickup Radius | 1.5 | 2.5 | Gem 회수 부담 완화 |
| 기본 무기 | 단발 | 3발 Shotgun, Spread 20도 | 근접 군집 대응 |
| 무기 성장 | 없음 | 기본/방사형 피해 강화 | 공격 성장 보상 |
| Max Level | 10 | 20 | 5분 내 선택 폭 유지 |
| Spawn | 4/8/14 단계식 | 3→7/s + 10→18/s Final Rush | 성장과 클라이맥스 분리 |

이 변경 이후의 성능 수치는 새로 측정하지 않았습니다. 따라서 기존 성능 수치를 현재 밸런스의 성능 결과로 해석하지 않습니다.

## 13. Related Documentation

- [README.md](README.md): 포트폴리오 요약, 성능 결과, 실행 방법
- [DECISIONS.md](DECISIONS.md): 설계 선택과 트레이드오프
- [PERF_LOG.md](PERF_LOG.md): 원본 측정 조건과 수치
- [TROUBLESHOOTING.md](TROUBLESHOOTING.md): 실제 오류와 해결 기록
- [UPDATELOG.md](UPDATELOG.md): 날짜별 구현·변경 이력
