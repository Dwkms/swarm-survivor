using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DisplaySettings : MonoBehaviour
{
    [Header("버튼")]
    [SerializeField] private Button applyButton;

    [Header("드롭다운")]
    [SerializeField] private Dropdown displayModeDropdown;
    [SerializeField] private Dropdown resolutionDropdown;

    private readonly List<Vector2Int> resolutions = new List<Vector2Int>();
    private void Start()
    {
        if (!ValidateReferences()) return;

        SetupDisplayModeDropdown();
        BuildResolutionList();
        applyButton.onClick.AddListener(Apply);
        RefreshCurrentSettings();
    }

    public void Apply()
    {
        Vector2Int resolution = resolutions[resolutionDropdown.value];
        FullScreenMode fullScreenMode = displayModeDropdown.value == 0
            ? FullScreenMode.Windowed
            : FullScreenMode.FullScreenWindow;

        Screen.SetResolution(resolution.x, resolution.y, fullScreenMode);
    }

    private bool ValidateReferences()
    {
        bool isValid = true;

        if (applyButton == null)
        {
            Debug.LogError("DisplaySettings: Apply Button is not assigned in the Inspector.", this);
            isValid = false;
        }

        if (displayModeDropdown == null)
        {
            Debug.LogError("DisplaySettings: Display Mode Dropdown is not assigned in the Inspector.", this);
            isValid = false;
        }

        if (resolutionDropdown == null)
        {
            Debug.LogError("DisplaySettings: Resolution Dropdown is not assigned in the Inspector.", this);
            isValid = false;
        }

        return isValid;
    }

    private void SetupDisplayModeDropdown()
    {
        displayModeDropdown.ClearOptions();
        displayModeDropdown.AddOptions(new List<string>
        {
            "창 모드",
            "전체 화면"
        });
    }

    private void BuildResolutionList()
    {
        resolutions.Clear();

        foreach (Resolution resolution in Screen.resolutions)
        {
            Vector2Int size = new Vector2Int(resolution.width, resolution.height);
            if (!resolutions.Contains(size))
            {
                // 주사율 차이는 이번 설정 범위가 아니므로 가로·세로만 남긴다.
                resolutions.Add(size);
            }
        }

        Vector2Int currentSize = new Vector2Int(Screen.width, Screen.height);
        if (!resolutions.Contains(currentSize))
        {
            resolutions.Add(currentSize);
        }

        resolutions.Sort((left, right) =>
        {
            int widthComparison = right.x.CompareTo(left.x);
            return widthComparison != 0
                ? widthComparison
                : right.y.CompareTo(left.y);
        });

        List<string> options = new List<string>();
        foreach (Vector2Int resolution in resolutions)
        {
            options.Add($"{resolution.x} × {resolution.y}");
        }

        resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(options);
    }

    public void RefreshCurrentSettings()
    {
        displayModeDropdown.value = Screen.fullScreenMode == FullScreenMode.Windowed ? 0 : 1;
        displayModeDropdown.RefreshShownValue();

        Vector2Int currentSize = new Vector2Int(Screen.width, Screen.height);
        for (int i = 0; i < resolutions.Count; i++)
        {
            if (resolutions[i] != currentSize) continue;

            resolutionDropdown.value = i;
            resolutionDropdown.RefreshShownValue();
            return;
        }
    }
}
