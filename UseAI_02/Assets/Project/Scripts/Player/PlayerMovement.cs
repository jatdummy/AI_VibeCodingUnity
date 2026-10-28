using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("입력")]
    [SerializeField] private InputActionReference _moveAction;

    [Header("이동")]
    [SerializeField] private float _moveSpeed = 5f;

    private Rigidbody _rigidbody;
    private Vector2 _moveInput;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        _moveAction.action.Enable();
    }

    private void Update()
    {
        // 입력은 Update에서 읽어 두고, 물리 적용은 FixedUpdate에서 한다
        _moveInput = _moveAction.action.ReadValue<Vector2>();
    }

    private void FixedUpdate()
    {
        // 입력의 y축을 월드 z축(앞뒤)으로 옮기고, 중력에 의한 y 속도는 물리 엔진에 맡기기 위해 유지한다
        Vector3 velocity = new Vector3(_moveInput.x * _moveSpeed, _rigidbody.linearVelocity.y, _moveInput.y * _moveSpeed);
        _rigidbody.linearVelocity = velocity;
    }

    private void OnDisable()
    {
        _moveAction.action.Disable();
    }
}
