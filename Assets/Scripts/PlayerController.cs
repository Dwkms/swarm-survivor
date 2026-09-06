using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private Animator animator;

    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");

    public float MoveSpeed => moveSpeed;
    public void SetMoveSpeed(float value) => moveSpeed = value;

    private Rigidbody2D rb;
    private Vector2 moveInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (animator == null)
        {
            Debug.LogError("[PlayerController] Animator 참조가 비어 있습니다. Player의 Animator를 연결하세요.", this);
        }
    }

    private void Update()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        moveInput = new Vector2(h, v).normalized;

        // 이동에 실제 사용하는 입력 벡터로 Idle/Run 상태를 함께 판단한다.
        if (animator != null)
        {
            animator.SetBool(IsMovingHash, moveInput.sqrMagnitude > 0f);
        }
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = moveInput * moveSpeed;
    }
}