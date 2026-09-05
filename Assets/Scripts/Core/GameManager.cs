using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("게임 규칙")]
    [SerializeField] private float gameDuration = 300f;   // 5분

    [Header("참조")]
    [SerializeField] private PlayerStats playerStats;

    public float ElapsedTime { get; private set; }
    public float RemainingTime => Mathf.Max(0f, gameDuration - ElapsedTime);
    public bool IsPlaying { get; private set; }

    // true = 승리(5분 생존), false = 패배(체력 0)
    public event Action<bool> OnGameEnd;

    private void Awake()
    {
        Instance = this;

        if (playerStats == null)
        {
            playerStats = FindAnyObjectByType<PlayerStats>();
        }
    }

    private void OnEnable()
    {
        if (playerStats != null)
        {
            playerStats.OnDied += HandlePlayerDied;
        }
    }

    private void OnDisable()
    {
        if (playerStats != null)
        {
            playerStats.OnDied -= HandlePlayerDied;
        }
    }

    private void Start()
    {
        if (playerStats == null)
        {
            Debug.LogError("[GameManager] PlayerStats를 찾지 못했다.", this);
        }

        // 이전 판에서 0으로 두고 끝났을 수 있으므로 명시적으로 되돌린다.
        Time.timeScale = 1f;
        IsPlaying = true;
    }

    private void Update()
    {
        if (!IsPlaying) return;

        // unscaledDeltaTime이 아니라 deltaTime을 쓴다.
        // 업그레이드 카드를 고르는 동안 timeScale이 0이므로 게임 시간이 멈춰야 한다.
        // 카드를 오래 들여다본 사람이 손해를 보면 안 된다.
        ElapsedTime += Time.deltaTime;

        if (ElapsedTime >= gameDuration)
        {
            EndGame(true);
        }
    }

    private void HandlePlayerDied()
    {
        EndGame(false);
    }

    private void EndGame(bool victory)
    {
        if (!IsPlaying) return;   // 승리와 패배가 같은 프레임에 겹치는 경우 방지

        IsPlaying = false;
        Time.timeScale = 0f;

        Debug.Log(victory ? "승리 ? 5분 생존" : $"패배 ? {ElapsedTime:F1}초 생존");

        OnGameEnd?.Invoke(victory);
    }

    // HUD가 쓸 형식. "M:SS"
    public string GetRemainingTimeText()
    {
        int total = Mathf.CeilToInt(RemainingTime);
        return $"{total / 60}:{total % 60:00}";
    }
}