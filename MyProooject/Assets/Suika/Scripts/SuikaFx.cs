using UnityEngine;

namespace Suika
{
    /// <summary>
    /// 과일이 합쳐질 때 과일 색 파티클을 터뜨린다.
    /// 파티클 시스템 하나를 계속 재사용해서 매번 오브젝트를 만들지 않는다.
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public class SuikaFx : MonoBehaviour
    {
        ParticleSystem ps;

        void Awake()
        {
            ps = GetComponent<ParticleSystem>();
            var main = ps.main;
            // loop를 켜 두어야 시스템이 계속 재생 상태로 남아 Emit한 파티클이 갱신된다.
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 1000;
            main.gravityModifier = 0.6f;
            main.startLifetime = 0.6f;

            var emission = ps.emission;
            emission.enabled = false; // 자동 방출 없이 Emit으로만 낸다.
            var shape = ps.shape;
            shape.enabled = false;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.5f), new GradientAlphaKey(0f, 1f) });
            colorOverLifetime.color = gradient;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));

            ps.Play();
        }

        public void Burst(Vector2 position, Color color, float radius)
        {
            if (ps == null) return;
            int count = Mathf.RoundToInt(10f + radius * 14f);
            var p = new ParticleSystem.EmitParams();
            for (int i = 0; i < count; i++)
            {
                Vector2 dir = Random.insideUnitCircle.normalized;
                if (dir == Vector2.zero) dir = Vector2.up;
                p.position = position + dir * radius * 0.7f;
                p.velocity = dir * Random.Range(1.5f, 4f) * (0.6f + radius * 0.5f);
                p.startColor = Color.Lerp(color, Color.white, Random.Range(0f, 0.35f));
                p.startSize = Random.Range(0.08f, 0.2f) * (0.7f + radius * 0.4f);
                p.startLifetime = Random.Range(0.35f, 0.7f);
                ps.Emit(p, 1);
            }
        }
    }
}
