using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed = 12f;      // 유닛/초
    [SerializeField] private float lifeTime = 3f;    // 이 시간 뒤 사라진다
    [SerializeField] private int damage = 10;        // 불릿 Lv1 = 10

    // 읽기 전용 프로퍼티. 4단계에서 적이 이 값을 가져가 HP를 깎는다.
    // 필드를 public으로 열면 외부에서 값을 바꿀 수 있으므로 get만 연다.
    public int Damage => damage;

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // 무기가 생성 직후 호출한다. 어느 방향으로 날아갈지 여기서 정해진다.
    public void Launch(Vector2 direction)
    {
        // 호출한 쪽이 정규화를 잊어도 속도가 튀지 않도록 여기서 한 번 더 건다.
        Vector2 dir = direction.normalized;

        // 속도를 "한 번만" 지정한다.
        // 중력 0, 마찰 0이므로 이후로는 물리 엔진이 등속으로 밀어준다.
        // Update에서 매 프레임 위치를 옮기는 방식보다 싸고,
        // 물리 스텝에 맞춰 움직이므로 Trigger 판정도 안정적이다.
        rb.linearVelocity = dir * speed;

        // 사각형이 날아가는 방향을 바라보게 회전시킨다.
        // Atan2(y, x)는 벡터의 각도를 라디안으로 준다. Rad2Deg로 도 단위로 바꾼다.
        // 2D에서는 Z축 회전만 쓴다.
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        // 지금은 의도적으로 Destroy를 쓴다.
        // 나중에 풀링으로 바꿀 때 이 한 줄이 "풀에 반납"으로 교체되고,
        // 그 전후의 GC Alloc / 프레임 시간을 비교하는 것이 이 프로젝트의 산출물이다.
        Destroy(gameObject, lifeTime);
    }
}