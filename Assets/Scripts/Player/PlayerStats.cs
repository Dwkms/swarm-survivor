using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("체력")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private float invincibleDuration = 0.5f;

    // HUD가 나중에 읽어갈 값들. 지금은 Inspector와 로그로만 확인한다.
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

  

    private int currentHealth;
    private float invincibleTimer;
    private bool isDead;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    private void Update()
    {
        // 무적 시간을 흘려보낸다.
        // Time.deltaTime을 쓰므로 프레임레이트와 무관하게 항상 0.5초다.
        if (invincibleTimer > 0f)
        {
            invincibleTimer -= Time.deltaTime;
        }
    }

    // 최대 체력이 늘면 늘어난 만큼 회복시킨다.
    // 안 그러면 카드를 골랐는데 체력바만 길어지고 실제로는 안 채워진다.
    public void SetMaxHealth(int newMax)
    {
        int delta = newMax - maxHealth;
        maxHealth = newMax;

        if (delta > 0)
        {
            currentHealth += delta;
        }

        currentHealth = Mathf.Min(currentHealth, maxHealth);
    }

    // Enter가 아니라 Stay를 쓰는 이유:
    // 적은 죽을 때까지 플레이어에게 붙어 있는다.
    // Enter는 처음 겹친 순간 딱 한 번만 오므로, 한 번 맞고 나면
    // 그 적은 계속 붙어 있어도 다시는 피해를 못 준다.
    // Stay는 겹쳐 있는 동안 물리 스텝마다(초당 50회) 호출된다.
    // 그래서 무적 타이머로 걸러내야 한다. 안 걸면 초당 250 데미지다.
    private void OnTriggerStay2D(Collider2D other)
    {
        if (isDead) return;
        if (invincibleTimer > 0f) return;

        Enemy enemy = other.GetComponent<Enemy>();
        if (enemy == null) return;

        TakeDamage(enemy.ContactDamage);
    }

    private void TakeDamage(int amount)
    {
        currentHealth -= amount;

        // 무적은 "한 대 맞을 때마다" 새로 시작한다.
        // 여러 적에게 동시에 둘러싸여도 0.5초에 한 번만 맞는다.
        invincibleTimer = invincibleDuration;

        Debug.Log($"피격 {amount} → 남은 HP {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;
        currentHealth = 0;

        Debug.Log("플레이어 사망");

        // 임시 처리다. 나중에 GameManager가 게임 정지와 ResultPanel 표시를 맡는다.
        gameObject.SetActive(false);
    }
}