#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace Suika.DebugTools
{
    /// <summary>
    /// 에디터 전용 자동 테스트 봇. 씬의 SuikaGame에 붙이면 무작위 위치로 과일을 계속 떨어뜨리면서
    /// 매 프레임 규칙 위반(합쳐지지 않고 붙어 있는 같은 과일, 벽/바닥 뚫림, NaN 등)을 검사한다.
    /// 게임 오버가 되면 자동으로 재시작해서 여러 판을 돈다.
    /// </summary>
    public class SuikaAutoTester : MonoBehaviour
    {
        public float timeScale = 4f;
        public float dropInterval = 0.55f;
        public int gamesToRun = 3;

        public int games;
        public int drops;
        public int merges;
        public int maxLevelSeen;
        public int stuckPairErrors;
        public int outOfBoundsErrors;
        public int nanErrors;
        public int scoreErrors;
        public bool finished;

        SuikaGame game;
        float dropTimer;
        int lastScore;
        int lastCount;
        float restartTimer;
        readonly Dictionary<long, float> touchTime = new Dictionary<long, float>();
        readonly HashSet<long> reported = new HashSet<long>();

        void Start()
        {
            game = GetComponent<SuikaGame>();
            Time.timeScale = timeScale;
            Debug.Log("[AutoTest] 시작");
        }

        void OnDisable() => Time.timeScale = 1f;

        void Update()
        {
            if (finished || game == null) return;

            if (game.IsGameOver)
            {
                if (game.IsGameOverPanelShown)
                {
                    restartTimer += Time.unscaledDeltaTime;
                    if (restartTimer > 0.3f)
                    {
                        games++;
                        Debug.Log($"[AutoTest] {games}판 종료 점수={game.Score} 드롭={drops} 합치기={merges} 최대단계={maxLevelSeen}");
                        if (games >= gamesToRun) { Finish(); return; }
                        restartTimer = 0f;
                        lastScore = 0;
                        lastCount = 0;
                        touchTime.Clear();
                        game.StartNewGame();
                    }
                }
                return;
            }

            // 점수는 한 판 안에서 줄어들면 안 된다.
            if (game.Score < lastScore) { scoreErrors++; Debug.LogError($"[AutoTest] 점수 감소 {lastScore} -> {game.Score}"); }
            lastScore = game.Score;

            dropTimer -= Time.deltaTime;
            if (dropTimer <= 0f && game.HeldFruit != null)
            {
                float x = Random.Range(game.InnerLeft, game.InnerRight);
                if (game.TryDropAt(x)) drops++;
                dropTimer = dropInterval;
            }

            Check();
        }

        void Check()
        {
            var list = game.ActiveFruits;
            int count = 0;
            for (int i = 0; i < list.Count; i++)
            {
                var a = list[i];
                if (a == null || a.IsMerging) continue;
                count++;
                if (a.Level > maxLevelSeen) maxLevelSeen = a.Level;

                Vector2 p = a.Body.position;
                if (float.IsNaN(p.x) || float.IsNaN(p.y)) { nanErrors++; Debug.LogError("[AutoTest] NaN 위치 " + a.name); continue; }

                // 벽/바닥 뚫림 검사 (약간의 물리 겹침은 허용)
                // 중심이 벽/바닥에서 반지름의 60%보다 가까우면 콜라이더가 크게 파고든 것으로 본다.
                float m = a.Radius * 0.6f;
                if (p.x < game.InnerLeft + m || p.x > game.InnerRight - m || p.y < game.FloorY + m)
                {
                    outOfBoundsErrors++;
                    Debug.LogError($"[AutoTest] 상자 밖 과일 {a.name} pos={p}");
                }

                // 같은 단계인데 닿아 있는 채로 합쳐지지 않는 쌍 검사
                for (int j = i + 1; j < list.Count; j++)
                {
                    var b = list[j];
                    if (b == null || b.IsMerging || b.Level != a.Level || !a.IsInPlay || !b.IsInPlay) continue;
                    float dist = Vector2.Distance(a.Body.position, b.Body.position);
                    long key = PairKey(a, b);
                    if (dist < a.Radius + b.Radius - 0.02f)
                    {
                        touchTime.TryGetValue(key, out float t);
                        t += Time.deltaTime;
                        touchTime[key] = t;
                        if (t > 0.3f && reported.Add(key))
                        {
                            stuckPairErrors++;
                            Debug.LogError($"[AutoTest] 합쳐지지 않는 같은 과일 쌍 {a.name} / {b.name} dist={dist:F3}");
                        }
                    }
                    else touchTime.Remove(key);
                }
            }

            // 과일 수 변화로 추정하면 드롭과 합치기가 같은 프레임에 겹칠 때 놓치므로 게임의 카운터를 쓴다.
            merges = game.MergeCount;
            lastCount = count;
        }

        static long PairKey(Fruit a, Fruit b)
        {
            int x = a.GetInstanceID(), y = b.GetInstanceID();
            if (x > y) { int tmp = x; x = y; y = tmp; }
            return ((long)x << 32) ^ (uint)y;
        }

        void Finish()
        {
            finished = true;
            Time.timeScale = 1f;
            Debug.Log($"[AutoTest] 완료 판={games} 드롭={drops} 합치기(추정)={merges} 최대단계={maxLevelSeen} " +
                      $"붙은채안합쳐짐={stuckPairErrors} 상자밖={outOfBoundsErrors} NaN={nanErrors} 점수오류={scoreErrors}");
        }
    }
}
#endif
