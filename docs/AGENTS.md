# AGENTS.md — Swarm Survivor 작업 규칙

이 문서는 AI 에이전트(Codex · Claude Code)가 이 저장소에서 작업할 때의 규칙입니다.
**작업 시작 전에 이 문서를 끝까지 읽고, 아래 "문서 지도"의 해당 문서를 먼저 확인하세요.**

---

## 0. 이 프로젝트가 무엇인가

**재미있는 게임을 만드는 것이 아니라 성능 최적화 과정을 증명하는 것이 목적입니다.**

2D 탑다운 뱀서라이크의 형태를 빌렸지만, 산출물은 게임이 아니라
**"어느 부하가 얼마나 컸고, 오브젝트 풀링이 그중 무엇을 얼마나 줄였는지"** 의 측정 기록입니다.

- 게임 회사 인턴 지원용 포트폴리오
- **마감 2026년 9월 6일**
- 작업자: 게임공학 전공, 졸업 후 3년 공백. Unity·C#을 3년 만에 다시 잡음
- 데스크탑과 노트북 두 대를 오가며 작업

**핵심 측정은 2026-09-04에 완료됐습니다.** 남은 것은 게임 완성과 마감 작업입니다.

---

## 1. 문서 지도 — 먼저 읽을 것

| 문서 | 언제 읽나 |
|---|---|
| [`docs/README.md`](docs/README.md) | 프로젝트 전체 구조와 측정 결과 요약 |
| [`docs/PERF_LOG.md`](docs/PERF_LOG.md) | **성능 관련 작업 전 필수.** 측정 조건·수치·폐기한 결론 |
| [`docs/DECISIONS.md`](docs/DECISIONS.md) | **설계를 바꾸기 전 필수.** 왜 그 선택을 했는지 |
| [`docs/TROUBLESHOOTING.md`](docs/TROUBLESHOOTING.md) | 문제가 생겼을 때. 18건이 증상·원인·판별·해결로 정리돼 있음 |
| [`docs/UPDATELOG.md`](docs/UPDATELOG.md) | 날짜별 작업 내역과 판단 |

**작업이 끝나면 해당 문서를 갱신하세요.** 문체는 `~합니다`체입니다.

---

## 2. 응답 규칙

1. **한 번에 한 기능만 다룹니다.** 여러 기능을 몰아서 주지 마세요
2. 코드를 줄 때는 항상 넷을 함께 제공합니다
   - **Unity 에디터에서 사람이 해야 할 조작** (오브젝트 생성, 컴포넌트 추가, Inspector 값)
   - ▶ 재생 후 무엇이 보이면 성공인지
   - 커밋 메시지 제안
   - git 명령어
3. **코드에 주석을 적극적으로 답니다.** 작업자가 면접에서 직접 설명해야 합니다
4. **설계 판단에는 반드시 이유를 붙입니다**
5. 3년 사이 바뀐 API와 흔한 실수를 먼저 짚습니다
6. **`Ctrl+S` 씬 저장을 상기시킵니다.** 컴포넌트 설정은 `.unity`에만 저장됩니다

### 에이전트가 할 수 없는 일

**Unity 에디터 조작은 사람이 합니다.** 오브젝트 생성, 컴포넌트 추가, Inspector 값 입력,
프리팹 편집, 씬 저장은 코드로 대신할 수 없습니다.

에디터 작업이 필요하면 **단계별로 명확히 지시하고, 사람이 완료했다고 알릴 때까지 기다리세요.**
"아마 되어 있을 것"으로 넘어가면 안 됩니다.

---

## 3. 개발 환경

- Unity 6 (6000.0.82f1), Universal 2D (URP)
- Active Input Handling = Both. 구식 `Input.GetAxisRaw`
- `C:\dev\swarm-survivor` · `github.com/Dwkms/swarm-survivor`

### 3년 사이 바뀐 API

| 옛것 | 지금 |
|---|---|
| `Rigidbody2D.velocity` | **`linearVelocity`** |
| `FindObjectsOfType` | **`FindObjectsByType(FindObjectsSortMode.None)`** |
| `FindObjectOfType` | **`FindAnyObjectByType`** |
| `Create → C# Script` | `Create → Scripting → MonoBehaviour Script` |

---

## 4. 확정된 설계 결정 (재논의 금지)

근거는 [`docs/DECISIONS.md`](docs/DECISIONS.md)에 있습니다. **바꾸자고 제안하지 마세요.**

### 물리·프레임

- 물리로 움직이는 오브젝트는 `Interpolate = Interpolate`
- **`Lerp(a, b, t * deltaTime)` 형태 금지.** `SmoothDamp` 또는 명시적 속도 제어
- 이동은 `FixedUpdate`, 따라가는 로직은 `LateUpdate`
- 주기적 처리는 **시간 누적 방식**(accumulator + `while`)으로 프레임 독립 처리

