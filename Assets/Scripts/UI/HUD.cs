using UnityEngine;
using UnityEngine.UI;

public class HUD : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private LevelSystem levelSystem;
    [SerializeField] private GameManager gameManager;

    [Header("체력")]
    [SerializeField] private Image healthFill;
    [SerializeField] private Text healthText;

    [Header("경험치")]
    [SerializeField] private Image expFill;
    [SerializeField] private Text levelText;

    [Header("정보")]
    [SerializeField] private Text timeText;
    [SerializeField] private Text killText;

    private void Awake()
    {
        if (playerStats == null) playerStats = FindAnyObjectByType<PlayerStats>();
        if (levelSystem == null) levelSystem = FindAnyObjectByType<LevelSystem>();
        if (gameManager == null) gameManager = FindAnyObjectByType<GameManager>();
    }

    private void Update()
    {
        UpdateHealth();
        UpdateExp();
        UpdateInfo();
    }

    private void UpdateHealth()
    {
        if (playerStats == null) return;

        // 0으로 나누는 것을 막는다. 최대 체력이 0인 상황은 없어야 하지만
        // Inspector 값 하나로 게임이 죽는 것보다 방어하는 편이 낫다.
        float ratio = playerStats.MaxHealth > 0
            ? (float)playerStats.CurrentHealth / playerStats.MaxHealth
            : 0f;

        if (healthFill != null) healthFill.fillAmount = ratio;
        if (healthText != null) healthText.text = $"{playerStats.CurrentHealth} / {playerStats.MaxHealth}";
    }

    private void UpdateExp()
    {
        if (levelSystem == null) return;

        // MAX 레벨이면 RequiredExp가 0이다. 그때는 게이지를 가득 채운다.
        float ratio = levelSystem.IsMaxLevel
            ? 1f
            : (levelSystem.RequiredExp > 0 ? (float)levelSystem.CurrentExp / levelSystem.RequiredExp : 0f);

        if (expFill != null) expFill.fillAmount = ratio;
        if (levelText != null)
        {
            levelText.text = levelSystem.IsMaxLevel
                ? $"Lv {levelSystem.CurrentLevel}  MAX"
                : $"Lv {levelSystem.CurrentLevel}  {levelSystem.CurrentExp}/{levelSystem.RequiredExp}";
        }
    }

    private void UpdateInfo()
    {
        if (gameManager == null) return;

        if (timeText != null) timeText.text = gameManager.GetRemainingTimeText();
        if (killText != null) killText.text = $"처치 {gameManager.KillCount}";
    }
}