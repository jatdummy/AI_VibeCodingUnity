using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerJump : MonoBehaviour
{
    [Header("입력")]
    [SerializeField] private InputActionReference _jumpAction;  // Player/Jump (Button)

    [Header("점프")]
    [SerializeField] private float _jumpForce = 5f;             // 위쪽으로 가하는 순간 힘

    [Header("착지 판정")]
    [SerializeField] private float _groundCheckDistance = 1.1f; // 피벗에서 아래로 쏘는 레이 길이
    [SerializeField] private LayerMask _groundLayer;            // 바닥으로 인정할 레이어

    private Rigidbody _rigidbody;
    private bool _isJumpRequested;                              // Update에서 받은 입력을 FixedUpdate로 넘기기 위한 플래그

    private void Awake() => _rigidbody = GetComponent<Rigidbody>();
    private void OnEnable() => _jumpAction.action.Enable();
    private void Update() => ReadJumpInput();
    private void FixedUpdate() => Jump();
    private void OnDisable() => _jumpAction.action.Disable();

    // 땅에 있을 때 눌린 점프 입력만 요청으로 저장
    private void ReadJumpInput()
    {
        if (_jumpAction.action.WasPressedThisFrame() && IsGrounded())
            _isJumpRequested = true;
    }

    // 물리 처리는 FixedUpdate에서 수행
    private void Jump()
    {
        if (!_isJumpRequested) return;

        _rigidbody.AddForce(Vector3.up * _jumpForce, ForceMode.Impulse);
        _isJumpRequested = false;
    }

    // 발밑으로 레이를 쏴 바닥 여부 확인
    private bool IsGrounded() => Physics.Raycast(transform.position, Vector3.down, _groundCheckDistance, _groundLayer);
}
