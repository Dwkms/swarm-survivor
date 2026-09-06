# Swarm Survivor

Unity 6로 만든 2D Survivor 프로토타입입니다. 기능 개수보다 **문제 발견 → 가설 → 구현 → 측정 → 결과 분석 → 판단 → 기록**의 성능 최적화 과정을 보여주는 포트폴리오를 목표로 합니다.

## 목차

- [Project Overview](#project-overview)
- [Gameplay](#gameplay)
- [Tech Stack](#tech-stack)
- [Core Features](#core-features)
- [How to Play](#how-to-play)
- [Performance Optimization](#performance-optimization)
- [Benchmark Method](#benchmark-method)
- [Key Design Decisions](#key-design-decisions)
- [Controls](#controls)
- [Third-Party Assets](#third-party-assets)
- [Run / Setup](#run--setup)
- [Project Status](#project-status)
- [Documentation](#documentation)

## Project Overview

5분 생존 루프 위에서 Object Pooling의 효과와 한계를 같은 조건으로 비교했습니다. 대량 Enemy 환경에서 생성·반환, 이동, Burst 상황을 분리해 frame time(ms)의 `AVG`와 `worst`를 기록하고, 측정 결과가 설계 판단으로 이어지는 과정을 문서화했습니다.

## Gameplay

[Windows Build v1.0.0에서 Gameplay 확인](https://github.com/Dwkms/swarm-survivor/releases/tag/v1.0.0)

## Tech Stack

| 영역 | 사용 기술 |
| --- | --- |
| Engine | Unity 6 (6000.0.82f1) |
| Language | C# |
| Render Pipeline | Universal 2D / URP |
| Physics | Rigidbody2D, Physics2D Layer Collision Matrix |
| Version Control | Git / GitHub |

## Core Features

5분 생존, 자동 조준 3발 Shotgun과 해금형 8방향 Radial Weapon, EXP·레벨업·3장 Upgrade 선택, 회전형 섹터 Spawn과 Final Rush, Pooling, HUD·Pause·Display/Audio Settings, 비정지형 Upgrade 현황 Overlay를 구현했습니다. 상세 기능과 구현 이유는 [상세 기능 및 구현 설명](FEATURES.md)에서 확인할 수 있습니다.

## How to Play

1. `WASD` 또는 방향키로 이동합니다. 기본 무기는 최근접 Enemy를 자동 조준해 발사합니다.
2. Enemy가 남긴 EXP Gem을 회수해 레벨업합니다.
3. 레벨업마다 제시되는 3장의 Upgrade 카드 중 하나를 선택합니다.
4. `U`로 현재 획득한 Upgrade와 Stack을 확인할 수 있습니다. 이 Overlay를 열어도 게임은 계속 진행됩니다.
5. 0~4분은 성장 구간이며 Spawn Rate가 3/s에서 7/s까지 증가합니다. 마지막 1분은 10/s에서 18/s까지 증가하는 Final Rush입니다.
6. 5분 생존 시 승리하며, HP가 0이 되면 실패합니다.

## Performance Optimization

### Object Pooling

`PoolManager.usePooling` 스위치로 같은 Build와 같은 입력 조건에서 Instantiate/Destroy와 Stack 기반 Pooling을 비교했습니다. Pool 객체의 재사용 상태는 `OnEnable`에서 초기화하며, Enemy·Projectile·EXP Gem이 기존 Spawn/Despawn 경로를 그대로 사용합니다.

아래 `/` 값은 첫 실행 워밍업을 제외한 2·3회차 기록입니다. 항목명과 수치는 [PERF_LOG.md](PERF_LOG.md)의 원본 기록을 그대로 사용했습니다.

| PERF_LOG 측정 지표 | OFF (`Instantiate`/`Destroy`) | ON (Pooling) | PERF_LOG 기록 |
| --- | ---: | ---: | --- |
| A. 생성 (`F1`, Enemy 100) | 1.24 / 1.93 ms | 0.93 / 2.90 ms | 단일 개선율 결론 보류 |
| B. 이동 AVG (Enemy 900) | 3.73 / 3.58 ms | 3.42 ms | 변화 없음 |
| C. 제거+생성 worst (`F5`) | 15.10 / 16.17 ms | 11.72 / 11.81 ms | 약 25% 감소 |

`F1` Enemy 100 Spawn 조건의 GC Alloc은 0 KB로 측정됐습니다. 따라서 Pooling으로 GC가 크게 감소했다고 주장하지 않고, 생성 프레임과 대량 제거·생성 구간의 비용 일부를 줄였다는 결과만 기록합니다.

### Performance Conclusion

Pooling은 생성 비용과 대량 Destroy/Spawn이 겹치는 구간의 worst frame time을 줄였습니다. 반면 Pooling이 이미 활성화된 Enemy의 이동·Physics 비용까지 제거하지는 못했으며, Enemy 900 이동 AVG에서는 뚜렷한 개선이 없었습니다.

이 결과를 바탕으로 활성 Enemy 상한은 400으로 유지했습니다. 60fps frame budget(16.67ms)을 넘을 가능성은 400/600 Enemy 측정 사이에서 추정한 약 430마리 지점으로 기록돼 있으며, 직접 실측값과 추정값을 구분합니다. 현재 밸런스 변경 이후의 성능 결과로 과거 수치를 재해석하지 않습니다.

## Benchmark Method

공식 성능 측정은 Unity Editor가 아닌 Windows non-development Build에서 수행했습니다.

- Windows Build / non-development / VSync OFF / Windowed 1280×720
- 동일 PC, 동일 게임 조건
- `autoSpawnEnabled = false`, `BulletWeapon` OFF, `PlayerStats.maxHealth = 99999`
- 첫 실행 워밍업 제외, 2·3회차 기록
- FPS 대신 frame time(ms)의 `AVG`와 `worst` 중심 기록
- 60fps frame budget: 16.67ms

`PerfMonitor`는 `Time.unscaledDeltaTime`으로 측정합니다. 일반 배포에서는 Benchmark Tools를 OFF로 두며, 도구 사용법은 [FEATURES.md의 Benchmark Tools](FEATURES.md#11-benchmark-tools)를 참고합니다.

## Key Design Decisions

- Rigidbody2D 이동은 `FixedUpdate`, 카메라 추적은 `LateUpdate + SmoothDamp`로 분리합니다.
- Enemy↔Enemy 충돌을 비활성화하고 자동 Spawn Enemy에 목표 Offset을 둡니다.
- EXP Gem은 Collider/Rigidbody 대신 거리 비교로 회수합니다.
- Pool은 Stack으로 관리하고 재사용 상태는 `OnEnable`에서 초기화합니다.
- 게임 시간은 `Time.deltaTime`, 성능 측정 시간은 `Time.unscaledDeltaTime`을 사용합니다.
- 자동 Spawn은 회전형 180도 섹터, 활성 Enemy 상한은 400으로 유지합니다.

선택 이유와 트레이드오프는 [DECISIONS.md](DECISIONS.md)에 기록했습니다.

## Controls

| 입력 | 동작 |
| --- | --- |
| `W` `A` `S` `D` / 방향키 | Player 이동 |
| Mouse Click | Upgrade 카드와 UI 버튼 선택 |
| `U` | Upgrade 현황 표시/숨김 |
| `ESC` | PauseMenu 열기/닫기, Settings에서 뒤로 가기 |

## Third-Party Assets

게임 로직과 성능 측정·최적화 코드 작성에는 생성형 AI 코딩 도구를 적극 활용했습니다. 기능 요구사항과 성공 조건을 정의하고, Unity 적용·Play/Build 테스트·성능 측정·가설 검증·최종 판단을 직접 수행했습니다. 시각·오디오 Asset은 아래 외부 Asset을 사용합니다.

- Asset: **Undead Survivor Asset Pack**
- Creator: Goldmetal
- Source: Unity Asset Store
- License: Standard Unity Asset Store EULA

The original Asset Store files are not included in this repository. `Assets/Undead Survivor/`와 해당 `.meta`는 `.gitignore`에서 제외됩니다.

## Run / Setup

1. Unity Hub에서 Unity **6000.0.82f1**로 프로젝트를 엽니다.
2. 필요한 환경에서는 Undead Survivor Asset Pack을 별도로 Import합니다.
3. `Assets/Scenes/SampleScene.unity`를 엽니다.
4. Play를 실행합니다.

## Project Status

핵심 게임 루프, 성능 측정·Pooling 비교 경로, UI/Settings/Audio, Windows Build 검증을 마친 완성 프로토타입 상태입니다. 향후 확장 후보는 Boss 같은 게임 콘텐츠 추가이며, 기존 성능 측정 기록은 현재 상태와 분리해 보존합니다.

## Documentation

- [FEATURES.md](FEATURES.md): 기능별 동작, 구현 방식, 이유
- [DECISIONS.md](DECISIONS.md): 설계 선택과 트레이드오프
- [PERF_LOG.md](PERF_LOG.md): 원본 성능 측정 조건과 결과
- [TROUBLESHOOTING.md](TROUBLESHOOTING.md): 실제 오류 해결 기록
- [UPDATELOG.md](UPDATELOG.md): 날짜별 구현·검증 이력
