using System;
using UnityEngine;

public class LevelSystem : MonoBehaviour
{
    [Header("레벨 설정")]
    [SerializeField] private int maxLevel = 10;

    // 레벨 n → n+1 에 필요한 EXP = expPerLevelBase * n
    // Inspector에 열어둔 이유는 테스트할 때 잠시 낮춰보기 위해서다.
    [SerializeField] private int expPerLevelBase = 100;

    [Header("참조")]
    [SerializeField] private ExpCollector expCollector;

    public int CurrentLevel { get; private set; } = 1;
    public int CurrentExp { get; private set; }

    // 다음 레벨까지 필요한 양. MAX면 의미가 없으므로 0을 돌려준다.
    public int RequiredExp => IsMaxLevel ? 0 : expPerLevelBase * CurrentLevel;
    public bool IsMaxLevel => CurrentLevel >= maxLevel;

    // 레벨업 1회당 한 번 발생한다. 3단계에서 UpgradePanel이 구독한다.
    public event Action<int> OnLevelUp;

    // EXP가 바뀔 때마다. 나중에 HUD가 구독한다.
    public event Action OnExpChanged;

    // 한 프레임에 여러 번 레벨업할 수 있다.
    // 젬을 한꺼번에 주우면 EXP가 몰려 들어와 두세 레벨이 같이 오른다.
    // 카드 선택은 하나씩 해야 하므로 대기 수를 세어두고 순서대로 처리한다.
    private int pendingLevelUps;
    private bool isProcessingLevelUp;

    private void Awake()
    {
        // 같은 오브젝트에 붙어 있으면 Inspector에 안 넣어도 찾아준다.
        if (expCollector == null)
        {
            expCollector = GetComponent<ExpCollector>();
        }
    }

    private void OnEnable()
    {
        if (expCollector != null)
        {
            expCollector.OnExpCollected += HandleExpCollected;
        }
    }

    private void OnDisable()
    {
        // 구독 해제를 빠뜨리면 이 오브젝트가 사라진 뒤에도 이벤트가
        // 죽은 참조를 붙들고 있게 된다. 등록한 곳에서 반드시 짝을 맞춘다.
        if (expCollector != null)
        {
            expCollector.OnExpCollected -= HandleExpCollected;
        }
    }

    private void Start()
    {
        if (expCollector == null)
        {
            Debug.LogError("[LevelSystem] ExpCollector를 찾지 못했다. 같은 오브젝트에 붙였는지 확인해라.", this);
        }
    }

    private void HandleExpCollected(int amount)
    {
        if (IsMaxLevel) return;

        CurrentExp += amount;

        // while인 이유: 한 번에 여러 레벨을 넘길 수 있다.
        // RequiredExp는 CurrentLevel에 따라 달라지므로, 빼고 나서 레벨을 올린다.
        while (!IsMaxLevel && CurrentExp >= RequiredExp)
        {
            CurrentExp -= RequiredExp;
            CurrentLevel++;
            pendingLevelUps++;
        }

        if (IsMaxLevel)
        {
            CurrentExp = 0;
        }

        OnExpChanged?.Invoke();

        TryProcessNextLevelUp();
    }

    private void TryProcessNextLevelUp()
    {
        // 이미 카드 선택이 진행 중이면 기다린다.
        if (isProcessingLevelUp) return;
        if (pendingLevelUps <= 0) return;

        pendingLevelUps--;
        isProcessingLevelUp = true;

        Debug.Log($"레벨업 → Lv{CurrentLevel}   (대기 {pendingLevelUps})");

        OnLevelUp?.Invoke(CurrentLevel);

        // 아직 UpgradePanel이 없어 아무도 구독하지 않으면 여기서 멈춰버린다.
        // 구독자가 없을 때만 스스로 완료 처리한다.
        // 3단계에서 UI가 구독을 시작하면 이 분기는 지나가지 않는다.
        if (OnLevelUp == null)
        {
            CompleteLevelUp();
        }
    }

    // UpgradePanel이 카드 선택을 끝내면 호출한다.
    public void CompleteLevelUp()
    {
        if (!isProcessingLevelUp) return;

        isProcessingLevelUp = false;

        // 대기 중인 레벨업이 더 있으면 이어서 처리한다.
        TryProcessNextLevelUp();
    }
}