# AGENTS.md — Swarm Survivor 작업 규칙

이 문서는 Swarm Survivor 저장소에서 작업하는 에이전트와 협업자를 위한 현재 작업 기준입니다. 프로젝트의 목적은 단순 기능 구현이 아니라, 성능 문제를 발견하고 같은 조건에서 검증·기록하는 과정을 보여주는 것입니다.

## 목차

- [1. 프로젝트와 환경](#1-프로젝트와-환경)
- [2. 문서 우선순위](#2-문서-우선순위)
- [3. 작업 원칙](#3-작업-원칙)
- [4. 확정 기술 규칙](#4-확정-기술-규칙)
- [5. 현재 최종 게임 설정](#5-현재-최종-게임-설정)
- [6. 성능 측정과 Benchmark Mode](#6-성능-측정과-benchmark-mode)
- [7. 완료 상태와 범위](#7-완료-상태와-범위)
- [8. Unity Editor 및 Git](#8-unity-editor-및-git)

## 1. 프로젝트와 환경

- Unity 6 `6000.0.82f1`, Universal 2D / URP, C#을 사용합니다.
- 프로젝트 경로는 `C:\dev\swarm-survivor`입니다.
- Active Input Handling은 Both이며 기존 입력은 `Input.GetAxisRaw`를 사용합니다.
- Unity 6에서는 `Rigidbody2D.velocity` 대신 `linearVelocity`를 사용합니다.
- 외부 코드 패키지는 추가하지 않습니다. Cinemachine, DOTween, ScriptableObject 기반 확장은 현재 범위 밖입니다.
- Asset Store의 Undead Survivor 원본 Asset은 저장소에 포함하지 않으며 `.gitignore` 예외를 유지합니다.

## 2. 문서 우선순위

작업 전에는 요청과 관련된 문서를 먼저 읽고, 작업 후에는 실제 변경 범위에 맞춰 갱신합니다.

| 문서 | 역할 |
| --- | --- |
| [README.md](README.md) | 포트폴리오 진입점과 성능 요약 |
| [FEATURES.md](FEATURES.md) | 현재 기능, 구현 방식, 플레이 흐름 |
| [DECISIONS.md](DECISIONS.md) | 설계 선택과 트레이드오프 |
| [PERF_LOG.md](PERF_LOG.md) | 원본 성능 측정 조건과 수치 |
| [TROUBLESHOOTING.md](TROUBLESHOOTING.md) | 실제 증상·원인·판별·해결 |
| [UPDATELOG.md](UPDATELOG.md) | 날짜별 구현·검증 이력 |

모든 `docs` 문서는 제목 바로 아래에 목차를 둡니다. 과거 측정값과 변경 이력은 삭제하거나 현재 결과로 재해석하지 않습니다.

## 3. 작업 원칙

1. 한 작업에서는 요청한 하나의 목표만 다룹니다. 관련 없는 리팩터링과 설정 변경은 하지 않습니다.
2. 문서와 코드가 다르면 임의로 선택하지 않고 문서 기준·실제 상태·영향·권장 처리를 먼저 보고합니다.
3. Inspector, Scene, Prefab 변경이 필요하면 Unity Editor에서 사용자가 수행할 항목을 구체적으로 전달합니다. 저장하지 않은 Editor 변경을 코드에서 가정하지 않습니다.
4. Inspector 노출 필드는 기본적으로 `[SerializeField] private`를 사용합니다. 필수 참조 누락은 초기화 시 `Debug.LogError`로 명확하게 알립니다.
5. 주기 처리는 accumulator + `while`을 우선하며, `Lerp(a, b, t * deltaTime)` 같은 프레임 의존 보간은 사용하지 않습니다.
6. 완료 보고에는 변경 파일, 동작 확인 방법, Unity Editor 작업, 범위 밖 파일 미변경 여부, 제안 커밋 메시지와 git 명령을 포함합니다.

## 4. 확정 기술 규칙

- Player Rigidbody2D 이동은 `FixedUpdate`, CameraFollow는 `LateUpdate + Vector3.SmoothDamp`를 사용합니다. 카메라 Z는 Awake에서 캐싱해 유지합니다.
- Player Rigidbody2D는 `Interpolate = Interpolate`, `Sleeping Mode = Never Sleep`을 사용합니다.
- Enemy↔Enemy, Projectile↔Projectile, Projectile↔Player 충돌은 비활성화하고 Player↔Enemy, Projectile↔Enemy Trigger만 사용합니다.
- EXP Gem은 Collider/Rigidbody 없이 거리 비교로 수집합니다.
- Pool은 `Stack` 기반이며 재사용 상태는 `Awake`가 아닌 `OnEnable`에서 초기화합니다. `PoolManager.usePooling` ON/OFF 비교 경로를 보존합니다.
- 게임 시간은 `Time.deltaTime`, 성능 측정은 `Time.unscaledDeltaTime`을 사용합니다.
- Enemy 사망은 게임 로직과 EXP Gem 생성을 즉시 처리하고, 이동·Collider를 끈 시각적 Dead 상태를 0.3초 유지한 뒤 Pool로 반환합니다.
- 일반 배포에서는 Benchmark Tools를 OFF로 둡니다. F1~F7과 측정용 해상도·VSync 고정은 Benchmark Mode에서만 사용합니다.

## 5. 현재 최종 게임 설정

| 항목 | 현재 값 |
| --- | --- |
| 게임 시간 | 300초 / 5분 |
| Player Max HP | 200 |
| 기본 EXP 회수 반경 | 2.5 |
| Max Level | 20 |
| EXP 기준값 | 10 |
| 활성 Enemy 상한 | 400 |
| 기본 무기 | 0.8초, 3발, Spread 20도, 기본 피해 10 |
| 방사형 무기 | 기본 잠금, 해금 후 2초마다 8방향 발사 |
| 자동 Spawn | 180도 섹터, 5초마다 90도 회전 |
| 성장 구간 | 0~240초, 3/s → 7/s 선형 증가 |
| Final Rush | 240~300초, 10/s → 18/s 선형 증가 |
| Enemy Death Animation | 0.3초 |

업그레이드는 카드 3장 중 하나를 선택합니다. 일반 강화는 최대 3 Stack, RadialShot은 1회 획득이며, 기본·방사형 무기 피해 강화는 각각 10 → 12 → 14 → 16으로 적용됩니다.

## 6. 성능 측정과 Benchmark Mode

공식 성능 수치는 Windows non-development Build / VSync OFF / Windowed 1280×720 / 동일 PC / 첫 실행 워밍업 제외 / 2·3회차 기준입니다. FPS가 아니라 frame time(ms)의 `AVG`와 `worst`를 기록하며 60fps frame budget은 16.67ms입니다.

측정할 때는 `PerfMonitor.Benchmark Tools Enabled`를 ON으로 설정하고, 자동 Spawn을 끄며 `BulletWeapon` OFF, Player Max Health 99999 등의 [PERF_LOG.md](PERF_LOG.md) 조건을 정확히 맞춥니다. Benchmark Mode OFF에서는 F1/F3/F4/F5/F6/F7, 측정 Overlay, 1280×720 강제, VSync OFF 강제가 모두 동작하지 않습니다.

| 키 | Benchmark Mode ON 동작 |
| --- | --- |
| F1 | Enemy 100마리 360도 Burst Spawn |
| F3 | 성능 Overlay 표시/숨김 |
| F4 | 측정값 Reset |
| F5 | 활성 Enemy 전체 즉사 테스트 |
| F6 / F7 | game clock +30초 / +60초 |

F6/F7은 simulation 가속이 아니며 건너뛴 시간의 Enemy를 소급 생성하지 않습니다. 이 실행은 밸런스·성능 측정 결과가 아니라 특정 시간대 로직을 빠르게 확인하는 용도입니다.

## 7. 완료 상태와 범위

현재 구현에는 5분 생존, Enemy 자동 Spawn·회전형 섹터·Final Rush, 3발 Shotgun, 해금형 Radial Weapon, EXP·Level·Upgrade, Upgrade 현황 Overlay, Object Pooling, HUD·Result, Pause·Display·Audio Settings, 기본 오디오·애니메이션, Benchmark Mode가 포함됩니다.

Boss, 추가 캐릭터/Enemy 종류, Save/Load, 모바일 빌드, WeaponManager/WeaponBase/ScriptableObject 구조는 현재 범위에 포함하지 않습니다. `DamageText`는 확장 후보로만 남겨 두며 요청 없이 구현하지 않습니다.

## 8. Unity Editor 및 Git

Scene, Prefab, Animator, AudioClip, Slider, Inspector 참조는 Unity Editor에서 설정한 뒤 `Ctrl+S`로 저장해야 합니다. 코드 변경만으로 Scene 설정이 적용됐다고 가정하지 않습니다.

작업 전후에는 다음을 확인합니다.

```bash
git status
git diff --check
git diff -- docs/README.md docs/FEATURES.md docs/AGENTS.md docs/DECISIONS.md docs/UPDATELOG.md
```

문서 작업의 권장 커밋 메시지는 `docs: finalize portfolio documentation`입니다.
