using System.Collections.Generic;
using UnityEngine;

public class PoolManager : MonoBehaviour
{
    [Header("실험")]
    // false면 풀을 쓰지 않고 Instantiate/Destroy로 동작한다.
    // 같은 코드, 같은 빌드에서 두 방식을 비교하기 위한 스위치다.
    // 코드를 되돌렸다 다시 바꾸는 것보다 조건 통제가 확실하다.
    [SerializeField] private bool usePooling = true;

    [Header("사전 생성")]
    // 로딩 시점에 미리 만들어 둘 목록.
    // 어떤 프리팹을 얼마나 데워둘지 한곳에서 본다.
    [SerializeField] private List<PrewarmEntry> prewarmOnStart = new List<PrewarmEntry>();


    private static PoolManager instance;

    // 프리팹마다 풀을 하나씩 둔다.
    private readonly Dictionary<GameObject, ObjectPool> pools = new Dictionary<GameObject, ObjectPool>();

    // Play를 반복해도 static에 지난 판의 잔재가 남지 않도록 초기화한다.
    private void Start()
    {
        if (!usePooling) return;

        for (int i = 0; i < prewarmOnStart.Count; i++)
        {
            PrewarmEntry entry = prewarmOnStart[i];
            if (entry.prefab == null) continue;

            GetPool(entry.prefab).Prewarm(entry.count);
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }

    private static PoolManager Instance
    {
        get
        {
            if (instance == null)
            {
                // Unity 6에서 FindObjectOfType은 Deprecated. FindAnyObjectByType을 쓴다.
                instance = FindAnyObjectByType<PoolManager>();
            }

            // 씬에 없으면 스스로 만든다. 이 경우 usePooling은 기본값 true다.
            if (instance == null)
            {
                GameObject go = new GameObject("[PoolManager]");
                instance = go.AddComponent<PoolManager>();
            }

            return instance;
        }
    }
    [System.Serializable]
    public class PrewarmEntry
    {
        public GameObject prefab;
        public int count = 100;
    }
    public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (!Instance.usePooling)
        {
            return Instantiate(prefab, position, rotation);
        }

        return Instance.GetPool(prefab).Get(position, rotation);
    }

    public static void Despawn(GameObject go)
    {
        if (go == null) return;

        PooledObject marker = go.GetComponent<PooledObject>();

        if (marker == null || marker.Pool == null)
        {
            // 풀 미사용 모드에서는 이게 정상 경로다.
            // 그 상태에서 경고를 찍으면 900줄이 쏟아진다.
            if (Instance.usePooling)
            {
                Debug.LogWarning($"[PoolManager] 풀 소속이 아닌 오브젝트를 반납하려 했다: {go.name}", go);
            }

            Destroy(go);
            return;
        }

        marker.Pool.Return(go);
    }

    public static void Prewarm(GameObject prefab, int count)
    {
        if (!Instance.usePooling) return;

        Instance.GetPool(prefab).Prewarm(count);
    }

    // 디버그 표시용. PerfMonitor가 읽는다.
    public static string GetStats()
    {
        if (instance == null) return "pool -";
        if (!instance.usePooling) return "pool OFF";

        int created = 0;
        int idle = 0;

        foreach (ObjectPool pool in instance.pools.Values)
        {
            created += pool.TotalCreated;
            idle += pool.IdleCount;
        }

        return $"pool {created - idle} / {created}";
    }

    private ObjectPool GetPool(GameObject prefab)
    {
        if (!pools.TryGetValue(prefab, out ObjectPool pool))
        {
            // 프리팹마다 컨테이너를 따로 둔다. Hierarchy에서 구분이 된다.
            GameObject container = new GameObject($"Pool_{prefab.name}");
            container.transform.SetParent(transform, false);

            pool = new ObjectPool(prefab, container.transform);
            pools[prefab] = pool;
        }

        return pool;
    }
}