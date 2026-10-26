using UnityEngine;

// 골인 지점: 플레이어가 들어오면 클리어 메시지를 띄우고 깃발을 돌린다.
public class GoalZone : MonoBehaviour
{
    [SerializeField] private Transform flag;          // 클리어 후 빙글빙글 돌릴 깃발
    [SerializeField] private float spinSpeed = 360f;  // 초당 회전 각도

    private bool cleared;

    private void OnTriggerEnter(Collider other)
    {
        if (cleared || !other.CompareTag("Player")) return;

        cleared = true;
        Debug.Log("골인! 클리어했어요!");
    }

    private void Update()
    {
        if (cleared && flag != null)
        {
            flag.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
        }
    }
}
