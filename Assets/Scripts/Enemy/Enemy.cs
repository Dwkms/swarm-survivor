using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Enemy : MonoBehaviour
{
    [Header("이동")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float approachSpreadRadius = 0.75f;

    [Header("전투")]
    [SerializeField] private int maxHealth = 20;      // 슬라임 기준
    [SerializeField] private int contactDamage = 5;   // 몸에 닿았을 때 주는 피해
    [SerializeField] private int scoreValue = 10;   // 슬라임 기준

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private float deathAnimationDuration = 0.3f;

    private static readonly int HitHash = Animator.StringToHash("Hit");
    private static readonly int DeadHash = Animator.StringToHash("Dead");

    [Header("드랍")]
    [SerializeField] private GameObject expGemPrefab;

    // 프리팹이 비어 있을 때 적 한 마리마다 로그를 찍으면
    // 900마리 스폰 시 Console이 900줄로 막힌다. 한 번만 알린다.
    private static bool warnedMissingGem;

    // 플레이어가 데미지를 계산할 때 읽어간다. 읽기 전용으로만 연다.
    public int ContactDamage => contactDamage;

    private Rigidbody2D rb;
    private Collider2D enemyCollider;
    private Transform target;
    private Vector2 approachOffset;

    private int currentHealth;

    // 같은 프레임에 총알 두 발을 맞으면 Die()가 두 번 불릴 수 있다.
    // Destroy는 프레임 끝에 처리되므로 그 사이에 또 맞을 수 있기 때문.
    private bool isDead;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        enemyCollider = GetComponent<Collider2D>();

        if (animator == null)
        {
            Debug.LogError("[Enemy] Animator 참조가 비어 있습니다. Enemy 프리팹의 Animator를 연결하세요.", this);
        }

        if (enemyCollider == null)
        {
            Debug.LogError("[Enemy] Collider2D를 찾지 못했습니다. Enemy 프리팹의 Collider를 확인하세요.", this);
        }

        if (deathAnimationDuration <= 0f)
        {
            Debug.LogError("[Enemy] Death Animation Duration은 0보다 커야 합니다.", this);
        }
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

    public void SetTarget(Transform newTarget, bool useApproachSpread)
    {
        target = newTarget;
        approachOffset = useApproachSpread ? CreateApproachOffset() : Vector2.zero;
    }

    private void FixedUpdate()
    {
        if (isDead)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (target == null)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 targetPosition = (Vector2)target.position + approachOffset;
        Vector2 dir = (targetPosition - rb.position).normalized;
        rb.linearVelocity = dir * moveSpeed;
    }

    private Vector2 CreateApproachOffset()
    {
        float angle = Random.Range(0f, Mathf.PI * 2f);
        float distance = Random.Range(approachSpreadRadius * 0.5f, approachSpreadRadius);
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
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
        else if (animator != null)
        {
            // 실제 피해로 생존한 경우에만 Hit 상태를 전환한다.
            animator.SetTrigger(HitHash);
        }
    }

    private void Die()
    {
        isDead = true;
        rb.linearVelocity = Vector2.zero;

        if (enemyCollider != null)
        {
            enemyCollider.enabled = false;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddKill(scoreValue);
        }

        DropExpGem();

        if (animator != null)
        {
            animator.SetBool(DeadHash, true);
        }

        if (deathAnimationDuration <= 0f)
        {
            PoolManager.Despawn(gameObject);
            return;
        }

        StartCoroutine(DespawnAfterDeathAnimation());
    }

    private IEnumerator DespawnAfterDeathAnimation()
    {
        // Animator Update Mode가 Normal이므로 Pause 중 animation과 함께 대기가 멈춘다.
        yield return new WaitForSeconds(deathAnimationDuration);

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
        approachOffset = Vector2.zero;

        // 반납 직전의 속도가 남아 있으면 되살아난 첫 프레임에 엉뚱한 방향으로 튄다.
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        if (enemyCollider != null)
        {
            enemyCollider.enabled = true;
        }

        if (animator != null)
        {
            animator.SetBool(DeadHash, false);
            animator.ResetTrigger(HitHash);
        }

        EnemySpawner.RegisterEnemy();
    }

    private void OnDisable()
    {
        EnemySpawner.UnregisterEnemy();
    }
}
