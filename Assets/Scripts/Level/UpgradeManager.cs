using System.Collections.Generic;
using UnityEngine;

public enum UpgradeType
{
    MoveSpeed,      // 이동속도 +15%
    FireInterval,   // 발사간격 -12%
    PickupRadius,   // 픽업반경 +30%
    MaxHealth,      // 최대체력 +20
    RadialShot,     // 8방향 방사형 사격 해금
    BasicWeaponDamage,
    RadialWeaponDamage
}

// Inspector에서 값을 채우기 위한 데이터 묶음.
// ScriptableObject로 빼는 것은 나중에. 지금은 카드가 4장 고정이다.
[System.Serializable]
public class UpgradeOption
{
    public UpgradeType type;
    public string title;
    public string description;
    public int maxStack = 3;

    // 런타임 상태라 Inspector에 노출하지 않는다.
    [HideInInspector] public int currentStack;
}

public class UpgradeManager : MonoBehaviour
{
    [SerializeField] private List<UpgradeOption> options = new List<UpgradeOption>();

    [Header("참조 (같은 오브젝트에서 자동으로 찾는다)")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private BulletWeapon bulletWeapon;
    [SerializeField] private ExpCollector expCollector;
    [SerializeField] private RadialWeapon radialWeapon;

    // 기본값. 업그레이드는 항상 여기서부터 다시 계산한다.
    // 현재값에 곱해나가면 1.15^3 = 1.52가 되어 "+15% 3중첩"과 어긋난다.
    private float baseMoveSpeed;
    private float baseFireInterval;
    private float basePickupRadius;
    private int baseMaxHealth;

    // 후보를 담는 임시 목록. 매번 new 하지 않으려고 필드로 둔다.
    private readonly List<UpgradeOption> available = new List<UpgradeOption>();

    private void Awake()
    {
        if (playerController == null) playerController = GetComponent<PlayerController>();
        if (playerStats == null) playerStats = GetComponent<PlayerStats>();
        if (bulletWeapon == null) bulletWeapon = GetComponent<BulletWeapon>();
        if (expCollector == null) expCollector = GetComponent<ExpCollector>();
        if (radialWeapon == null) radialWeapon = GetComponent<RadialWeapon>();

        baseMoveSpeed = playerController.MoveSpeed;
        baseFireInterval = bulletWeapon.FireInterval;
        basePickupRadius = expCollector.PickupRadius;
        baseMaxHealth = playerStats.MaxHealth;
    }

    private void Start()
    {
        if (options.Count == 0)
        {
            Debug.LogError("[UpgradeManager] 업그레이드 목록이 비어 있다. Inspector에서 카드를 채워라.", this);
        }

        if (radialWeapon == null)
        {
            Debug.LogError("[UpgradeManager] RadialWeapon을 찾지 못했습니다. Player의 RadialWeapon 참조를 확인하세요.", this);
        }
    }

    // 3중첩을 채우지 않은 카드 중에서 중복 없이 뽑아 buffer에 담고 개수를 돌려준다.
    public int PickCandidates(UpgradeOption[] buffer)
    {
        available.Clear();

        for (int i = 0; i < options.Count; i++)
        {
            if (CanOffer(options[i]))
            {
                available.Add(options[i]);
            }
        }

        // 남은 후보가 3장보다 적을 수 있다. 있는 만큼만 준다.
        int count = Mathf.Min(buffer.Length, available.Count);

        for (int i = 0; i < count; i++)
        {
            // 뽑은 것을 목록에서 빼서 같은 카드가 두 번 나오지 않게 한다.
            int r = Random.Range(0, available.Count);
            buffer[i] = available[r];
            available.RemoveAt(r);
        }

        return count;
    }

    // Inspector 옵션 순서대로 현재 적용된 강화만 복사한다.
    // 선택 이력은 따로 저장하지 않고, 실제 stack 상태만 UI에 제공한다.
    public void GetAppliedUpgrades(List<UpgradeOption> buffer)
    {
        if (buffer == null)
        {
            Debug.LogError("[UpgradeManager] 적용된 업그레이드를 받을 buffer가 비어 있습니다.", this);
            return;
        }

        buffer.Clear();

        for (int i = 0; i < options.Count; i++)
        {
            if (options[i].currentStack > 0)
            {
                buffer.Add(options[i]);
            }
        }
    }

    public void Apply(UpgradeOption option)
    {
        if (option == null) return;
        if (!CanOffer(option)) return;

        option.currentStack++;
        ApplyStat(option.type, option.currentStack);

        // 유효한 카드가 실제로 적용된 뒤에만 선택음을 재생한다.
        GameAudio.PlayUpgradeSelect();

        Debug.Log($"업그레이드: {option.title}  ({option.currentStack}/{option.maxStack})");
    }

    private void ApplyStat(UpgradeType type, int stack)
    {
        switch (type)
        {
            case UpgradeType.MoveSpeed:
                playerController.SetMoveSpeed(baseMoveSpeed * (1f + 0.15f * stack));
                break;

            case UpgradeType.FireInterval:
                // 발사간격은 줄어야 강해진다.
                bulletWeapon.SetFireInterval(baseFireInterval * (1f - 0.12f * stack));
                break;

            case UpgradeType.PickupRadius:
                expCollector.SetPickupRadius(basePickupRadius * (1f + 0.30f * stack));
                break;

            case UpgradeType.MaxHealth:
                playerStats.SetMaxHealth(baseMaxHealth + 20 * stack);
                break;

            case UpgradeType.RadialShot:
                radialWeapon.Unlock();
                break;

            case UpgradeType.BasicWeaponDamage:
                bulletWeapon.SetDamageMultiplier(1f + 0.20f * stack);
                break;

            case UpgradeType.RadialWeaponDamage:
                radialWeapon.SetDamageMultiplier(1f + 0.20f * stack);
                break;
        }
    }

    private bool CanOffer(UpgradeOption option)
    {
        if (option.type == UpgradeType.RadialShot)
        {
            // 1회 획득형 무기는 실제 해금 상태를 후보 조건으로 사용한다.
            return radialWeapon != null && !radialWeapon.IsUnlocked;
        }

        if (option.type == UpgradeType.RadialWeaponDamage)
        {
            // 방사형 무기를 얻기 전에는 강화 카드만 먼저 나올 수 없다.
            return radialWeapon != null && radialWeapon.IsUnlocked && option.currentStack < option.maxStack;
        }

        return option.currentStack < option.maxStack;
    }
}
