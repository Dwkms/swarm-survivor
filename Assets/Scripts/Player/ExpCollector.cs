using System;
using UnityEngine;

public class ExpCollector : MonoBehaviour
{
    [SerializeField] private float pickupRadius = 1.5f;

    // 획득한 EXP를 알린다. 2단계에서 LevelSystem이 구독한다.
    // 수집기는 "주웠다"만 알리고 레벨 계산은 모른다.
    public event Action<int> OnExpCollected;


    // 업그레이드 카드로 픽업 반경이 +30%씩 늘어난다.
    // 값 하나만 바꾸면 되는 것이 거리 비교 방식을 택한 이유다.
    public void SetPickupRadius(float radius)
    {
        pickupRadius = radius;
    }

    // 물리가 아니라 단순 거리 비교라 Update에 둔다.
    // FixedUpdate에 두면 물리 주기(초당 50회)에 묶여 오히려 반응이 늦어진다.
    private void Update()
    {
        // 제곱근을 생략하려고 반경도 제곱해서 비교한다.
        float sqrRadius = pickupRadius * pickupRadius;
        Vector2 myPos = transform.position;

        // 뒤에서부터 순회하는 이유:
        // TryCollect()가 목록에서 자기를 즉시 빼기 때문에 목록이 순회 도중 줄어든다.
        // 앞에서부터 돌면 인덱스가 밀려 한 칸씩 건너뛴다.
        for (int i = ExpGem.All.Count - 1; i >= 0; i--)
        {
            ExpGem gem = ExpGem.All[i];
            if (gem == null) continue;

            float sqrDist = ((Vector2)gem.transform.position - myPos).sqrMagnitude;
            if (sqrDist > sqrRadius) continue;

            if (gem.TryCollect(out int amount))
            {
                // 누적과 레벨 계산은 LevelSystem이 한다.
                // 수집기는 "주웠다"만 알린다.
                OnExpCollected?.Invoke(amount);
            }
        }
    }

    // Scene 뷰에서 픽업 반경을 눈으로 확인하기 위한 것.
    // Selected 버전이라 오브젝트를 선택했을 때만 그려진다.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
}