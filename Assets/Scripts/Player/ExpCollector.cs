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
    public float PickupRadius => pickupRadius;

    // 물리가 아니라 단순 거리 비교라 Update에 둔다.
    // FixedUpdate에 두면 물리 주기(초당 50회)에 묶여 오히려 반응이 늦어진다.
    private void Update()
    {
        float sqrRadius = pickupRadius * pickupRadius;
        Vector2 myPos = transform.position;

        // 이번 프레임에 주운 EXP를 모은다.
        // 젬마다 이벤트를 쏘면 한 프레임에 50번이 불리는데, 받는 쪽이 하는 일은 같다.
        // 합쳐서 한 번만 알리면 구독자가 늘어나도 비용이 커지지 않고,
        // "한 번에 여러 레벨이 오르는" 경우도 한 호출 안에서 자연스럽게 처리된다.
        int collectedThisFrame = 0;

        for (int i = ExpGem.All.Count - 1; i >= 0; i--)
        {
            ExpGem gem = ExpGem.All[i];
            if (gem == null) continue;

            float sqrDist = ((Vector2)gem.transform.position - myPos).sqrMagnitude;
            if (sqrDist > sqrRadius) continue;

            if (gem.TryCollect(out int amount))
            {
                collectedThisFrame += amount;
            }
        }

        if (collectedThisFrame > 0)
        {
            OnExpCollected?.Invoke(collectedThisFrame);
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