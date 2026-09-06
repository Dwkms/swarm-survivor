using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed = 12f;
    [SerializeField] private float lifeTime = 3f;
    [SerializeField] private int damage = 10;

    public int BaseDamage => damage;
    public int Damage => currentDamage;

    private Rigidbody2D rb;
    // 풀에서 꺼낸 이번 활성화에만 적용되는 피해량이다.
    // 공용 Prefab의 기본 damage를 무기별 강화값으로 덮어쓰지 않는다.
    private int currentDamage;

    // 관통하지 않는 총알이다. 한 번 맞추면 끝.
    private bool hasHit;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (damage < 1)
        {
            Debug.LogError("[Projectile] Damage는 1 이상이어야 합니다.", this);
        }
    }
    private float lifeTimer;

    // 풀에서 꺼낼 때마다 초기화한다.
    private void OnEnable()
    {
        hasHit = false;
        lifeTimer = lifeTime;
        currentDamage = damage;
    }

    private void Update()
    {
        // Destroy(gameObject, lifeTime)을 쓸 수 없어 직접 센다.
        // 프레임마다 Update가 도는 비용이 생기지만, 동시에 떠 있는 총알이
        // 몇 개뿐이라 감수한다. 수백 개가 되면 다시 볼 지점이다.
        lifeTimer -= Time.deltaTime;

        if (lifeTimer <= 0f)
        {
            PoolManager.Despawn(gameObject);
        }
    }

    public void Launch(Vector2 direction)
    {
        Launch(direction, damage);
    }

    public void Launch(Vector2 direction, int damageValue)
    {
        if (damageValue < 1)
        {
            Debug.LogError("[Projectile] Launch Damage는 1 이상이어야 합니다.", this);
            PoolManager.Despawn(gameObject);
            return;
        }

        currentDamage = damageValue;
        Vector2 dir = direction.normalized;

        rb.linearVelocity = dir * speed;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

      
    }

    // 적의 Collider가 Is Trigger이므로 이 콜백이 온다.
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Destroy는 "즉시"가 아니라 이번 프레임이 끝날 때 처리된다.
        // 그 사이에 다른 적과도 겹치면 이 함수가 또 불린다.
        // 플래그가 없으면 총알 한 발이 여러 마리를 죽인다.
        if (hasHit) return;

        // CompareTag로 먼저 거르지 않는 이유:
        // 어차피 TakeDamage를 부르려면 컴포넌트가 필요하다.
        // 태그 비교 후 GetComponent를 또 하면 같은 일을 두 번 하는 셈이다.
        // GetComponent가 null이면 그게 곧 "적이 아니다"라는 판정이다.
        Enemy enemy = other.GetComponent<Enemy>();
        if (enemy == null) return;

        hasHit = true;
        enemy.TakeDamage(currentDamage);

        PoolManager.Despawn(gameObject);
    }
}
