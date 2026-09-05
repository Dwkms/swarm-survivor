using System.Collections.Generic;
using UnityEngine;

// MonoBehaviour가 아니다. 씬에 붙을 이유가 없고, PoolManager가 소유한다.
public class ObjectPool
{
    private readonly GameObject prefab;
    private readonly Transform container;

    // Queue가 아니라 Stack인 이유:
    // 방금 반납한 오브젝트를 바로 다시 꺼내면 CPU 캐시에 남아 있을 확률이 높다.
    // 꺼내는 순서가 게임 로직에 영향을 주지 않으므로 LIFO가 유리하다.
    private readonly Stack<GameObject> idle = new Stack<GameObject>();

    // 통계. 풀이 실제로 재사용하고 있는지 눈으로 확인하기 위한 것.
    public int TotalCreated { get; private set; }
    public int IdleCount => idle.Count;
    public int ActiveCount => TotalCreated - idle.Count;

    public ObjectPool(GameObject prefab, Transform container)
    {
        this.prefab = prefab;
        this.container = container;
    }

    public GameObject Get(Vector3 position, Quaternion rotation)
    {
        GameObject go = idle.Count > 0 ? idle.Pop() : CreateNew();

        // 활성화 "전에" 위치를 잡는다.
        // 순서가 반대면 이전에 쓰던 자리에서 한 프레임 보이고 순간이동한다.
        go.transform.SetPositionAndRotation(position, rotation);
        go.SetActive(true);

        return go;
    }

    public void Return(GameObject go)
    {
        // SetActive(false)가 OnDisable을 부른다.
        // Enemy의 활성 카운터, ExpGem의 목록 해제가 여기에 걸려 있고,
        // 그래서 처음부터 Awake/OnDestroy가 아니라 OnEnable/OnDisable에 걸어뒀다.
        go.SetActive(false);
        idle.Push(go);
    }

    // 로딩 시점에 미리 만들어 둔다.
    // 풀링이 생성 비용을 "없애는" 것이 아니라 "앞당기는" 것임을 보여주는 부분이다.
    public void Prewarm(int count)
    {
        for (int i = 0; i < count; i++)
        {
            idle.Push(CreateNew());
        }
    }

    private GameObject CreateNew()
    {
        // 컨테이너의 자식으로 만들고 이후로는 부모를 바꾸지 않는다.
        // 반납할 때마다 SetParent를 하면 Transform 계층 갱신 비용이 매번 붙는다.
        GameObject go = Object.Instantiate(prefab, container);
        go.SetActive(false);

        // 이 오브젝트가 어느 풀 소속인지 스스로 기억하게 한다.
        // Despawn을 부르는 쪽이 풀을 몰라도 되게 만드는 장치다.
        PooledObject marker = go.AddComponent<PooledObject>();
        marker.Pool = this;

        TotalCreated++;
        return go;
    }
}