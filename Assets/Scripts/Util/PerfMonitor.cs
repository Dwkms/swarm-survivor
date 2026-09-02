using System;
using UnityEngine;

public class PerfMonitor : MonoBehaviour
{
    [Header("표시 설정")]
    [SerializeField] private float refreshInterval = 0.5f;  // 문자열 갱신 주기
    [SerializeField] private int fontSize = 18;

    [Header("키 설정")]
    [SerializeField] private KeyCode burstKey = KeyCode.F1;  // 스포너의 대량 스폰 키
    [SerializeField] private KeyCode toggleKey = KeyCode.F3;  // 표시 on/off
    [SerializeField] private KeyCode resetKey = KeyCode.F4;  // 최악 프레임 리셋

    // 화면에 그릴 문자열. 0.5초에 한 번만 새로 만든다.
    // 매 프레임 만들면 측정 도구 자신이 GC를 만들게 된다.
    private string cachedText = "";
    private float refreshTimer;

    // 구간 평균용 누적
    private float accumulatedMs;
    private int frameCount;

    // 리셋 이후 최악 프레임. 구간마다 지우지 않는다.
    private float worstMs;

    private float elapsed;

    // 이번 구간에 한 번이라도 움직였는가.
    // 0.5초에 한 번만 입력을 읽으면 그 순간에 키를 안 누르고 있을 수 있어 놓친다.
    private bool movedThisInterval;

    // F4 리셋 이후 구간 전체의 평균.
    // 화면의 0.5초 평균은 순간값이라 눈으로 읽는 시점에 따라 값이 달라진다.
    // 측정용으로 쓸 수 있는 건 이쪽이다.
    private float sessionMs;
    private int sessionFrames;

    // F1 버스트 측정
    private bool burstPending;
    private float lastBurstMs = -1f;
    private long lastBurstAllocBytes;
    private long prevManagedBytes;

    private bool visible = true;

    private GUIStyle style;
    private Texture2D backgroundTex;

    private void Awake()
    {
        // 배경용 1x1 반투명 검은 텍스처. Rect 크기로 늘려 그린다.
        // 게임 화면이 밝을 때 흰 글씨가 안 보이는 것을 막는다.
        backgroundTex = new Texture2D(1, 1);
        backgroundTex.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.65f));
        backgroundTex.Apply();

        prevManagedBytes = GC.GetTotalMemory(false);
    }

    private void OnDestroy()
    {
        // 코드로 만든 텍스처는 직접 해제해야 한다. 안 하면 에디터가 누수 경고를 낸다.
        if (backgroundTex != null)
        {
            Destroy(backgroundTex);
        }
    }

    private void Update()
    {
        // ─────────────────────────────────────────────────────────
        // 1. 직전 프레임이 F1 버스트 프레임이었다면 여기서 기록한다.
        //
        //    Time.unscaledDeltaTime은 "직전 프레임이 걸린 시간"이다.
        //    F1을 누른 그 프레임 안에서 100마리가 생성되므로,
        //    그 비용은 다음 프레임에 와야 읽을 수 있다.
        //    이 한 값이 풀링 전후 비교의 핵심 지표다.
        // ─────────────────────────────────────────────────────────
        long managedNow = GC.GetTotalMemory(false);

        if (burstPending)
        {
            lastBurstMs = Time.unscaledDeltaTime * 1000f;
            lastBurstAllocBytes = managedNow - prevManagedBytes;
            burstPending = false;
        }

        prevManagedBytes = managedNow;

        // ── 2. 입력 ──
        if (Input.GetKeyDown(burstKey))
        {
            // 이번 프레임에 스포너가 100마리를 만든다. 다음 프레임에 비용을 읽는다.
            burstPending = true;
        }

        if (Input.GetKeyDown(toggleKey))
        {
            visible = !visible;
        }

        if (Input.GetKeyDown(resetKey))
        {
            worstMs = 0f;
            lastBurstMs = -1f;
            sessionMs = 0f;        // 추가
            sessionFrames = 0;     // 추가
        }

        // ── 3. 프레임 시간 누적 ──
        // unscaledDeltaTime을 쓰는 이유:
        // timeScale = 0인 업그레이드 화면에서도 성능은 계속 측정돼야 한다.
        float ms = Time.unscaledDeltaTime * 1000f;

        accumulatedMs += ms;
        sessionMs += ms;
        sessionFrames++;
        frameCount++;
        elapsed += Time.unscaledDeltaTime;

        if (ms > worstMs)
        {
            worstMs = ms;
        }

        // 이동 여부는 매 프레임 확인해 구간 안에 한 번이라도 있었는지 기록한다.
        if (Input.GetAxisRaw("Horizontal") != 0f || Input.GetAxisRaw("Vertical") != 0f)
        {
            movedThisInterval = true;
        }

        // ── 4. 0.5초에 한 번만 문자열을 만든다 ──
        refreshTimer += Time.unscaledDeltaTime;
        if (refreshTimer >= refreshInterval)
        {
            RebuildText();

            refreshTimer = 0f;
            accumulatedMs = 0f;
            frameCount = 0;
            movedThisInterval = false;
        }
    }

    private void RebuildText()
    {
        float avgMs = frameCount > 0 ? accumulatedMs / frameCount : 0f;
        float fps = avgMs > 0f ? 1000f / avgMs : 0f;

        // 구간 평균. 측정에 쓰는 값은 이것이다.
        float sessionAvg = sessionFrames > 0 ? sessionMs / sessionFrames : 0f;

        string state = movedThisInterval ? "MOVING" : "IDLE";

        string burst = lastBurstMs < 0f
            ? "-"
            : $"{lastBurstMs:F2} ms  /  GC {lastBurstAllocBytes / 1024f:F0} KB";

        cachedText =
            $"now     {avgMs:F2} ms   ({fps:F0} fps)   [{state}]\n" +
            $"AVG     {sessionAvg:F2} ms   ({sessionFrames} frames)\n" +
            $"worst   {worstMs:F2} ms\n" +
            $"enemies {EnemySpawner.ActiveEnemyCount}\n" +
            $"burst   {burst}\n" +
            $"elapsed {elapsed:F1} s     (F3 숨김 / F4 리셋)";
    }

    private void OnGUI()
    {
        if (!visible) return;

        // OnGUI는 한 프레임에 Layout·Repaint 등으로 여러 번 호출된다.
        // 걸러내지 않으면 같은 그리기를 반복한다.
        if (Event.current.type != EventType.Repaint) return;

        // GUIStyle은 OnGUI 안에서 만들어야 안전하다. 한 번만 만들고 캐싱한다.
        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                alignment = TextAnchor.UpperLeft
            };
            style.normal.textColor = Color.white;
        }

        Rect box = new Rect(10f, 10f, 400f, 170f);

        GUI.DrawTexture(box, backgroundTex);
        GUI.Label(new Rect(box.x + 10f, box.y + 8f, box.width - 20f, box.height - 16f),
                  cachedText, style);
    }
}