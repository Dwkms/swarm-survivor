using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Enemy : MonoBehaviour
{
    [Header("이동")]
    [SerializeField] private float moveSpeed = 2f;

    [Header("전투")]
    [SerializeField] private int maxHealth = 20;      // 슬라임 기준
    [SerializeField] private int contactDamage = 5;   // 몸에 닿았을 때 주는 피해

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

        // 체력 초기화는 Awake에서 한다.
        // Instantiate 직후 Start가 오기 전에 총알을 맞을 수도 있는데,
        // 그때 currentHealth가 0이면 태어나자마자 죽는다.
        currentHealth = maxHealth;
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

        // 지금은 의도적으로 Destroy를 쓴다.
        // 풀링으로 바꿀 때 이 줄이 "풀에 반납"이 되고,
        // OnEnable/OnDisable에 걸어둔 카운터가 그대로 동작한다.
        // 나중에 EXP 젬 드랍도 이 자리에 들어간다.
        Destroy(gameObject);
    }

    private void OnEnable()
    {
        EnemySpawner.RegisterEnemy();
    }

    private void OnDisable()
    {
        EnemySpawner.UnregisterEnemy();
    }
}