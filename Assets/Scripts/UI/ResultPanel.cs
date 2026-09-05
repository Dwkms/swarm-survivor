using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ResultPanel : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private GameManager gameManager;

    [Header("UI")]
    // UpgradePanel과 같은 이유로 이 스크립트는 Canvas에 붙인다.
    // 패널에 붙이면 꺼져 있는 동안 이벤트를 못 받는다.
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Text titleText;
    [SerializeField] private Text detailText;
    [SerializeField] private Button restartButton;

    private void Awake()
    {
        if (gameManager == null)
        {
            gameManager = FindAnyObjectByType<GameManager>();
        }
    }

    private void OnEnable()
    {
        if (gameManager != null) gameManager.OnGameEnd += HandleGameEnd;
    }

    private void OnDisable()
    {
        if (gameManager != null) gameManager.OnGameEnd -= HandleGameEnd;
    }

    private void Start()
    {
        restartButton.onClick.AddListener(Restart);
        panelRoot.SetActive(false);
    }

    private void HandleGameEnd(bool victory)
    {
        titleText.text = victory ? "CLEAR" : "GAME OVER";
        titleText.color = victory ? new Color(0.4f, 0.9f, 0.5f) : new Color(0.9f, 0.4f, 0.4f);

        LevelSystem levelSystem = FindAnyObjectByType<LevelSystem>();
        int level = levelSystem != null ? levelSystem.CurrentLevel : 1;

        detailText.text =
            $"생존 시간   {gameManager.GetElapsedTimeText()}\n" +
            $"도달 레벨   Lv {level}\n" +
            $"처치        {gameManager.KillCount}\n" +
            $"점수        {gameManager.Score}";

        panelRoot.SetActive(true);
    }

    private void Restart()
    {
        // 씬을 다시 불러오기 전에 timeScale을 되돌린다.
        // 0인 채로 로드하면 새 씬이 멈춘 상태로 시작한다.
        Time.timeScale = 1f;

        // static 목록은 씬을 다시 불러와도 살아남는다.
        // 파괴된 오브젝트가 남아 있으면 다음 판에서 null 참조가 섞인다.
        ExpGem.All.Clear();

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}