using System.Collections.Generic;
using UnityEngine;

public class ExpGem : MonoBehaviour
{
    [SerializeField] private int expAmount = 1;

    public int ExpAmount => expAmount;

    // 씬에 존재하는 모든 젬. 플레이어는 이 목록만 순회한다.
    // FindObjectsByType으로 매 프레임 찾으면 그때마다 배열이 새로 할당되고
    // 씬 전체를 훑는다. 무기의 최근접 적 탐색이 지금 그렇게 되어 있고,
    // 그건 개선 전후를 비교하려고 일부러 남겨둔 것이다. 여기서는 반복하지 않는다.
    public static readonly List<ExpGem> All = new List<ExpGem>();

    // 이미 획득했는지.
    // Destroy는 즉시가 아니라 프레임 끝에 처리되므로, 플래그가 없으면
    // 같은 젬을 다음 프레임에 한 번 더 먹을 수 있다. (총알의 hasHit와 같은 이유)
    private bool collected;

    // Play를 반복해도 static 목록에 지난 판의 잔재가 남지 않도록 초기화한다.
    // 도메인 리로드를 끈 설정에서는 static이 살아남는다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        All.Clear();
    }

    // OnEnable/OnDisable에 등록·해제를 거는 이유는 Enemy의 활성 카운터와 같다.
    // 지금은 Instantiate/Destroy라 Awake/OnDestroy와 결과가 같지만,
    // 풀링으로 바꾸면 오브젝트는 파괴되지 않고 켜졌다 꺼지기만 한다.
    private void OnEnable()
    {
        collected = false;   // 풀에서 다시 꺼내 쓸 때를 대비해 여기서 초기화한다
        All.Add(this);
    }

    private void OnDisable()
    {
        All.Remove(this);
    }

    // 획득에 성공하면 true와 EXP 양을 돌려준다.
    public bool TryCollect(out int amount)
    {
        amount = 0;
        if (collected) return false;

        collected = true;
        amount = expAmount;

        // 목록에서 "즉시" 뺀다. OnDisable은 프레임 끝에야 호출되므로
        // 그때까지 목록에 남아 있으면 같은 프레임에 다시 걸린다.
        All.Remove(this);

        // 지금은 의도적으로 Destroy를 쓴다.
        // 풀링 전환 시 이 줄이 "풀에 반납"으로 바뀐다.
        Destroy(gameObject);
        return true;
    }
}