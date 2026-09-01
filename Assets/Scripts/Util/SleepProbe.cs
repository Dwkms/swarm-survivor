using UnityEngine;

/// <summary>
/// 수면 가설 검증용 임시 진단 스크립트.
/// F2를 누른 "그 순간"의 적 Rigidbody2D 상태를 한 번만 집계해 Console에 출력한다.
/// 매 프레임 도는 코드가 아니므로 측정 대상 프레임에 부하를 주지 않는다.
/// 0단계와 2단계 측정이 끝나면 삭제한다.
/// </summary>
public class SleepProbe : MonoBehaviour
{
    // F1(대량 스폰)과 겹치지 않게 F2를 쓴다. Inspector에서 바꿀 수 있게 노출.
    [SerializeField] private KeyCode probeKey = KeyCode.F2;

    // 플레이어 위치를 알아야 "적이 도착했는지"를 판정할 수 있다.
    // Inspector 참조로 두지 않고 Tag로 찾는다. 다른 PC에서 GUID가 갈려도 안 끊긴다.
    private Transform player;

    private void Start()
    {
        GameObject go = GameObject.FindWithTag("Player");

        // 조용히 실패시키지 않는다. 못 찾으면 그 사실을 소리 내어 알린다.
        if (go == null)
        {
            Debug.LogError("[SleepProbe] Tag가 Player인 오브젝트를 못 찾았다.", this);
            return;
        }

        player = go.transform;
    }

    private void Update()
    {
        // GetKeyDown은 "누른 그 프레임"에만 true.
        // GetKey를 쓰면 누르고 있는 동안 매 프레임 900개를 순회해 그 자체가 부하가 된다.
        if (!Input.GetKeyDown(probeKey)) return;

        Probe();
    }

    private void Probe()
    {
        // 씬 전체에서 Enemy를 긁어온다. 비효율적이지만 키를 누른 순간 한 번만 도는
        // 진단 코드라 허용한다. 측정 프레임에는 영향이 없다.
        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);

        // 조용히 실패하지 않는다. 적이 없으면 왜 없는지 소리를 낸다.
        if (enemies.Length == 0)
        {
            Debug.LogWarning("[SleepProbe] 적이 0마리다. F1로 먼저 스폰해라.");
            return;
        }

        int sleeping = 0;      // 물리 엔진이 재운 바디 수
        int awake = 0;         // 시뮬레이션에 참여 중인 바디 수
        int noRigidbody = 0;   // Rigidbody2D가 없는 경우 (프리팹 설정 사고 감지용)
        float speedSum = 0f;
        float maxSpeed = 0f;

        int arrived = 0;                  // 플레이어 반경 1 이내에 도달한 적 수
        float minDistance = float.MaxValue;  // 가장 가까운 적의 거리


        foreach (Enemy enemy in enemies)
        {
            Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();

            // null이면 세어만 두고 넘어간다. 예외로 죽으면 나머지 집계를 못 본다.
            if (rb == null)
            {
                noRigidbody++;
                continue;
            }

            // IsSleeping()이 이 검증의 핵심이다.
            // Velocity가 0인 것과 "잠들었다"는 것은 다른 상태이고, 둘을 구분해야
            // 가설 A와 B가 갈린다.
            if (rb.IsSleeping()) sleeping++;
            else awake++;

            // 여기서는 sqrMagnitude가 아니라 magnitude를 쓴다.
            // 대소 비교가 아니라 "사람이 읽을 실제 속도값"이 필요하기 때문이다.
            // 이동속도 2로 설정했으니 활성 상태면 2 근처가 나와야 한다.
            float speed = rb.linearVelocity.magnitude;
            speedSum += speed;
            if (speed > maxSpeed) maxSpeed = speed;

            // 여기서만 sqrMagnitude가 아니라 실제 거리를 쓴다.
            // 사람이 "몇 유닛 남았나"를 읽어야 하는 값이라 제곱근이 필요하다.
            // 진단 코드이고 키를 누른 프레임에 한 번만 도니 비용은 무시한다.
            if (player != null)
            {
                float distance = Vector2.Distance(rb.position, player.position);
                if (distance < minDistance) minDistance = distance;
                if (distance <= 1f) arrived++;
            }
        }

        int counted = sleeping + awake;
        float avgSpeed = counted > 0 ? speedSum / counted : 0f;

        // 문자열 조합은 GC를 만들지만, 키를 누른 프레임에 한 번뿐이라 무시할 수 있다.
        // 이 원칙이 깨지면 안 되는 곳은 매 프레임 도는 PerfMonitor 쪽이다.
        Debug.Log(
          $"[SleepProbe] 적 {enemies.Length}마리 | 수면 {sleeping} / 활성 {awake} | " +
          $"평균속도 {avgSpeed:F3} / 최대속도 {maxSpeed:F3} | " +
          $"도착(1이내) {arrived} / 최소거리 {minDistance:F2}" +
          (noRigidbody > 0 ? $" | Rigidbody2D 없음 {noRigidbody}" : ""));

    }
}
