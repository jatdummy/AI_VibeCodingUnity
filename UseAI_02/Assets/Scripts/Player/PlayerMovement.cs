using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("입력")]
    [SerializeField] private InputActionReference _moveAction;  // Player/Move (Vector2)

    [Header("이동")]
    [SerializeField] private float _moveSpeed = 5f;             // 초당 이동 거리

    private void OnEnable() => _moveAction.action.Enable();
    private void Update() => Move();
    private void OnDisable() => _moveAction.action.Disable();

    // WASD 입력을 XZ 평면 이동으로 변환
    private void Move()
    {
        Vector2 input = _moveAction.action.ReadValue<Vector2>();
        Vector3 direction = new Vector3(input.x, 0f, input.y);

        transform.Translate(direction * _moveSpeed * Time.deltaTime, Space.World);
    }
}
