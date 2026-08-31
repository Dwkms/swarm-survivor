using UnityEngine;

// 이 컴포넌트가 붙는 오브젝트에 Rigidbody2D가 없으면 자동으로 붙여준다.
// 프리팹에서 실수로 컴포넌트를 지웠을 때 NullReference 대신 조용히 복구된다.
[RequireComponent(typeof(Rigidbody2D))]
public class Enemy : MonoBehaviour
{
    // [SerializeField] private = Inspector에는 보이지만 다른 스크립트는 못 건드린다.
    // public으로 열면 어디서든 값이 바뀔 수 있어 버그 추적이 어려워진다.
    [SerializeField] private float moveSpeed = 2f;

    private Rigidbody2D rb;      // 매 프레임 GetComponent를 부르지 않기 위해 캐싱
    private Transform target;    // 쫓아갈 대상 (플레이어)

    // Awake: 오브젝트가 생성되는 즉시, 다른 어떤 Start보다도 먼저 실행된다.
    // "자기 자신을 준비하는 일"은 여기서 한다.
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Start: 첫 프레임 갱신 직전에 한 번 실행된다.
    // "남을 찾는 일"은 여기서 한다. 상대가 아직 Awake도 안 끝났을 수 있기 때문.
    private void Start()
    {
        // target이 이미 채워져 있으면(= 스포너가 넘겨줬으면) 찾지 않는다.
        // 씬에 손으로 배치한 적은 여기서 스스로 플레이어를 찾는다.
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
        }
    }

    // 스포너가 적을 만든 직후 호출한다.
    // Instantiate 직후 ~ Start 실행 전 사이에 값이 들어오므로 위 Start의 if가 걸러준다.
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    // FixedUpdate: 물리 갱신 주기(기본 0.02초 = 초당 50회)마다 호출된다.
    // Rigidbody2D를 건드리는 코드는 반드시 여기에 둔다.
    private void FixedUpdate()
    {
        // 플레이어가 죽어서 사라진 경우 등. 없으면 제자리에 선다.
        if (target == null)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // rb.position을 쓰는 이유: Interpolate를 켜면 transform.position은
        // 눈에 보이기 좋게 보정된 값이라 실제 물리 위치보다 살짝 뒤처져 있다.
        // 물리 계산은 물리 좌표로 한다.
        //
        // normalized: 방향만 남기고 길이를 1로 만든다.
        // 안 하면 플레이어에서 멀수록 빨라진다.
        Vector2 dir = ((Vector2)target.position - rb.position).normalized;

        // Unity 6부터 velocity → linearVelocity 로 이름이 바뀌었다.
        // 속도를 직접 지정하면 물리 엔진이 충돌 반응을 정상적으로 처리해준다.
        rb.linearVelocity = dir * moveSpeed;
    }

    // OnEnable / OnDisable에 넣는 이유:
    // 지금은 Instantiate/Destroy라 Awake/OnDestroy에 넣어도 같지만,
    // 나중에 풀링으로 바꾸면 오브젝트는 파괴되지 않고 켜졌다 꺼지기만 한다.
    // OnEnable/OnDisable에 두면 그때 코드를 고칠 필요가 없다.
    private void OnEnable()
    {
        EnemySpawner.RegisterEnemy();
    }

    private void OnDisable()
    {
        EnemySpawner.UnregisterEnemy();
    }
}