using System.Collections.Generic;
using UnityEngine;

public class PoolManager : MonoBehaviour
{
    private static PoolManager instance;

    // 프리팹마다 풀을 하나씩 둔다.
    private readonly Dictionary<GameObject, ObjectPool> pools = new Dictionary<GameObject, ObjectPool>();

    // Play를 반복해도 static에 지난 판의 잔재가 남지 않도록 초기화한다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }

    // 씬에 미리 놓지 않아도 필요할 때 스스로 생긴다.
    // Inspector 연결을 하나 줄이면 "연결을 빠뜨려서 안 되는" 실패도 하나 줄어든다.
    private static PoolManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("[PoolManager]");
                instance = go.AddComponent<PoolManager>();
            }
            return instance;
        }
    }

    public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        return Instance.GetPool(prefab).Get(position, rotation);
    }

    public static void Despawn(GameObject go)
    {
        if (go == null) return;

        PooledObject marker = go.GetComponent<PooledObject>();

        if (marker == null || marker.Pool == null)
        {
            // 풀에서 나온 것이 아니다. 씬에 손으로 놓은 오브젝트 등.
            // 조용히 넘기면 원인을 못 찾으므로 소리를 낸 뒤 Destroy로 처리한다.
            Debug.LogWarning($"[PoolManager] 풀 소속이 아닌 오브젝트를 반납하려 했다: {go.name}", go);
            Destroy(go);
            return;
        }

        marker.Pool.Return(go);
    }

    public static void Prewarm(GameObject prefab, int count)
    {
        Instance.GetPool(prefab).Prewarm(count);
    }

    // 디버그 표시용. PerfMonitor가 읽는다.
    public static string GetStats()
    {
        if (instance == null) return "pool -";

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