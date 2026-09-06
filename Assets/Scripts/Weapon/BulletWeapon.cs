using UnityEngine;

public class BulletWeapon : MonoBehaviour
{
    [Header("무기 설정")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float fireInterval = 0.80f;   // 불릿 Lv1 = 0.80초
    [SerializeField] private int projectileCount = 3;
    [SerializeField] private float spreadAngle = 20f;

    // 스포너와 동일한 누적 방식.
    // 발사 주기가 프레임레이트에 끌려다니면 초당 데미지가 달라지고,
    // 그러면 "적 300마리에서 성능이 어떤가"를 재는 조건 자체가 흔들린다.
    private float fireAccumulator;
    private bool isConfigurationValid;
    private int projectileBaseDamage;
    private float damageMultiplier = 1f;

    private void Start()
    {
        isConfigurationValid = true;

        if (projectilePrefab == null)
        {
            Debug.LogError("[BulletWeapon] Projectile Prefab이 비어 있다.", this);
            isConfigurationValid = false;
        }
        else
        {
            Projectile projectile = projectilePrefab.GetComponent<Projectile>();
            if (projectile == null)
            {
                Debug.LogError("[BulletWeapon] Projectile Prefab에 Projectile 컴포넌트가 없습니다.", this);
                isConfigurationValid = false;
            }
            else
            {
                projectileBaseDamage = projectile.BaseDamage;
                if (projectileBaseDamage < 1)
                {
                    Debug.LogError("[BulletWeapon] Projectile 기본 Damage는 1 이상이어야 합니다.", this);
                    isConfigurationValid = false;
                }
            }
        }

        if (projectileCount < 1)
        {
            Debug.LogError("[BulletWeapon] Projectile Count는 1 이상이어야 합니다.", this);
            isConfigurationValid = false;
        }

        if (spreadAngle < 0f)
        {
            Debug.LogError("[BulletWeapon] Spread Angle은 0 이상이어야 합니다.", this);
            isConfigurationValid = false;
        }
    }

    // 발사는 물리 이동이 아니라 "타이밍 판단"이라 Update에 둔다.
    // 실제 이동은 생성된 Projectile의 Rigidbody2D가 물리 스텝에서 처리한다.
    private void Update()
    {
        if (!isConfigurationValid) return;

        fireAccumulator += Time.deltaTime;

        while (fireAccumulator >= fireInterval)
        {
            Transform target = FindNearestEnemy();

            if (target == null)
            {
                // 쏠 대상이 없으면 모아둔 시간을 버린다.
                // 그냥 두면 적이 처음 등장하는 순간 밀린 만큼 한꺼번에 쏟아진다.
                fireAccumulator = 0f;
                break;
            }

            Fire(target);
            fireAccumulator -= fireInterval;
        }
    }
    public float FireInterval => fireInterval;
    public void SetFireInterval(float value) => fireInterval = value;

    public void SetDamageMultiplier(float value)
    {
        if (value <= 0f)
        {
            Debug.LogError("[BulletWeapon] Damage Multiplier는 0보다 커야 합니다.", this);
            return;
        }

        damageMultiplier = value;
    }

    private void Fire(Transform target)
    {
        // 최근접 Enemy 방향은 Burst마다 한 번만 구한다.
        // 이후 펠릿은 이 중앙 방향을 회전시킬 뿐, 각각 다시 타겟을 찾지 않는다.
        Vector2 centerDirection = ((Vector2)target.position - (Vector2)transform.position).normalized;
        float angleStep = projectileCount > 1 ? spreadAngle / (projectileCount - 1) : 0f;
        float startAngle = -spreadAngle * 0.5f;
        int projectileDamage = Mathf.RoundToInt(projectileBaseDamage * damageMultiplier);
        bool launchedProjectile = false;

        for (int i = 0; i < projectileCount; i++)
        {
            // 3발, 총 20도면 -10도 / 0도 / +10도다.
            Vector2 direction = RotateDirection(centerDirection, startAngle + angleStep * i);
            GameObject obj = PoolManager.Spawn(projectilePrefab, transform.position, Quaternion.identity);
            Projectile projectile = obj.GetComponent<Projectile>();

            if (projectile == null)
            {
                continue;
            }

            projectile.Launch(direction, projectileDamage);
            launchedProjectile = true;
        }

        // 펠릿 수와 관계없이 실제로 성립한 Burst 하나에만 발사음을 낸다.
        if (launchedProjectile)
        {
            GameAudio.PlayFire();
        }
    }

    private static Vector2 RotateDirection(Vector2 direction, float angleDegrees)
    {
        float radians = angleDegrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);

        return new Vector2(
            direction.x * cos - direction.y * sin,
            direction.x * sin + direction.y * cos);
    }

    private Transform FindNearestEnemy()
    {
        // ── Unity 6 주의 ──
        // FindObjectsOfType은 Deprecated다. FindObjectsByType을 쓴다.
        // FindObjectsSortMode.None = "정렬하지 마라".
        // 어차피 전부 순회할 건데 정렬까지 하면 순수 낭비다.
        //
        // 이 방식은 느리다. 매 발사마다 씬 전체를 훑고 배열을 새로 할당한다(GC 부담).
        // 지금은 의도적으로 이 상태로 둔다. 나중에 개선했을 때
        // "무엇을 얼마나 줄였는가"를 숫자로 말하려면 기준선이 있어야 한다.
        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);

        Transform nearest = null;
        float nearestSqrDist = float.MaxValue;
        Vector2 myPos = transform.position;

        for (int i = 0; i < enemies.Length; i++)
        {
            // sqrMagnitude = 제곱근을 생략한 거리.
            // 실제 거리값이 필요한 게 아니라 "누가 더 가까운가"만 비교하면 되므로
            // 비싼 Sqrt를 적 수만큼 반복할 이유가 없다.
            // a < b 이면 a² < b² 이므로 대소 비교 결과는 같다.
            float sqrDist = ((Vector2)enemies[i].transform.position - myPos).sqrMagnitude;

            if (sqrDist < nearestSqrDist)
            {
                nearestSqrDist = sqrDist;
                nearest = enemies[i].transform;
            }
        }

        return nearest;
    }
}