### 시간 축이 둘입니다

- **게임 진행** → `Time.deltaTime` (업그레이드 카드 선택 중 `timeScale = 0`이면 멈춰야 함)
- **성능 측정** → `Time.unscaledDeltaTime` (프레임은 `timeScale`과 무관하게 흐름)

### 카메라

- Cinemachine 미사용. `CameraFollow` 자작
- Orthographic Size 6, **Position Z = -10** (z는 `Awake`에서 캐싱)
- `LateUpdate` + `Vector3.SmoothDamp`, smoothTime 0.15

### 레이어

- Layer: `Player` / `Enemy` / `Projectile`
- **해제**: `Enemy ↔ Enemy`, `Projectile ↔ Projectile`, `Projectile ↔ Player`
- **유지**: `Projectile ↔ Enemy`, `Player ↔ Enemy`
- **Trigger도 이 매트릭스를 따릅니다.** 꺼져 있으면 Trigger 콜백 자체가 안 옵니다
- 적 Collider는 `Is Trigger` (적이 쌓이면 플레이어가 갇히는 문제로 전환)
- Player Rigidbody2D는 `Sleeping Mode = Never Sleep` (잠들면 `OnTriggerStay2D`가 안 옴)

### 풀링

- 풀 자료구조는 `Stack` (LIFO — 캐시 지역성)
- 반납처는 `PooledObject` 마커가 기억
- **부모는 생성 시 한 번만 정하고 다시 바꾸지 않습니다**
- **초기화는 `Awake`가 아니라 `OnEnable`에서** — 풀은 이전 사용의 상태를 물려줍니다
- `PoolManager.usePooling` 스위치로 `Instantiate`/`Destroy` 경로와 전환 가능 (측정용, 유지)

### 코드 컨벤션

- Inspector 노출은 `[SerializeField] private`
- **조용히 실패하는 null 가드를 두지 마세요.** `Start`에서 전제 조건을 검사하고 `Debug.LogError`
- 대량 생성되는 오브젝트에서 로그는 `static` 플래그로 한 번만

### 외부 패키지

**코드 패키지는 도입하지 않습니다.** Cinemachine, DOTween, 에셋스토어 스크립트 전부 미사용.
필요한 기능이 대부분 수십 줄로 끝나고, 이 프로젝트의 산출물은 성능 비교이기 때문입니다.

**아트 에셋과 사운드는 예외입니다.** 학습 비용이 없고 드래그해서 넣으면 끝입니다.
CC0 라이선스(Kenney 등)를 쓰고 출처는 README에 적습니다.

---

## 5. 측정 규칙 — 성능을 건드릴 때

이 프로젝트에서 측정 결론이 **네 번 뒤집혔습니다.** 규칙은 그 대가로 얻은 것입니다.

- **지표는 FPS가 아니라 프레임 시간(ms).** 60fps = 16.67ms
- **기준선은 에디터가 아니라 빌드에서 잽니다.** 에디터에서는 `EditorLoop`가 프레임의 86%
- **1회차는 버리고 2·3회차를 기록합니다.** 첫 실행에는 워밍업이 섞입니다. **모든 지표에 적용**
- **한 번에 변수를 하나만 바꿉니다.** 스폰하면서 측정하지 않습니다
- 모든 측정치에 조건을 함께 적습니다 — **이동 여부 / 마릿수 / BulletWeapon ON·OFF /
  창 크기 / 어느 PC**
- **측정 도구를 믿기 전에 도구를 검증합니다.** 알고 있는 방향의 변화를 줬을 때 지표가
  그 방향으로 움직이는지 먼저 확인하세요

### 측정 조건 (재현하려면)

Windows 빌드 / **non-development** / **VSync OFF** / **창모드 1280×720** /
`spawnsPerSecond` 0 / `BulletWeapon` OFF / `PlayerStats.maxHealth` 99999

### 디버그 키

| 키 | 동작 |
|---|---|
| `F1` | 적 100마리 즉시 생성 — **계측 도구** |
| `F3` | 성능 표시 on/off |
| `F4` | 구간 평균·최악 프레임 리셋 |
| `F5` | 살아있는 적 전체 즉사 — **게임 로직 테스트 전용. 측정에 쓰지 마세요** |

---

## 6. 이미 밟은 함정 (반복 금지)

