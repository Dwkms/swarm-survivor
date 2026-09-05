using UnityEngine;

// 풀에서 나온 오브젝트에 자동으로 붙는 표식.
// 자기가 어느 풀로 돌아가야 하는지만 기억한다.
[DisallowMultipleComponent]
public class PooledObject : MonoBehaviour
{
    public ObjectPool Pool { get; set; }
}