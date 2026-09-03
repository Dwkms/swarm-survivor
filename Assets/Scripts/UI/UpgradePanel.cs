using UnityEngine;
using UnityEngine.UI;

public class UpgradePanel : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private LevelSystem levelSystem;
    [SerializeField] private UpgradeManager upgradeManager;

    [Header("UI")]
    // 패널 본체. 이 스크립트는 Canvas에 붙이고 panelRoot만 켜고 끈다.
    // 스크립트를 panelRoot에 붙이면, 꺼져 있는 동안 이벤트를 받지 못한다.
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button[] cardButtons = new Button[3];
    [SerializeField] private Text[] cardTexts = new Text[3];

    // 지금 화면에 띄운 카드들.
    private readonly UpgradeOption[] shown = new UpgradeOption[3];

    private void OnEnable()
    {
        if (levelSystem != null) levelSystem.OnLevelUp += HandleLevelUp;
    }

    private void OnDisable()
    {
        if (levelSystem != null) levelSystem.OnLevelUp -= HandleLevelUp;
    }

    private void Start()
    {
        // 클릭 연결을 코드에서 하는 이유:
        // Inspector의 OnClick에 등록하면 어느 버튼이 몇 번인지 눈으로 추적해야 하고,
        // 버튼을 복제할 때 잘못된 인덱스가 딸려간다.
        for (int i = 0; i < cardButtons.Length; i++)
        {
            int index = i;   // 람다가 루프 변수 i를 그대로 캡처하지 않도록 복사한다
            cardButtons[i].onClick.AddListener(() => OnCardClicked(index));
        }

        panelRoot.SetActive(false);
    }

    private void HandleLevelUp(int newLevel)
    {
        int count = upgradeManager.PickCandidates(shown);

        if (count == 0)
        {
            // 모든 카드가 3중첩을 채웠다. 고를 것이 없으니 그냥 넘어간다.
            levelSystem.CompleteLevelUp();
            return;
        }

        for (int i = 0; i < cardButtons.Length; i++)
        {
            bool active = i < count;
            cardButtons[i].gameObject.SetActive(active);

            if (active)
            {
                cardTexts[i].text =
                    $"{shown[i].title}\n\n{shown[i].description}\n\n({shown[i].currentStack}/{shown[i].maxStack})";
            }
        }

        panelRoot.SetActive(true);

        // 고르는 동안 게임을 멈춘다.
        // timeScale = 0이면 Time.deltaTime이 0이 되고 FixedUpdate가 돌지 않아
        // 적도 총알도 멈춘다. UI 입력은 timeScale과 무관하게 동작한다.
        Time.timeScale = 0f;
    }

    private void OnCardClicked(int index)
    {
        upgradeManager.Apply(shown[index]);

        panelRoot.SetActive(false);
        Time.timeScale = 1f;

        // 대기 중인 레벨업이 남아 있으면 여기서 다음 카드가 바로 뜬다.
        // 그때 HandleLevelUp이 timeScale을 다시 0으로 만든다.
        levelSystem.CompleteLevelUp();
    }
}