# Swarm Survivor

Unity 6로 만든 2D Survivor 프로젝트입니다. 이 프로젝트의 목적은 게임 기능을 많이 만드는 것이 아니라,
**문제 발견 → 가설 → 구현 → 측정 → 결과 분석 → 판단 → 기록**의 흐름으로 성능 최적화를 설명하는 것입니다.

적을 대량으로 생성하는 환경에서 `Instantiate`/`Destroy`와 오브젝트 풀링을 같은 조건으로 비교하고,
풀링이 실제로 줄이는 비용과 줄이지 못하는 비용을 분리해 기록했습니다.

## 목차

- [Project Overview](#project-overview)
- [Tech Stack](#tech-stack)
- [Core Features](#core-features)
- [Performance Optimization](#performance-optimization)
- [Benchmark Method](#benchmark-method)
- [Key Design Decisions](#key-design-decisions)
- [Controls](#controls)
- [Third-Party Assets](#third-party-assets)
- [Run / Setup](#run--setup)
- [Project Status](#project-status)
- [Documentation](#documentation)

## Project Overview

Swarm Survivor는 5분 생존형 탑다운 게임 루프 위에 성능 측정 도구를 구성한 포트폴리오 프로젝트입니다.
측정 중 결론이 여러 번 뒤집힌 과정을 숨기지 않고, 가설을 기각하거나 측정 방법을 수정한 이유까지 문서로 남겼습니다.

핵심 질문은 다음과 같습니다.

- 적이 많아졌을 때 어떤 부하가 커지는가?
- 오브젝트 풀링은 그 부하 중 어디까지 개선하는가?
- 공정한 비교를 위해 어떤 조건을 통제해야 하는가?

## Tech Stack

| 영역            | 사용 기술                                     |
| --------------- | --------------------------------------------- |
| Engine          | Unity 6 (6000.0.82f1)                         |
| Language        | C#                                            |
| Render Pipeline | Universal 2D / URP                            |
| Physics         | Rigidbody2D, Physics2D Layer Collision Matrix |
| Version Control | Git / GitHub                                  |

## Core Features

- 5분 생존, 체력 0 패배, 결과 화면 및 재시작
- WASD 이동과 자동 투사체 전투
- EXP 젬 획득, 레벨업, 3개 업그레이드 카드 선택
- 자동 적 스폰: 180도 회전 섹터, 5초 간격 90도 회전
- 시간대별 스폰율: 0~1분 4/s, 1~3분 8/s, 3분 이후 14/s
- 자동 스폰 활성 적 상한 400
- 자동 게임플레이 Enemy의 개인별 접근 목표 Offset으로 과도한 한 점 중첩 완화
- 적·투사체·EXP 젬 Object Pooling과 ON/OFF 비교 경로
- HUD, 성능 모니터, PauseMenu, 화면 모드·해상도 설정
- PauseMenu의 Resume, Settings, Game Quit 및 Result/Upgrade UI 우선순위 처리

## Performance Optimization

### Object Pooling

`PoolManager.usePooling` 스위치로 같은 빌드·같은 세션에서 `Instantiate`/`Destroy`와 풀링 경로를
전환합니다. 풀은 `Stack`으로 관리하며, 풀 대상의 재사용 상태는 `OnEnable`에서 초기화합니다.

실제 측정에서는 적, 투사체, EXP 젬을 모두 풀링 대상으로 전환했습니다.

아래 `/` 구분값은 워밍업 1회차를 제외한 **2·3회차 기록**입니다. 단일 값은 `PERF_LOG.md`에
한 값만 기록된 경우 그대로 표기했습니다. 지표 이름은 `PERF_LOG.md`의 측정 항목을 그대로 사용했습니다.

| PERF_LOG 측정 지표          | OFF (`Instantiate`/`Destroy`) |        ON (풀링) | PERF_LOG 기록           |
| --------------------------- | ----------------------------: | ---------------: | ----------------------- |
| A. 생성 (`F1`, Enemy 100개) |                1.24 / 1.93 ms |   0.93 / 2.90 ms | 중앙값 기준 약 40% 감소 |
| B. 이동 AVG (Enemy 900마리) |                3.73 / 3.58 ms |          3.42 ms | 변화 없음               |
| C. 파괴+생성 worst (`F5`)   |              15.10 / 16.17 ms | 11.72 / 11.81 ms | 약 25% 감소             |

`F1`으로 Enemy 100개를 생성하는 기준선에서는 GC Alloc이 **0 KB**로 측정됐습니다.
따라서 이 프로젝트는 “풀링으로 GC가 크게 감소했다”라고 주장하지 않습니다.

### Performance Conclusion

풀링은 생성 프레임과 대량 파괴·생성이 겹치는 최악 프레임의 일부를 줄였습니다. 반면 이미 활성화된
Enemy의 이동·Physics 비용은 줄이지 못했습니다. 즉, 풀링은 생성·파괴 비용에 대한 선택이지 모든
프레임 비용을 없애는 해결책은 아닙니다.

이동 중 400마리에서 `worst`는 14.89ms였고, 60fps 프레임 예산 초과 예상 지점은
400~600마리 측정 결과를 기반으로 한 추정치인 약 430마리입니다. 이 결과를 근거로 자동 스폰의 활성 Enemy 상한은
**400**으로 유지했습니다.

900마리 이동 비용의 초선형 증가와 물리 브로드페이즈의 관계는 유력 가설로만 기록했습니다.
검증하지 않은 병목 원인을 사실로 단정하지 않았습니다.

## Benchmark Method

성능 기준선은 Unity Editor가 아니라 다음 조건의 Windows Build에서 측정했습니다.

- Windows Build / non-development / VSync OFF / 창모드 1280×720
- 동일 데스크탑, 동일 게임 조건
- `autoSpawnEnabled = false`, `BulletWeapon` OFF, `PlayerStats.maxHealth = 99999`
- 첫 실행 워밍업을 제외하고 2·3회차 기록
- FPS 대신 frame time(ms)의 `AVG`와 `worst` 중심으로 기록
- 60fps frame budget: **16.67ms**

`PerfMonitor`는 `Time.unscaledDeltaTime`으로 프레임 시간을 수집합니다. `now`는 직전 0.5초 평균인
참고값이고, `F4` 이후 누적한 `AVG`와 `worst`를 기록값으로 사용합니다.

상세 측정 조건, 이상치, 기각한 가설은 [PERF_LOG.md](./PERF_LOG.md)에 남겨두었습니다.

## Key Design Decisions

- **물리 이동은 FixedUpdate**: Rigidbody2D 속도는 `linearVelocity`로 지정합니다.
- **카메라는 LateUpdate + SmoothDamp**: 물리 이동 이후를 추적하고, 카메라 Z는 고정합니다.
- **Enemy ↔ Enemy 충돌 비활성화**: 수백 Enemy의 충돌 쌍과 플레이어 포위벽을 피합니다.
- **접근 목표 Offset**: 자동 스폰 Enemy만 플레이어 중심 주변의 작은 Offset을 사용하며, 주변 탐색이나 Physics Query 기반 separation은 추가하지 않습니다.
- **ExpGem은 물리 없이 거리 비교로 획득**: 대량 젬의 Collider/Rigidbody 비용을 피합니다.
- **Pool은 Stack, 재사용 초기화는 OnEnable**: 방금 반납한 오브젝트의 재사용과 풀링 상태 초기화를 단순하게 유지합니다.
- **게임 시간과 측정 시간을 분리**: 게임 진행은 `Time.deltaTime`, 성능 측정은 `Time.unscaledDeltaTime`을 사용합니다.
- **자동 스폰은 회전 섹터**: 스폰 수·반경은 유지하면서 플레이어가 빠져나갈 빈 방향을 만듭니다.
- **F1은 전체 원주를 유지**: 게임플레이 스폰과 분리해 기존 성능 계측 기준선을 보존합니다.

설계 선택의 근거와 버린 대안은 [DECISIONS.md](./DECISIONS.md)에서 확인할 수 있습니다.

## Controls

### Gameplay

| 입력            | 동작                                                    |
| --------------- | ------------------------------------------------------- |
| `W` `A` `S` `D` | 플레이어 이동                                           |
| `ESC`           | PauseMenu 열기/닫기, SettingsPanel에서 PauseMenu로 복귀 |
| PauseMenu 버튼  | Resume, Settings, Game Quit                             |

무기는 자동으로 발사됩니다.

### Developer / Benchmark

| 입력 | 동작                                                                   |
| ---- | ---------------------------------------------------------------------- |
| `F1` | Enemy 100마리 즉시 생성. 360도 전체 원주를 유지하는 성능 계측용 버스트 |
| `F3` | 기본 숨김 상태의 성능 오버레이 표시/숨김                               |
| `F4` | 성능 모니터의 `AVG`, `worst`, burst 측정값 리셋                        |
| `F5` | 살아있는 Enemy 전체 처치. 게임 로직 테스트용                           |

## Third-Party Assets

게임 로직, 성능 측정, 최적화 코드는 직접 구현했습니다. 시각 아트에는 아래 외부 에셋을 사용합니다.

- Asset: **Undead Survivor Asset Pack**
- Creator: Goldmetal
- Source: Unity Asset Store
- License: Standard Unity Asset Store EULA

The original Asset Store files are not included in this repository.

`Assets/Undead Survivor/`와 해당 `.meta` 파일은 `.gitignore`로 제외되어 있습니다.

## Run / Setup

1. Unity Hub에서 Unity **6000.0.82f1**로 프로젝트를 엽니다.
2. 시각 에셋이 필요한 환경에서는 Undead Survivor Asset Pack을 별도로 Import합니다.
3. `Assets/Scenes/SampleScene.unity`를 엽니다.
4. Play를 실행합니다.

## Project Status

핵심 게임 루프, 성능 측정·풀링 비교 경로, PauseMenu와 디스플레이 설정이 구현돼 있습니다.
Unity Play 및 Windows Build에서 게임 진행, PauseMenu, 화면 모드·해상도 변경, HUD/UI 레이아웃을 검증했습니다.

이 프로젝트의 중심 산출물은 “풀링을 적용했다”는 사실이 아니라, **측정 조건을 통제하고 실제 결과로
주장을 제한한 과정**입니다.

## Documentation

- [PERF_LOG.md](./PERF_LOG.md) — 측정 조건, 실제 수치, 이상치와 기각한 가설
- [DECISIONS.md](./DECISIONS.md) — 설계 선택의 이유와 트레이드오프
- [TROUBLESHOOTING.md](./TROUBLESHOOTING.md) — 실제 증상, 원인, 판별, 해결
- [UPDATELOG.md](./UPDATELOG.md) — 날짜별 구현 및 검증 기록
