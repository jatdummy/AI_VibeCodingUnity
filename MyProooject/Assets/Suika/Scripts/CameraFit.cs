using UnityEngine;

namespace Suika
{
    /// <summary>
    /// 화면 비율(가로/세로)이 달라도 게임 영역 전체가 항상 보이도록 직교 카메라 크기를 맞추고,
    /// 큰 과일이 합쳐질 때 화면 흔들림을 준다.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFit : MonoBehaviour
    {
        [Tooltip("항상 화면 안에 들어와야 하는 월드 영역")]
        [SerializeField] Rect worldArea = new Rect(-4.3f, -6.55f, 8.6f, 12.65f);

        Camera cam;
        Vector3 basePosition;
        float shakeTime;
        float shakeDuration;
        float shakeStrength;

        void Awake()
        {
            cam = GetComponent<Camera>();
            cam.orthographic = true;
            basePosition = new Vector3(worldArea.center.x, worldArea.center.y, transform.position.z);
            Fit();
        }

        void LateUpdate()
        {
            Fit();

            Vector3 offset = Vector3.zero;
            if (shakeTime > 0f)
            {
                shakeTime -= Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(shakeTime / shakeDuration);
                offset = (Vector3)(Random.insideUnitCircle * shakeStrength * k);
            }
            transform.position = basePosition + offset;
        }

        void Fit()
        {
            float aspect = Mathf.Max(0.01f, cam.aspect);
            float sizeByHeight = worldArea.height * 0.5f;
            float sizeByWidth = worldArea.width * 0.5f / aspect;
            cam.orthographicSize = Mathf.Max(sizeByHeight, sizeByWidth);
        }

        /// <summary>
        /// 흔들림 오프셋을 뺀 기준 위치로 화면 x좌표를 월드 x좌표로 바꾼다.
        /// </summary>
        public float ScreenToWorldXStable(float screenX)
        {
            Rect r = cam.pixelRect;
            float t = r.width > 0f ? (screenX - r.x) / r.width : 0.5f;
            float halfWidth = cam.orthographicSize * cam.aspect;
            return basePosition.x + (t * 2f - 1f) * halfWidth;
        }

        public void Shake(float strength, float duration)
        {
            // 진행 중인 더 강한 흔들림은 약한 흔들림으로 덮어쓰지 않는다.
            if (shakeTime > 0f && strength < shakeStrength) return;
            shakeStrength = strength;
            shakeDuration = Mathf.Max(0.01f, duration);
            shakeTime = shakeDuration;
        }
    }
}
