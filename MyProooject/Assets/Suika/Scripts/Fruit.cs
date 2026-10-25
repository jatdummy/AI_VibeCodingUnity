using System.Collections;
using UnityEngine;

namespace Suika
{
    /// <summary>
    /// 과일 한 개의 상태(단계, 합쳐지는 중인지, 착지했는지)와 충돌 감지를 담당한다.
    /// 합치기 판단과 새 과일 생성은 SuikaGame.Merge 한 곳에서만 처리해서
    /// 두 과일이 동시에 충돌 이벤트를 받아도 한 번만 합쳐지게 한다.
    /// </summary>
    // 그림(SpriteRenderer)은 자식 'Visual'에 둔다. 합치기 연출로 크기를 바꿀 때 물리 몸체(루트)는 건드리지 않기 위해서다.
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public class Fruit : MonoBehaviour
    {
        // 스프라이트 안에 그려진 원의 반지름(로컬 단위). 콜라이더도 이 값에 맞춘다.
        public const float VisualRadius = 0.97f;

        public int Level { get; private set; }
        public float Radius { get; private set; }
        public bool IsInPlay { get; private set; }
        public bool IsMerging { get; private set; }
        public bool HasLanded { get; private set; }
        public float DangerTime { get; set; }
        public Rigidbody2D Body => body;

        Rigidbody2D body;
        CircleCollider2D circle;
        SpriteRenderer spriteRenderer;
        Transform visual;
        SuikaGame game;
        Vector3 baseScale = Vector3.one;
        float mergeSettleTimer;

        // 합쳐진 직후 이 속도(단위/초)보다 빨리 튕겨 나가지 않게 제한한다.
        const float MergeMaxSpeed = 6f;

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            circle = GetComponent<CircleCollider2D>();
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            visual = spriteRenderer != null ? spriteRenderer.transform : transform;
        }

        // 연출 중 '보이는 크기'의 기준. 그림이 자식에 있으면 1, 루트에 있으면 루트 크기.
        Vector3 VisualRestScale => visual == transform ? baseScale : Vector3.one;

        public void Setup(SuikaGame owner, int level, FruitDef def)
        {
            game = owner;
            Level = level;
            Radius = def.radius;
            circle.radius = VisualRadius;
            baseScale = Vector3.one * (def.radius / VisualRadius);
            // 자동 질량(면적 비례)이면 수박이 체리보다 약 49배 무거워서 작은 과일이 바닥에 파묻힌다.
            // 반지름에 비례하는 완만한 질량으로 바꿔 '큰 게 더 무겁다'는 느낌만 남긴다(약 3배 차이).
            body.useAutoMass = false;
            body.mass = 1f + def.radius * 2f;
            transform.localScale = baseScale;
            visual.localScale = VisualRestScale;
            spriteRenderer.sprite = def.sprite;
            // 큰 과일이 작은 과일 위에 그려지도록 단계별로 정렬 순서를 준다.
            spriteRenderer.sortingOrder = 10 + level;
            gameObject.name = $"Fruit_{level:00}_{def.displayName}";
        }

        /// <summary>위에서 들고 있는 상태: 물리 계산에서 제외한다.</summary>
        public void SetHeld()
        {
            IsInPlay = false;
            body.simulated = false;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
        }

        /// <summary>손을 놓아 떨어뜨린다.</summary>
        public void Release()
        {
            body.simulated = true;
            // 들고 있는 동안 물리 위치가 갱신되지 않았으므로 현재 위치로 맞춰 보간 튐을 막는다.
            body.position = transform.position;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            IsInPlay = true;
        }

        /// <summary>합치기로 새로 태어난 과일. 바로 착지한 것으로 취급한다.</summary>
        public void SpawnFromMerge(Vector2 velocity)
        {
            body.simulated = true;
            body.linearVelocity = velocity;
            IsInPlay = true;
            HasLanded = true;
            mergeSettleTimer = 0.15f;
            StartCoroutine(PopIn());
        }

        /// <summary>합쳐지는 중: 더 이상 충돌·합치기에 참여하지 않게 즉시 막는다.</summary>
        public void MarkMerging()
        {
            IsMerging = true;
            IsInPlay = false;
            circle.enabled = false;
            body.simulated = false;
        }

        /// <summary>게임 오버 시 멈춤.</summary>
        public void Freeze()
        {
            IsInPlay = false;
            body.simulated = false;
        }

        void FixedUpdate()
        {
            if (mergeSettleTimer <= 0f || !IsInPlay) return;
            mergeSettleTimer -= Time.fixedDeltaTime;
            // 큰 과일이 이웃 과일과 겹친 채 태어나면 밀어내는 힘으로 위로 튀어 오를 수 있어 속도를 제한한다.
            if (body.linearVelocity.sqrMagnitude > MergeMaxSpeed * MergeMaxSpeed)
                body.linearVelocity = body.linearVelocity.normalized * MergeMaxSpeed;
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            if (IsInPlay) HasLanded = true;
            TryMerge(collision);
        }

        // Enter 시점에 합치지 못한 경우(예: 상대가 막 생성 중)도 계속 붙어 있으면 다시 시도한다.
        void OnCollisionStay2D(Collision2D collision) => TryMerge(collision);

        void TryMerge(Collision2D collision)
        {
            if (!IsInPlay || IsMerging || game == null) return;
            if (!collision.collider.TryGetComponent(out Fruit other)) return;
            if (other == this || !other.IsInPlay || other.IsMerging) return;
            if (other.Level != Level) return;
            game.Merge(this, other);
        }

        IEnumerator PopIn()
        {
            const float duration = 0.16f;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / duration) - 1f;
                // easeOutBack: 살짝 커졌다가 원래 크기로 돌아오는 '뽁' 느낌
                float s = 1f + 2.2f * k * k * k + 1.2f * k * k;
                // 루트(물리 몸체)가 아니라 그림만 키운다. 루트를 매 프레임 바꾸면 보간 중인 Rigidbody2D가 떨린다.
                visual.localScale = VisualRestScale * Mathf.LerpUnclamped(0.55f, 1f, s);
                yield return null;
            }
            visual.localScale = VisualRestScale;
        }
    }
}