| 증상 | 원인 |
|---|---|
| 플레이어가 미세하게 떨림 | Rigidbody2D `Interpolate` 꺼짐 |
| 화면이 검게 나옴 | 2D 카메라 z가 0 |
| 다른 PC에서 컴포넌트가 비어 있음 | 씬 저장(`Ctrl+S`) 누락 |
| 스폰된 적이 원형으로 멈춰 있음 | **프리팹 원본이 아니라 씬 인스턴스에만 스크립트를 붙임.** 프리팹 수정은 Prefab Mode(더블클릭)에서 |
| 적이 안 나오는데 오류도 없음 | 조용히 `return`하는 null 가드가 원인을 숨김 |
| 총알 한 발이 여러 마리를 죽임 | `Destroy`는 프레임 끝 처리. `hasHit` 플래그 필요 |
| 다른 PC에서 `UnassignedReferenceException` | `.meta` GUID 문제. `.prefab`과 `.prefab.meta`는 쌍으로 커밋 |
| 에디터 성능이 실제와 다름 | `EditorLoop`가 86%. 빌드에서 측정 |
| 마릿수를 늘렸는데 평균이 낮아짐 | 0.5초 창 값을 눈으로 읽음. `AVG`와 `worst`로 기록 |
| 풀에서 꺼낸 적이 즉사 | 상태 초기화가 `OnEnable`에 없음 |
| Image에 `Image Type`이 없음 | `Source Image`가 비어 있음. `UISprite` 지정 |
| UI가 화면 끝까지 안 늘어남 | **부모가 stretch 안 됨.** 앵커 프리셋은 **Alt를 눌러야** 여백까지 0 |
| UI 글자가 너무 작음 | `Canvas Scaler` 기준 해상도 불일치 (현재 1280×720, Match 0.5) |

상세는 [`docs/TROUBLESHOOTING.md`](docs/TROUBLESHOOTING.md)에 있습니다.

---

## 7. 현재 상태 (2026-09-05 기준)

### 완료

`CameraFollow` · `PlayerController` · `PlayerStats` · `Enemy` · `EnemySpawner` ·
`Projectile` · `BulletWeapon` · `PerfMonitor` · `ExpGem` · `ExpCollector` ·
`LevelSystem` · `UpgradeManager` · `UpgradePanel` · `ObjectPool` · `PooledObject` ·
`PoolManager` · `GameManager` · `ResultPanel` · `HUD` · `DisplaySettings` · `PauseMenu`

**게임 루프가 성립합니다.** 적 스폰 → 자동 발사 → 사망 → 젬 → 레벨업 → 카드 선택 →
5분 타이머 → 승패 → 결과 화면 → 재시작.

**오브젝트 풀링 전후 측정이 완료됐습니다.** 결과는 [`docs/PERF_LOG.md`](docs/PERF_LOG.md).

**ESC PauseMenu와 디스플레이 설정이 완료됐고 Windows Build에서 검증됐습니다.** 창 모드·`FullScreenWindow`·
지원 해상도 변경과 해상도 변경 후 UI 레이아웃 유지를 확인했습니다.

### 미구현

`WeaponManager` · `WeaponBase` · `WeaponData(SO)` · `EnemyData(SO)` · `UpgradeData(SO)`

### 범위에서 제외

`DamageText` — 일정상 잘라냈습니다. 다시 제안하지 마세요.

---

## 8. 남은 작업 (우선순위 순)

### P0 — 반드시

| # | 작업 | 메모 |
|---|---|---|
| 1 | **시간대별 스폰율** | `GameManager.ElapsedTime`으로 초당 4 / 8 / 14 전환 |

### P1 — 되면

| # | 작업 |
|---|---|
| 4 | 스프라이트 교체 (Kenney CC0) |
| 5 | 사운드 4종 — 발사·피격·레벨업·BGM |
| 6 | `EnemyData(SO)` 3종 + 적 3종 등장 |
| 7 | 무기 Lv2/Lv3 |

### P2 — 남으면

| # | 작업 |
|---|---|
| 8 | 최종 빌드 · 5분 완주 테스트 |
| 9 | README용 플레이 GIF |
| 10 | 스프라이트 적용 후 900마리 재측정 (시각 교체 비용) |

### 섹터 스폰 — 완료

자동 적 스폰은 화면 밖 **180도 섹터** 안에서 발생하고 5초마다 시작 각도가 90도씩 회전합니다.
Unity Play에서 플레이어가 빠져나갈 수 있는 빈 방향이 생기는 것을 확인했습니다.

PauseMenu와 UpgradePanel 중에는 게임 진행과 섹터 회전이 함께 정지합니다.

F1 성능 계측은 기존처럼 360도 전체 원주를 사용하며, Unity Play에서 특정 반원에 집중되지 않는
전체 방향 분포를 확인했습니다. 기존 스폰율·반경·활성 상한은 변경하지 않았습니다.

### 꾸미기는 측정 뒤에

