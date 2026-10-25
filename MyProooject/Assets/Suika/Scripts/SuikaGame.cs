using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Suika
{
    /// <summary>
    /// 게임 전체 흐름: 과일 들기/이동/떨어뜨리기, 합치기, 점수, 게임 오버, 재시작.
    /// </summary>
    public class SuikaGame : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] FruitDatabase database;
        [SerializeField] Fruit fruitPrefab;
        [SerializeField] Transform fruitRoot;

        [Header("Container (world units)")]
        [SerializeField] float innerLeft = -3.3f;
        [SerializeField] float innerRight = 3.3f;
        [SerializeField] float floorY = -4.6f;
        [SerializeField] float spawnY = 4.5f;
        [SerializeField] float dangerLineY = 3.3f;

        [Header("Rules")]
        [Tooltip("떨어뜨린 뒤 다음 과일이 나타날 때까지의 시간")]
        [SerializeField] float dropCooldown = 0.5f;
        [Tooltip("과일이 선을 넘은 상태로 이 시간 이상 머물면 게임 오버")]
        [SerializeField] float gameOverDelay = 2f;
        [Tooltip("선 아래 이 거리 안에 과일이 있으면 경고선을 보여 준다")]
        [SerializeField] float warningMargin = 0.8f;
        [SerializeField] float keyboardSpeed = 6f;
        [Tooltip("가장 큰 과일 두 개가 합쳐져 사라질 때 보너스")]
        [SerializeField] int maxMergeBonus = 100;

        [Header("Refs")]
        [SerializeField] SpriteRenderer dangerLine;
        [SerializeField] SpriteRenderer guideLine;
        [SerializeField] SuikaHUD hud;
        [SerializeField] SuikaAudio sfx;
        [SerializeField] SuikaFx fx;
        [SerializeField] CameraFit cameraFit;

        const string BestScoreKey = "Suika_BestScore";

        readonly List<Fruit> activeFruits = new List<Fruit>();
        Fruit heldFruit;
        int nextLevel;
        int score;
        int bestScore;
        int bestAtStart;
        bool isGameOver;
        bool gameOverPanelShown;
        float spawnTimer;
        float heldX;
        float inputLockTimer;
        Vector2 lastMousePosition;
        Camera mainCamera;
        Color dangerBaseColor = new Color(1f, 0.3f, 0.3f, 1f);

        public FruitDatabase Database => database;
        public bool IsGameOver => isGameOver;
        public bool IsGameOverPanelShown => gameOverPanelShown;
        public int Score => score;
        public IReadOnlyList<Fruit> ActiveFruits => activeFruits;
        public Fruit HeldFruit => heldFruit;
        public float InnerLeft => innerLeft;
        public float InnerRight => innerRight;
        public float FloorY => floorY;
        /// <summary>지금까지 일어난 합치기 횟수(테스트·통계용).</summary>
        public int MergeCount { get; private set; }

        /// <summary>
        /// 자동 테스트용: 들고 있는 과일을 x 위치로 옮겨 바로 떨어뜨린다. 들고 있는 과일이 없으면 false.
        /// </summary>
        public bool TryDropAt(float x)
        {
            if (isGameOver || heldFruit == null) return false;
            heldX = ClampX(x, heldFruit.Radius);
            heldFruit.transform.position = new Vector3(heldX, spawnY, 0f);
            DropHeldFruit();
            return true;
        }

        void Awake()
        {
            mainCamera = Camera.main;
            // 물리 반복 횟수는 런타임에 바꾸면 프로젝트 설정이 영구히 바뀌므로 빌더(에디터)에서 한 번만 설정한다.
            if (dangerLine != null) dangerBaseColor = dangerLine.color;
        }

        void Start()
        {
            if (database == null || database.Count == 0 || fruitPrefab == null)
            {
                Debug.LogError("[Suika] FruitDatabase 또는 Fruit 프리팹이 연결되지 않았습니다.");
                enabled = false;
                return;
            }

            bestScore = PlayerPrefs.GetInt(BestScoreKey, 0);
            if (Mouse.current != null) lastMousePosition = Mouse.current.position.ReadValue();
            if (hud != null) hud.Init(this);
            StartNewGame();
        }

        public void StartNewGame()
        {
            StopAllCoroutines();
            // 재시작 버튼이 선택된 채로 남으면 다음 게임 오버 때 Enter 한 번에 재시작이 두 번 실행된다.
            if (UnityEngine.EventSystems.EventSystem.current != null)
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
            foreach (var f in activeFruits)
                if (f != null) Destroy(f.gameObject);
            activeFruits.Clear();
            if (heldFruit != null) Destroy(heldFruit.gameObject);
            heldFruit = null;

            score = 0;
            bestAtStart = bestScore;
            isGameOver = false;
            gameOverPanelShown = false;
            heldX = 0f;
            spawnTimer = 0f;
            // 재시작 버튼을 누른 클릭이 그대로 '떨어뜨리기'로 들어가지 않게 잠깐 입력을 막는다.
            inputLockTimer = 0.25f;

            nextLevel = RollSpawnLevel();
            SpawnHeldFruit();

            if (hud != null)
            {
                hud.SetScore(score, false);
                hud.SetBest(bestScore);
                hud.HideGameOver();
            }
            SetDangerAlpha(0.12f);
        }

        void Update()
        {
            if (isGameOver)
            {
                var kb = Keyboard.current;
                if (gameOverPanelShown && kb != null && (kb.rKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame))
                    StartNewGame();
                return;
            }

            if (inputLockTimer > 0f) inputLockTimer -= Time.deltaTime;

            HandleInput();

            if (heldFruit == null)
            {
                spawnTimer -= Time.deltaTime;
                if (spawnTimer <= 0f) SpawnHeldFruit();
            }

            UpdateDanger();
        }

        // 게임 도중 앱을 끄거나 내려도 최고 점수가 사라지지 않게 저장한다.
        void OnApplicationPause(bool paused) { if (paused) SaveBest(); }
        void OnApplicationQuit() => SaveBest();

        void SaveBest()
        {
            PlayerPrefs.SetInt(BestScoreKey, bestScore);
            PlayerPrefs.Save();
        }

        // ───────────── 입력 ─────────────

        void HandleInput()
        {
            bool drop = false;
            bool pointerMoved = false;
            float pointerX = heldX;

            var mouse = Mouse.current;
            if (mouse != null)
            {
                Vector2 mp = mouse.position.ReadValue();
                // 마우스를 움직였거나 누르고 있을 때만 따라간다(키보드 조작과 충돌 방지).
                if ((mp - lastMousePosition).sqrMagnitude > 1f || mouse.leftButton.isPressed)
                {
                    pointerX = ScreenToWorldX(mp);
                    pointerMoved = true;
                }
                lastMousePosition = mp;
                if (mouse.leftButton.wasReleasedThisFrame) drop = true;
            }

            var touch = Touchscreen.current;
            if (touch != null)
            {
                var pt = touch.primaryTouch;
                if (pt.press.isPressed || pt.press.wasReleasedThisFrame)
                {
                    pointerX = ScreenToWorldX(pt.position.ReadValue());
                    pointerMoved = true;
                }
                if (pt.press.wasReleasedThisFrame) drop = true;
            }

            float dir = 0f;
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.leftArrowKey.isPressed || kb.aKey.isPressed) dir -= 1f;
                if (kb.rightArrowKey.isPressed || kb.dKey.isPressed) dir += 1f;
                if (kb.spaceKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame)
                    drop = true;
            }

            if (pointerMoved) heldX = pointerX;
            else if (dir != 0f) heldX += dir * keyboardSpeed * Time.deltaTime;

            float r = heldFruit != null ? heldFruit.Radius : database.Get(nextLevel).radius;
            heldX = ClampX(heldX, r);

            if (heldFruit == null) return;

            heldFruit.transform.position = new Vector3(heldX, spawnY, 0f);
            UpdateGuideLine(r);

            if (drop && inputLockTimer <= 0f) DropHeldFruit();
        }

        float ScreenToWorldX(Vector2 screenPos)
        {
            // 화면 흔들림 중에도 들고 있는 과일이 좌우로 떨리지 않게, 흔들림을 뺀 카메라 기준으로 계산한다.
            if (cameraFit != null) return cameraFit.ScreenToWorldXStable(screenPos.x);
            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera == null) return heldX;
            return mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -mainCamera.transform.position.z)).x;
        }

        float ClampX(float x, float radius)
        {
            float min = innerLeft + radius;
            float max = innerRight - radius;
            if (min > max) return (innerLeft + innerRight) * 0.5f;
            return Mathf.Clamp(x, min, max);
        }

        void UpdateGuideLine(float radius)
        {
            if (guideLine == null) return;
            float top = spawnY - radius;
            float bottom = floorY;
            guideLine.enabled = top > bottom;
            guideLine.transform.position = new Vector3(heldX, (top + bottom) * 0.5f, 0f);
            guideLine.transform.localScale = new Vector3(0.05f, Mathf.Max(0.01f, top - bottom), 1f);
        }

        // ───────────── 과일 생성/떨어뜨리기 ─────────────

        int RollSpawnLevel()
        {
            int max = Mathf.Clamp(database.spawnableCount, 1, database.Count);
            return Random.Range(0, max);
        }

        void SpawnHeldFruit()
        {
            int level = nextLevel;
            nextLevel = RollSpawnLevel();

            var def = database.Get(level);
            heldX = ClampX(heldX, def.radius);
            heldFruit = CreateFruit(level, new Vector2(heldX, spawnY));
            heldFruit.SetHeld();
            UpdateGuideLine(def.radius);

            if (hud != null) hud.SetNext(database.Get(nextLevel));
        }

        Fruit CreateFruit(int level, Vector2 position)
        {
            var f = Instantiate(fruitPrefab, position, Quaternion.identity, fruitRoot);
            f.Setup(this, level, database.Get(level));
            return f;
        }

        void DropHeldFruit()
        {
            var f = heldFruit;
            heldFruit = null;
            f.Release();
            activeFruits.Add(f);
            spawnTimer = dropCooldown;
            if (guideLine != null) guideLine.enabled = false;
            if (sfx != null) sfx.PlayDrop();
        }

        // ───────────── 합치기 ─────────────

        /// <summary>
        /// 같은 단계 과일 두 개를 합친다. 두 과일이 각자 충돌 이벤트를 받아 이 함수가
        /// 두 번 불려도, 첫 호출에서 둘 다 MarkMerging 되므로 두 번째 호출은 무시된다.
        /// </summary>
        public void Merge(Fruit a, Fruit b)
        {
            if (isGameOver || a == null || b == null || a == b) return;
            if (a.IsMerging || b.IsMerging || !a.IsInPlay || !b.IsInPlay) return;
            if (a.Level != b.Level) return;

            int level = a.Level;
            Vector2 pos = (a.Body.position + b.Body.position) * 0.5f;
            Vector2 vel = (a.Body.linearVelocity + b.Body.linearVelocity) * 0.5f;

            a.MarkMerging();
            b.MarkMerging();
            MergeCount++;
            activeFruits.Remove(a);
            activeFruits.Remove(b);
            Destroy(a.gameObject);
            Destroy(b.gameObject);

            var oldDef = database.Get(level);

            if (database.IsMaxLevel(level))
            {
                // 가장 큰 과일 두 개는 사라지면서 보너스
                AddScore(maxMergeBonus);
                if (fx != null) fx.Burst(pos, oldDef.color, oldDef.radius * 1.5f);
                if (sfx != null) sfx.PlayMerge(level);
                if (cameraFit != null) cameraFit.Shake(0.25f, 0.4f);
                return;
            }

            int newLevel = level + 1;
            var newDef = database.Get(newLevel);

            // 새 과일이 벽이나 바닥 안쪽에 겹쳐서 생기지 않도록 위치를 보정한다.
            pos.x = ClampX(pos.x, newDef.radius);
            pos.y = Mathf.Max(pos.y, floorY + newDef.radius);

            var merged = CreateFruit(newLevel, pos);
            merged.SpawnFromMerge(vel * 0.5f);
            activeFruits.Add(merged);

            AddScore(newDef.score);
            if (fx != null) fx.Burst(pos, newDef.color, newDef.radius);
            if (sfx != null) sfx.PlayMerge(newLevel);
            if (cameraFit != null && newLevel >= 6) cameraFit.Shake(0.04f * (newLevel - 4), 0.25f);
        }

        void AddScore(int amount)
        {
            score += amount;
            if (score > bestScore)
            {
                bestScore = score;
                if (hud != null) hud.SetBest(bestScore);
            }
            if (hud != null) hud.SetScore(score, true);
        }

        // ───────────── 게임 오버 ─────────────

        void UpdateDanger()
        {
            bool over = false;
            bool near = false;

            for (int i = activeFruits.Count - 1; i >= 0; i--)
            {
                var f = activeFruits[i];
                if (f == null) { activeFruits.RemoveAt(i); continue; }

                // 아직 아무 데도 닿지 않고 떨어지는 중인 과일은 판정하지 않는다.
                if (!f.HasLanded) { f.DangerTime = 0f; continue; }

                float top = f.Body.position.y + f.Radius;
                if (top > dangerLineY)
                {
                    f.DangerTime += Time.deltaTime;
                    over = true;
                    if (f.DangerTime >= gameOverDelay)
                    {
                        TriggerGameOver();
                        return;
                    }
                }
                else
                {
                    f.DangerTime = 0f;
                    if (top > dangerLineY - warningMargin) near = true;
                }
            }

            if (over) SetDangerAlpha(0.45f + 0.45f * Mathf.Sin(Time.time * 14f));
            else if (near) SetDangerAlpha(0.55f);
            else SetDangerAlpha(0.12f);
        }

        void SetDangerAlpha(float a)
        {
            if (dangerLine == null) return;
            var c = dangerBaseColor;
            c.a = Mathf.Clamp01(a);
            dangerLine.color = c;
        }

        void TriggerGameOver()
        {
            if (isGameOver) return;
            isGameOver = true;

            if (heldFruit != null)
            {
                Destroy(heldFruit.gameObject);
                heldFruit = null;
            }
            if (guideLine != null) guideLine.enabled = false;
            foreach (var f in activeFruits)
                if (f != null) f.Freeze();

            SetDangerAlpha(1f);
            SaveBest();
            if (sfx != null) sfx.PlayGameOver();

            StartCoroutine(GameOverSequence());
        }

        IEnumerator GameOverSequence()
        {
            yield return new WaitForSeconds(0.6f);

            // 위에 있는 과일부터 하나씩 터뜨린다.
            activeFruits.RemoveAll(f => f == null);
            activeFruits.Sort((x, y) => y.transform.position.y.CompareTo(x.transform.position.y));
            var toPop = new List<Fruit>(activeFruits);
            activeFruits.Clear();
            foreach (var f in toPop)
            {
                if (f == null) continue;
                var def = database.Get(f.Level);
                if (fx != null) fx.Burst(f.transform.position, def.color, def.radius);
                if (sfx != null) sfx.PlayGameOverPop();
                Destroy(f.gameObject);
                yield return new WaitForSeconds(0.05f);
            }

            yield return new WaitForSeconds(0.3f);
            if (hud != null) hud.ShowGameOver(score, bestScore, score > bestAtStart && score > 0);
            gameOverPanelShown = true;
        }
    }
}
