using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeSummaryUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UpgradeManager upgradeManager;
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Text summaryText;

    [Header("Input")]
    [SerializeField] private KeyCode toggleKey = KeyCode.U;

    // 패널을 열 때만 채우는 임시 목록이다. 선택 이력은 저장하지 않는다.
    private readonly List<UpgradeOption> appliedUpgrades = new List<UpgradeOption>();
    private readonly StringBuilder summaryBuilder = new StringBuilder();
    private bool isInitialized;

    private void Start()
    {
        isInitialized = ValidateReferences();

        // 이 스크립트는 항상 Active인 Canvas에 붙고, 표시 대상만 따로 끈다.
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    private void Update()
    {
        if (!isInitialized) return;

        // Pause, Upgrade 선택, Result는 모두 timeScale 0 또는 게임 종료 상태다.
        // Summary가 먼저 열려 있었어도 다른 UI와 겹치지 않게 즉시 닫는다.
        if (panelRoot.activeSelf &&
            (Input.GetKeyDown(KeyCode.Escape) || Time.timeScale == 0f || !IsGamePlaying()))
        {
            panelRoot.SetActive(false);
            return;
        }

        if (!Input.GetKeyDown(toggleKey)) return;

        if (panelRoot.activeSelf)
        {
            panelRoot.SetActive(false);
            return;
        }

        // 이 Overlay는 게임 플레이 중에만 열 수 있으며, Pause 기능이 아니다.
        if (Time.timeScale == 0f || !IsGamePlaying()) return;

        RefreshSummary();
        panelRoot.SetActive(true);
    }

    private void RefreshSummary()
    {
        upgradeManager.GetAppliedUpgrades(appliedUpgrades);

        if (appliedUpgrades.Count == 0)
        {
            summaryText.text = "아직 획득한 업그레이드가 없습니다.";
            return;
        }

        summaryBuilder.Clear();

        for (int i = 0; i < appliedUpgrades.Count; i++)
        {
            UpgradeOption option = appliedUpgrades[i];
            if (i > 0)
            {
                summaryBuilder.Append('\n');
            }

            if (option.maxStack > 1)
            {
                summaryBuilder.Append(option.title).Append(" x").Append(option.currentStack);
            }
            else
            {
                summaryBuilder.Append(option.title).Append(" 획득");
            }
        }

        summaryText.text = summaryBuilder.ToString();
    }

    private static bool IsGamePlaying()
    {
        return GameManager.Instance == null || GameManager.Instance.IsPlaying;
    }

    private bool ValidateReferences()
    {
        bool isValid = true;

        if (upgradeManager == null)
        {
            Debug.LogError("[UpgradeSummaryUI] UpgradeManager 참조가 비어 있습니다.", this);
            isValid = false;
        }

        if (panelRoot == null)
        {
            Debug.LogError("[UpgradeSummaryUI] Panel Root 참조가 비어 있습니다.", this);
            isValid = false;
        }

        if (summaryText == null)
        {
            Debug.LogError("[UpgradeSummaryUI] Summary Text 참조가 비어 있습니다.", this);
            isValid = false;
        }

        return isValid;
    }
}
