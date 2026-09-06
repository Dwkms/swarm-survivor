using UnityEngine;

public class RadialWeapon : MonoBehaviour
{
    [Header("Projectile")]
    [SerializeField] private GameObject projectilePrefab;

    [Header("Weapon Settings")]
    [SerializeField] private float fireInterval = 2f;
    [SerializeField] private int projectileCount = 8;
    [SerializeField] private bool startUnlocked = false;

    private float fireAccumulator;
    private bool isUnlocked;
    private bool isConfigured;
    private int projectileBaseDamage;
    private float damageMultiplier = 1f;

    public bool IsUnlocked => isUnlocked;

    private void Awake()
    {
        isUnlocked = startUnlocked;
        isConfigured = ValidateConfiguration();
    }

    private void Update()
    {
        if (!isUnlocked)
        {
            // 잠금 중 경과 시간을 버려 Unlock 직후 밀린 발사가 쏟아지지 않게 한다.
            fireAccumulator = 0f;
            return;
        }

        if (!isConfigured) return;

        // 발사 간격은 게임 진행 시간에 맞춰 Pause 중 함께 멈춘다.
        fireAccumulator += Time.deltaTime;

        while (fireAccumulator >= fireInterval)
        {
            FireBurst();
            fireAccumulator -= fireInterval;
        }
    }

    public void Unlock()
    {
        if (isUnlocked) return;

        isUnlocked = true;
        fireAccumulator = 0f;
    }

    public void SetDamageMultiplier(float value)
    {
        if (value <= 0f)
        {
            Debug.LogError("[RadialWeapon] Damage Multiplier는 0보다 커야 합니다.", this);
            return;
        }

        damageMultiplier = value;
    }

    private void FireBurst()
    {
        float angleStep = 360f / projectileCount;
        int projectileDamage = Mathf.RoundToInt(projectileBaseDamage * damageMultiplier);
        bool launchedProjectile = false;

        for (int i = 0; i < projectileCount; i++)
        {
            float radians = i * angleStep * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));

            GameObject obj = PoolManager.Spawn(projectilePrefab, transform.position, Quaternion.identity);
            Projectile projectile = obj.GetComponent<Projectile>();
            if (projectile == null)
            {
                Debug.LogError("[RadialWeapon] Spawn된 Projectile에 Projectile 컴포넌트가 없습니다.", obj);
                PoolManager.Despawn(obj);
                continue;
            }

            projectile.Launch(direction, projectileDamage);
            launchedProjectile = true;
        }

        // 8발 각각이 아니라, 실제로 성립한 방사형 Burst 하나에만 발사음을 낸다.
        if (launchedProjectile)
        {
            GameAudio.PlayFire();
        }
    }

    private bool ValidateConfiguration()
    {
        bool isValid = true;

        if (projectilePrefab == null)
        {
            Debug.LogError("[RadialWeapon] Projectile Prefab이 비어 있습니다.", this);
            isValid = false;
        }
        else
        {
            Projectile projectile = projectilePrefab.GetComponent<Projectile>();
            if (projectile == null)
            {
                Debug.LogError("[RadialWeapon] Projectile Prefab에 Projectile 컴포넌트가 없습니다.", this);
                isValid = false;
            }
            else
            {
                projectileBaseDamage = projectile.BaseDamage;
                if (projectileBaseDamage < 1)
                {
                    Debug.LogError("[RadialWeapon] Projectile 기본 Damage는 1 이상이어야 합니다.", this);
                    isValid = false;
                }
            }
        }

        if (fireInterval <= 0f)
        {
            Debug.LogError("[RadialWeapon] Fire Interval은 0보다 커야 합니다.", this);
            isValid = false;
        }

        if (projectileCount <= 0)
        {
            Debug.LogError("[RadialWeapon] Projectile Count는 1 이상이어야 합니다.", this);
            isValid = false;
        }

        return isValid;
    }
}
