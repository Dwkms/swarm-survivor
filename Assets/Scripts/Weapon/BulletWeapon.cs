using UnityEngine;

public class BulletWeapon : MonoBehaviour
{
    [Header("무기 설정")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float fireInterval = 0.80f;   // 불릿 Lv1 = 0.80초

    // 스포너와 동일한 누적 방식.
    // 발사 주기가 프레임레이트에 끌려다니면 초당 데미지가 달라지고,
    // 그러면 "적 300마리에서 성능이 어떤가"를 재는 조건 자체가 흔들린다.
    private float fireAccumulator;

    private void Start()
    {
        if (projectilePrefab == null)
        {
            Debug.LogError("[BulletWeapon] Projectile Prefab이 비어 있다.", this);
        }
    }

    // 발사는 물리 이동이 아니라 "타이밍 판단"이라 Update에 둔다.
    // 실제 이동은 생성된 Projectile의 Rigidbody2D가 물리 스텝에서 처리한다.
    private void Update()
    {
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

    private void Fire(Transform target)
    {
        // 발사 시점의 방향을 계산해 총알에 넘긴다.
        // 넘긴 뒤에는 총알이 스스로 직진할 뿐, 적을 따라가지 않는다(유도탄 아님).
        Vector2 dir = (Vector2)(target.position - transform.position);

        GameObject obj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);

        Projectile projectile = obj.GetComponent<Projectile>();
        if (projectile != null)
        {
            projectile.Launch(dir);
        }
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