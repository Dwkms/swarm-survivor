using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("스폰 대상")]
    [SerializeField] private GameObject enemyPrefab;

    [Header("스폰 설정")]
    [SerializeField] private float spawnsPerSecond = 4f;   // 0~1분 구간 기준값
    [SerializeField] private float spawnRadius = 14f;      // 화면 대각선(12.3)보다 크게
    [SerializeField] private int maxActiveEnemies = 400;   // 이 수를 넘으면 스폰 일시 중단

    [Header("디버그")]
    [SerializeField] private int burstCount = 100;         // F1 한 번에 만들 마릿수
    [SerializeField] private KeyCode killAllKey = KeyCode.F5;  // F5 누를시 몹 전멸

    private Transform playerTransform;

    // 지금까지 흘러간 시간을 모아두는 통.
    // 한 마리 뽑을 만큼 차면 뽑고, 그만큼 통에서 뺀다.
    private float spawnAccumulator;

    // 현재 살아있는 적의 수. Enemy 쪽에서 켜지고 꺼질 때마다 갱신한다.
    // 매번 FindObjectsByType으로 세면 O(n) 탐색이 반복돼 스폰이 느려진다.
    public static int ActiveEnemyCount { get; private set; }

    public static void RegisterEnemy() { ActiveEnemyCount++; }
    public static void UnregisterEnemy() { ActiveEnemyCount--; }

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
        
        // 씬을 다시 시작해도 static 값이 남아있으면 안 된다.
        // static은 씬 로드와 무관하게 유지되기 때문에 명시적으로 0으로 되돌린다.
        ActiveEnemyCount = 0;
    }

    private void Update()
    {
        HandleDebugInput();
        HandleAutoSpawn();
    }

    private void HandleDebugInput()
    {
        // F1: 성능 측정용 대량 스폰.
        // 이건 장난 기능이 아니라, 나중에 Instantiate 방식과 풀링 방식을
        // "같은 조건"으로 비교하기 위한 계측 버튼이다.
        if (Input.GetKeyDown(KeyCode.F1))
        {
            for (int i = 0; i < burstCount; i++)
            {
                SpawnOne();
            }
            Debug.Log($"[F1] {burstCount}마리 스폰. 현재 활성: {ActiveEnemyCount}");
        }
        // F5: 살아있는 적을 전부 즉사시킨다.
        // "적이 한꺼번에 많이 죽는 상황"을 손으로 만들기 위한 테스트 키다.
        // FindObjectsByType은 비싸지만 키를 누른 그 프레임에만 도는 디버그 코드라 상관없다.
        // (무기의 최근접 탐색은 매 발사마다 도는 것이라 성격이 다르다)
        if (Input.GetKeyDown(killAllKey))
        {
            Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);

            for (int i = 0; i < enemies.Length; i++)
            {
                enemies[i].TakeDamage(9999);
            }

            Debug.Log($"[F5] {enemies.Length}마리 즉사 처리");
        }
    }

    private void HandleAutoSpawn()
    {
        if (playerTransform == null) return;

        // 활성 상한을 넘으면 시간도 모으지 않는다.
        // 여기서 return 하지 않고 통만 채워두면, 상한이 풀리는 순간
        // 밀린 물량이 한꺼번에 터져 프레임이 튄다.
        if (ActiveEnemyCount >= maxActiveEnemies) return;

        spawnAccumulator += Time.deltaTime;

        float interval = 1f / spawnsPerSecond;   // 초당 4마리 → 0.25초에 한 마리

        // while인 이유: 프레임이 크게 튀어 deltaTime이 0.25초를 넘긴 경우,
        // if면 한 마리만 나오고 나머지가 증발한다.
        // while이면 밀린 만큼 전부 뽑아내 "초당 4마리"가 프레임레이트와 무관하게 지켜진다.
        // 적 300마리에서 프레임이 떨어지는 상황을 일부러 만드는 프로젝트라
        // 스폰량이 프레임레이트에 끌려다니면 측정 조건이 오염된다.
        while (spawnAccumulator >= interval)
        {
            SpawnOne();
            spawnAccumulator -= interval;
        }
    }

    private void SpawnOne()
    {
        if (enemyPrefab == null || playerTransform == null) return;

        Vector3 spawnPos = GetRandomPointOnCircle();

        // 지금은 의도적으로 Instantiate를 쓴다.
        // 나중에 풀링으로 바꾸고 이 시점의 수치와 비교하는 것이 이 프로젝트의 산출물이다.
        // 현재 풀링으로 바꿈
        GameObject enemyObj = PoolManager.Spawn(enemyPrefab, spawnPos, Quaternion.identity);

        // 적이 스스로 FindGameObjectWithTag를 부르지 않도록 대상을 직접 넘긴다.
        // 100마리 동시 생성 시 Find 100번이면 그 자체로 프레임 스파이크가 된다.
        Enemy enemy = enemyObj.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.SetTarget(playerTransform);
        }
    }

    private Vector3 GetRandomPointOnCircle()
    {
        // 0 ~ 2π 사이의 각도를 뽑아 삼각함수로 원 위의 한 점을 만든다.
        // Random.insideUnitCircle은 원 "내부"라 화면 안에 적이 튀어나올 수 있다.
        float angle = Random.Range(0f, Mathf.PI * 2f);

        Vector3 offset = new Vector3(
            Mathf.Cos(angle) * spawnRadius,
            Mathf.Sin(angle) * spawnRadius,
            0f
        );

        // 카메라가 아니라 플레이어를 기준으로 삼는다.
        // 카메라는 SmoothDamp로 뒤따라오므로 기준으로 쓰면 스폰 거리가 미세하게 흔들린다.
        return playerTransform.position + offset;
    }
}