using UnityEngine;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    [Header("패널")]
    [SerializeField] private GameObject pauseMenuRoot;
    [SerializeField] private GameObject settingsPanelRoot;
    [SerializeField] private GameObject upgradePanelRoot;
    [SerializeField] private GameObject resultPanelRoot;

    [Header("버튼")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button backButton;
    [SerializeField] private Button quitButton;

    [Header("디스플레이 설정")]
    [SerializeField] private DisplaySettings displaySettings;

    private bool isInitialized;

    private void Start()
    {
        if (!ValidateReferences()) return;

        resumeButton.onClick.AddListener(Resume);
        settingsButton.onClick.AddListener(OpenSettings);
        backButton.onClick.AddListener(BackToPause);
        quitButton.onClick.AddListener(QuitGame);

        pauseMenuRoot.SetActive(false);
        settingsPanelRoot.SetActive(false);
        isInitialized = true;
    }

    private void Update()
    {
        if (!isInitialized || !Input.GetKeyDown(KeyCode.Escape)) return;

        // 게임 종료와 업그레이드 선택은 자기 UI 흐름을 우선한다.
        if (resultPanelRoot.activeSelf || upgradePanelRoot.activeSelf) return;

        if (settingsPanelRoot.activeSelf)
        {
            BackToPause();
        }
        else if (pauseMenuRoot.activeSelf)
        {
            Resume();
        }
        else
        {
            OpenPause();
        }
    }

    public void OpenPause()
    {
        settingsPanelRoot.SetActive(false);
        pauseMenuRoot.SetActive(true);
        Time.timeScale = 0f;
    }

    public void Resume()
    {
        pauseMenuRoot.SetActive(false);
        settingsPanelRoot.SetActive(false);
        Time.timeScale = 1f;
    }

    public void OpenSettings()
    {
        displaySettings.RefreshCurrentSettings();
        pauseMenuRoot.SetActive(false);
        settingsPanelRoot.SetActive(true);
        Time.timeScale = 0f;
    }

    public void BackToPause()
    {
        settingsPanelRoot.SetActive(false);
        pauseMenuRoot.SetActive(true);
        Time.timeScale = 0f;
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private bool ValidateReferences()
    {
        bool isValid = true;

        if (pauseMenuRoot == null)
        {
            Debug.LogError("PauseMenu: Pause Menu Root is not assigned in the Inspector.", this);
            isValid = false;
        }

        if (settingsPanelRoot == null)
        {
            Debug.LogError("PauseMenu: Settings Panel Root is not assigned in the Inspector.", this);
            isValid = false;
        }

        if (upgradePanelRoot == null)
        {
            Debug.LogError("PauseMenu: Upgrade Panel Root is not assigned in the Inspector.", this);
            isValid = false;
        }

        if (resultPanelRoot == null)
        {
            Debug.LogError("PauseMenu: Result Panel Root is not assigned in the Inspector.", this);
            isValid = false;
        }

        if (resumeButton == null)
        {
            Debug.LogError("PauseMenu: Resume Button is not assigned in the Inspector.", this);
            isValid = false;
        }

        if (settingsButton == null)
        {
            Debug.LogError("PauseMenu: Settings Button is not assigned in the Inspector.", this);
            isValid = false;
        }

        if (backButton == null)
        {
            Debug.LogError("PauseMenu: Back Button is not assigned in the Inspector.", this);
            isValid = false;
        }

        if (quitButton == null)
        {
            Debug.LogError("PauseMenu: Quit Button is not assigned in the Inspector.", this);
            isValid = false;
        }

        if (displaySettings == null)
        {
            Debug.LogError("PauseMenu: Display Settings is not assigned in the Inspector.", this);
            isValid = false;
        }

        return isValid;
    }
}
