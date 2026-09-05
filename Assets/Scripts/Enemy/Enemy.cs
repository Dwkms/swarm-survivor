using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Enemy : MonoBehaviour
{
    [Header("이동")]
    [SerializeField] private float moveSpeed = 2f;

    [Header("전투")]
    [SerializeField] private int maxHealth = 20;      // 슬라임 기준
    [SerializeField] private int contactDamage = 5;   // 몸에 닿았을 때 주는 피해

    [Header("드랍")]
    [SerializeField] private GameObject expGemPrefab;

    // 프리팹이 비어 있을 때 적 한 마리마다 로그를 찍으면
    // 900마리 스폰 시 Console이 900줄로 막힌다. 한 번만 알린다.
    private static bool warnedMissingGem;

    // 플레이어가 데미지를 계산할 때 읽어간다. 읽기 전용으로만 연다.
    public int ContactDamage => contactDamage;

    private Rigidbody2D rb;
    private Transform target;

    private int currentHealth;

    // 같은 프레임에 총알 두 발을 맞으면 Die()가 두 번 불릴 수 있다.
    // Destroy는 프레임 끝에 처리되므로 그 사이에 또 맞을 수 있기 때문.
    private bool isDead;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }

            if (expGemPrefab == null && !warnedMissingGem)
            {
                warnedMissingGem = true;
                Debug.LogError("[Enemy] Exp Gem Prefab이 비어 있다. Enemy 프리팹에 젬을 지정해라.", this);
            }
        }
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    private void FixedUpdate()
    {
        if (target == null)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 dir = ((Vector2)target.position - rb.position).normalized;
        rb.linearVelocity = dir * moveSpeed;
    }

    // 총알이 호출한다.
    public void TakeDamage(int amount)
    {
        if (isDead) return;

        currentHealth -= amount;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;

        DropExpGem();

        // Destroy가 아니라 풀로 반납한다.
        // Destroy는 프레임 끝에 처리되지만 Despawn은 즉시 비활성화되고,
        // OnDisable에서 활성 카운터도 바로 줄어든다.
        PoolManager.Despawn(gameObject);
    }

    private void DropExpGem()
    {
        if (expGemPrefab == null) return;

        // 젬의 EXP 양은 젬 프리팹이 갖고 있다.
        // 적 종류별로 1/2/3을 다르게 주는 것은 EnemyData(SO)를 만들 때 처리한다.
        PoolManager.Spawn(expGemPrefab, transform.position, Quaternion.identity);
    }

    // 풀에서 꺼낼 때마다 상태를 초기화한다.
    // Instantiate는 항상 새 객체라 필드가 기본값이지만,
    // 풀은 이전 사용의 상태를 그대로 물려준다.
    // 여기서 되돌리지 않으면 isDead가 true인 채로 되살아난다.
    private void OnEnable()
    {
        currentHealth = maxHealth;
        isDead = false;
        target = null;

        // 반납 직전의 속도가 남아 있으면 되살아난 첫 프레임에 엉뚱한 방향으로 튄다.
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        EnemySpawner.RegisterEnemy();
    }

    private void OnDisable()
    {
        EnemySpawner.UnregisterEnemy();
    }
}