using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed = 12f;
    [SerializeField] private float lifeTime = 3f;
    [SerializeField] private int damage = 10;

    public int Damage => damage;

    private Rigidbody2D rb;

    // 관통하지 않는 총알이다. 한 번 맞추면 끝.
    private bool hasHit;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Launch(Vector2 direction)
    {
        Vector2 dir = direction.normalized;

        rb.linearVelocity = dir * speed;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        Destroy(gameObject, lifeTime);
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
        enemy.TakeDamage(damage);

        Destroy(gameObject);
    }
}