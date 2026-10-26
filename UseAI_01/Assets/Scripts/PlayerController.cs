using UnityEngine;
using UnityEngine.InputSystem;

// WASD로 이동하고 스페이스바로 점프하는 플레이어 컨트롤러
// Rigidbody(물리) 기반으로 움직인다.
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerController : MonoBehaviour
{
    [Header("이동")]
    [SerializeField] private float moveSpeed = 5f;      // 초당 이동 거리
    [SerializeField] private float turnSpeed = 15f;     // 이동 방향으로 몸을 돌리는 속도

    [Header("점프")]
    [SerializeField] private float jumpForce = 5f;      // 점프할 때 위로 주는 속도
    [SerializeField] private float groundCheckDistance = 0.1f; // 발밑 바닥 감지 여유 거리
    [SerializeField] private LayerMask groundLayer = ~0;       // 바닥으로 인정할 레이어 (기본: 전부)

    [Header("낙하 리스폰")]
    [SerializeField] private float fallY = -10f;        // 이 높이보다 떨어지면 시작 지점으로

    [Header("귀여운 연출")]
    [SerializeField] private Transform visual;          // 찌그러뜨릴 외형(자식 오브젝트)
    [SerializeField] private float squashRecover = 10f; // 원래 모양으로 돌아오는 속도

    private Rigidbody rb;
    private CapsuleCollider capsule;
    private Vector3 moveInput;   // Update에서 읽은 입력을 FixedUpdate로 넘겨줄 값
    private bool jumpRequested;  // 점프 키를 눌렀는지
    private Vector3 spawnPosition;
    private bool wasGrounded;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();

        // 캡슐이 부딪혀서 넘어지지 않도록 회전을 막는다.
        rb.freezeRotation = true;

        // 떨어졌을 때 돌아올 위치 = 처음 서 있던 곳
        spawnPosition = transform.position;
    }

    // 입력은 매 프레임 호출되는 Update에서 읽는다 (키 입력을 놓치지 않기 위해).
    private void Update()
    {
        // 맵 밖으로 떨어지면 시작 지점으로 되돌린다.
        if (transform.position.y < fallY)
        {
            Respawn();
            return;
        }

        bool grounded = IsGrounded();

        // 막 착지한 순간: 납작하게 찌그러뜨렸다가 복원 (통통 튀는 느낌)
        if (grounded && !wasGrounded) Squash(new Vector3(1.25f, 0.75f, 1.25f));
        wasGrounded = grounded;

        // 찌그러진 모양을 원래 크기(1,1,1)로 서서히 되돌린다.
        if (visual != null)
            visual.localScale = Vector3.Lerp(visual.localScale, Vector3.one, squashRecover * Time.deltaTime);

        Keyboard kb = Keyboard.current;
        if (kb == null) return; // 키보드가 연결되지 않은 경우

        float x = 0f;
        float z = 0f;
        if (kb.aKey.isPressed) x -= 1f;
        if (kb.dKey.isPressed) x += 1f;
        if (kb.sKey.isPressed) z -= 1f;
        if (kb.wKey.isPressed) z += 1f;

        // 대각선 이동이 더 빨라지지 않도록 길이를 1로 맞춘다.
        moveInput = new Vector3(x, 0f, z).normalized;

        // 누른 '순간'만 감지 (꾹 누르고 있어도 한 번만 점프)
        if (kb.spaceKey.wasPressedThisFrame && grounded)
        {
            jumpRequested = true;
            Squash(new Vector3(0.8f, 1.3f, 0.8f)); // 뛰어오를 때는 길쭉하게
        }
    }

    // 물리 처리는 고정 주기로 호출되는 FixedUpdate에서 한다.
    private void FixedUpdate()
    {
        // 수평 속도만 입력으로 덮어쓰고, 수직 속도(중력·점프)는 그대로 둔다.
        Vector3 velocity = rb.linearVelocity;
        velocity.x = moveInput.x * moveSpeed;
        velocity.z = moveInput.z * moveSpeed;

        if (jumpRequested)
        {
            // 떨어지던 속도를 지우고 위로 속도를 줘야 항상 같은 높이로 뛴다.
            velocity.y = jumpForce;
            jumpRequested = false;
        }

        rb.linearVelocity = velocity;

        // 움직이는 방향을 바라보게 부드럽게 회전한다.
        // freezeRotation은 '물리 충돌로 인한 회전'만 막으므로 MoveRotation으로는 돌릴 수 있다.
        if (moveInput.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveInput);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, turnSpeed * Time.fixedDeltaTime));
        }
    }

    // 캡슐 중심에서 아래로 구를 쏴서 발밑에 바닥이 있는지 확인한다.
    private bool IsGrounded()
    {
        float radius = capsule.radius * 0.9f;          // 벽에 닿은 걸 바닥으로 착각하지 않게 살짝 줄임
        float distance = capsule.height * 0.5f - radius + groundCheckDistance;
        Vector3 origin = transform.TransformPoint(capsule.center);

        return Physics.SphereCast(origin, radius, Vector3.down, out _, distance,
                                  groundLayer, QueryTriggerInteraction.Ignore);
    }

    private void Squash(Vector3 scale)
    {
        if (visual != null) visual.localScale = scale;
    }

    private void Respawn()
    {
        rb.position = spawnPosition;
        transform.position = spawnPosition;
        rb.linearVelocity = Vector3.zero;
        moveInput = Vector3.zero;
    }
}
