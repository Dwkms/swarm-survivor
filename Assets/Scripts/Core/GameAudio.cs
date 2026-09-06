using UnityEngine;

public class GameAudio : MonoBehaviour
{
    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip bgmClip;
    [SerializeField] private AudioClip fireClip;
    [SerializeField] private AudioClip expPickupClip;
    [SerializeField] private AudioClip playerHitClip;
    [SerializeField] private AudioClip winClip;
    [SerializeField] private AudioClip loseClip;

    private static GameAudio instance;
    private static bool warnedMissingInstance;
    private bool isInitialized;

    public float BgmVolume => bgmSource != null ? bgmSource.volume : 0f;
    public float SfxVolume => sfxSource != null ? sfxSource.volume : 0f;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogError("[GameAudio] Scene에는 GameAudio가 하나만 있어야 합니다.", this);
            enabled = false;
            return;
        }

        instance = this;
        isInitialized = ValidateReferences();
        if (!isInitialized) return;

        // 배경음과 효과음은 모두 화면 위치와 무관한 2D 사운드로 재생합니다.
        bgmSource.spatialBlend = 0f;
        sfxSource.spatialBlend = 0f;
        bgmSource.playOnAwake = false;
        sfxSource.playOnAwake = false;

        bgmSource.clip = bgmClip;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    public static void PlayFire()
    {
        if (!TryGetReadyInstance(out GameAudio audio)) return;

        audio.sfxSource.PlayOneShot(audio.fireClip);
    }

    public static void PlayExpPickup()
    {
        if (!TryGetReadyInstance(out GameAudio audio)) return;

        audio.sfxSource.PlayOneShot(audio.expPickupClip);
    }

    public static void PlayPlayerHit()
    {
        if (!TryGetReadyInstance(out GameAudio audio)) return;

        audio.sfxSource.PlayOneShot(audio.playerHitClip);
    }

    public static void PlayGameEnd(bool victory)
    {
        if (!TryGetReadyInstance(out GameAudio audio)) return;

        audio.bgmSource.Stop();
        audio.sfxSource.PlayOneShot(victory ? audio.winClip : audio.loseClip);
    }

    public void SetBgmVolume(float value)
    {
        if (!isInitialized) return;

        bgmSource.volume = Mathf.Clamp01(value);
    }

    public void SetSfxVolume(float value)
    {
        if (!isInitialized) return;

        sfxSource.volume = Mathf.Clamp01(value);
    }

    public void PlaySfxPreview()
    {
        if (!isInitialized) return;

        sfxSource.PlayOneShot(expPickupClip);
    }

    private static bool TryGetReadyInstance(out GameAudio audio)
    {
        audio = instance;
        if (audio != null && audio.isInitialized) return true;

        if (!warnedMissingInstance)
        {
            warnedMissingInstance = true;
            Debug.LogError("[GameAudio] GameAudio가 없거나 초기화에 실패했습니다. Scene의 참조를 확인하세요.");
        }

        return false;
    }

    private bool ValidateReferences()
    {
        bool isValid = true;

        if (bgmSource == null)
        {
            Debug.LogError("[GameAudio] BGM AudioSource가 비어 있습니다.", this);
            isValid = false;
        }

        if (sfxSource == null)
        {
            Debug.LogError("[GameAudio] SFX AudioSource가 비어 있습니다.", this);
            isValid = false;
        }

        if (bgmSource != null && bgmSource == sfxSource)
        {
            Debug.LogError("[GameAudio] BGM과 SFX는 서로 다른 AudioSource를 사용해야 합니다.", this);
            isValid = false;
        }

        if (bgmClip == null)
        {
            Debug.LogError("[GameAudio] BGM Clip이 비어 있습니다.", this);
            isValid = false;
        }

        if (fireClip == null)
        {
            Debug.LogError("[GameAudio] Fire Clip이 비어 있습니다.", this);
            isValid = false;
        }

        if (expPickupClip == null)
        {
            Debug.LogError("[GameAudio] Exp Pickup Clip이 비어 있습니다.", this);
            isValid = false;
        }

        if (playerHitClip == null)
        {
            Debug.LogError("[GameAudio] Player Hit Clip이 비어 있습니다.", this);
            isValid = false;
        }

        if (winClip == null)
        {
            Debug.LogError("[GameAudio] Win Clip이 비어 있습니다.", this);
            isValid = false;
        }

        if (loseClip == null)
        {
            Debug.LogError("[GameAudio] Lose Clip이 비어 있습니다.", this);
            isValid = false;
        }

        return isValid;
    }
}
