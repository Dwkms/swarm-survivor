using UnityEngine;
using UnityEngine.UI;

public class AudioSettingsUI : MonoBehaviour
{
    private const float PreviewInterval = 0.2f;

    [SerializeField] private GameAudio gameAudio;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

    private bool isInitialized;
    private float lastPreviewTime = -PreviewInterval;

    private void Start()
    {
        if (!ValidateReferences()) return;

        bgmSlider.onValueChanged.AddListener(HandleBgmVolumeChanged);
        sfxSlider.onValueChanged.AddListener(HandleSfxVolumeChanged);

        isInitialized = true;
        SyncSliders();
    }

    private void OnEnable()
    {
        if (!isInitialized) return;

        SyncSliders();
    }

    private void HandleBgmVolumeChanged(float value)
    {
        gameAudio.SetBgmVolume(value);
    }

    private void HandleSfxVolumeChanged(float value)
    {
        gameAudio.SetSfxVolume(value);

        if (Time.unscaledTime - lastPreviewTime < PreviewInterval) return;

        lastPreviewTime = Time.unscaledTime;
        gameAudio.PlaySfxPreview();
    }

    private void SyncSliders()
    {
        bgmSlider.SetValueWithoutNotify(gameAudio.BgmVolume);
        sfxSlider.SetValueWithoutNotify(gameAudio.SfxVolume);
    }

    private bool ValidateReferences()
    {
        bool isValid = true;

        if (gameAudio == null)
        {
            Debug.LogError("[AudioSettingsUI] GameAudio 참조가 비어 있습니다.", this);
            isValid = false;
        }

        if (bgmSlider == null)
        {
            Debug.LogError("[AudioSettingsUI] BGM Slider 참조가 비어 있습니다.", this);
            isValid = false;
        }

        if (sfxSlider == null)
        {
            Debug.LogError("[AudioSettingsUI] SFX Slider 참조가 비어 있습니다.", this);
            isValid = false;
        }

        return isValid;
    }
}
