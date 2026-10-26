using UnityEngine;

// 플레이어 뒤쪽 위에서 부드럽게 따라다니는 카메라
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;                       // 따라갈 대상 (Player)
    [SerializeField] private Vector3 offset = new Vector3(0f, 5f, -8f); // 대상 기준 카메라 위치
    [SerializeField] private float smoothSpeed = 5f;                 // 클수록 빠르게 따라붙음

    // 플레이어가 Update/FixedUpdate에서 움직인 '다음'에 따라가야 떨림이 없어서 LateUpdate를 쓴다.
    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = target.position + offset;
        // 순간이동하지 않고 부드럽게 따라가도록 Lerp로 조금씩 다가간다.
        transform.position = Vector3.Lerp(transform.position, desired, smoothSpeed * Time.deltaTime);
        // 캐릭터 발이 아니라 몸 쪽을 바라보게 살짝 위를 본다.
        transform.LookAt(target.position + Vector3.up * 1f);
    }
}