**핵심 측정이 끝났으므로 이제 스프라이트와 사운드를 넣어도 됩니다.**

다만 현재 모든 수치는 **"무텍스처 컬러 사각형 · 단일 머티리얼"** 기준입니다.
교체 후 재측정하려면 그 조건을 새로 기록해야 합니다. 기존 수치는 그대로 유효합니다.

**사운드 주의** — 적 사망음은 넣지 마세요. `F5`로 900마리를 죽이면 `AudioSource`가 900개
생성되어 프레임이 멈춥니다. 발사음은 `AudioSource` 하나에서 `PlayOneShot`으로 처리합니다.

---

## 9. 측정용 임시 상태 (원복 확인)

측정할 때 바꾸고 **끝나면 반드시 되돌려야 하는 값**입니다. 잊으면 게임이 이상해지는데
에러가 안 납니다.

| 항목 | 측정용 | 정상 |
|---|---|---|
| `PoolManager` → Use Pooling | OFF (비교 시) | **ON** |
| `EnemySpawner` → Spawns Per Second | 0 | **4** |
| Player → `BulletWeapon` 체크박스 | OFF | **ON** |
| Player → `PlayerStats` → Max Health | 99999 | **100** |
| `LevelSystem` → Exp Per Level Base | 3 (테스트) | **100** |
| `GameManager` → Game Duration | 15 (테스트) | **300** |
| Quality → V Sync Count | Don't Sync | **유지** (측정 조건) |

**커밋 전에 이 표를 확인하세요.**

---

## 10. git 규칙

```bash
git pull                      # 작업 시작 전 (두 PC를 오갑니다)
# ... 작업 ...
# Unity에서 Ctrl+S 로 씬 저장
git status                    # .unity / .prefab / .meta 변경 확인
git add .
git commit -m "feat: ..."
git push
```

- **`Ctrl+S` 없이 커밋하면 씬 변경이 빠집니다.** GameObject 추가와 Inspector 설정은
  `.unity` 파일에만 기록됩니다
- **`.meta` 파일은 반드시 함께 커밋합니다.** Unity는 에셋을 이름이 아니라 GUID로 참조합니다
- `Build/`, `Library/`, `Temp/`, `obj/`는 `.gitignore`로 제외돼 있습니다
- 커밋 메시지 접두사: `feat:` `fix:` `perf:` `docs:` `chore:`

---

## 11. 게임 사양

1회 5분. 5분 생존 시 클리어, 체력 0이면 실패. 맵 1종.

**플레이어** — HP 100 / 이동속도 5 / 픽업 반경 1.5 / 피격 무적 0.5초 / 시작 무기 불릿 Lv1

| 적 | HP | 이속 | 공격 | EXP / 점수 | 등장 |
|---|---|---|---|---|---|
| 슬라임 | 20 | 2.0 | 접촉 5 | 1 / 10 | 0:00~ |
| 고블린궁수 | 15 | 1.5 | 원거리 8 (사거리 6, 간격 2.0) | 2 / 20 | 1:00~ |
| 오크전사 | 60 | 2.5 | 접촉 15 | 3 / 30 | 3:00~ |

프리팹 1개 + ScriptableObject 3종으로 구현합니다. **현재 슬라임 수치가 하드코딩돼 있습니다.**

**스폰** — 0~1분 초당 4 / 1~3분 초당 8 / 3~5분 초당 14. 동시 활성 400 초과 시 중단

> 활성 상한 400은 측정으로 확인한 값입니다. 최악 프레임이 60fps 기준선을 넘는 지점이
> 약 430마리이고 400은 그 아래입니다. **바꾸지 마세요.**

**레벨** — MAX 10. 레벨 n → n+1 필요 EXP = `100 × n`

**무기** — 불릿만. 최근접 적에게 직선 투사체

| 레벨 | 데미지 | 간격 | 발수 |
|---|---|---|---|
| Lv1 | 10 | 0.80초 | 1 |
| Lv2 | 14 | 0.60초 | 1 |
| Lv3 | 18 | 0.50초 | 2 (10도 분산) |

**스탯 카드** (각 3중첩) — 이동속도 +15% / 발사간격 -12% / 픽업반경 +30% / 최대체력 +20
레벨업 시 3장 제시, 1장 선택. 선택 중 `Time.timeScale = 0`

**스탯은 항상 기본값에서 다시 계산합니다.** 현재값에 곱해나가면 3중첩이 1.52배가 되어
"+15% 3중첩"과 어긋납니다.

---

## 12. 범위 밖 (제안 금지)

보스 · 캐릭터 2종 · 세이브/로드 · 모바일 빌드 · 맵 추가 · 무기 진화 · 스토리 ·
`DamageText` · 코드 패키지 도입
