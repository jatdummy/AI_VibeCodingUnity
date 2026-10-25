using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Suika
{
    /// <summary>
    /// 점수, 최고 점수, 다음 과일, 진화 순서 원, 게임 오버 패널을 관리한다.
    /// </summary>
    public class SuikaHUD : MonoBehaviour
    {
        [SerializeField] Text scoreText;
        [SerializeField] Text bestText;
        [SerializeField] Image nextImage;
        [SerializeField] RectTransform evolutionPanel;
        [SerializeField] RectTransform evolutionRing;
        [SerializeField] GameObject gameOverPanel;
        [SerializeField] Text finalScoreText;
        [SerializeField] GameObject newBestLabel;
        [SerializeField] Button restartButton;

        [SerializeField] float evolutionRadius = 140f;
        [SerializeField] float evolutionMinIcon = 44f;
        [SerializeField] float evolutionMaxIcon = 92f;
        [SerializeField] float nextMinSize = 70f;
        [SerializeField] float nextMaxSize = 150f;

        SuikaGame game;
        Coroutine bumpRoutine;
        Vector3 scoreBaseScale = Vector3.one;
        bool lastPortrait;
        bool layoutInitialized;

        void Awake()
        {
            ApplyKoreanFont();
            if (scoreText != null) scoreBaseScale = scoreText.transform.localScale;
        }

        public void Init(SuikaGame owner)
        {
            game = owner;
            if (restartButton != null)
            {
                restartButton.onClick.RemoveAllListeners();
                restartButton.onClick.AddListener(() => { if (game != null) game.StartNewGame(); });
            }
            BuildEvolutionRing();
        }

        void Update()
        {
            // 세로 화면에서는 진화 원이 과일 상자를 가리지 않게 작게 줄인다.
            bool portrait = Screen.height > Screen.width;
            if (!layoutInitialized || portrait != lastPortrait)
            {
                lastPortrait = portrait;
                layoutInitialized = true;
                if (evolutionPanel != null) evolutionPanel.localScale = Vector3.one * (portrait ? 0.6f : 1f);
            }
        }

        /// <summary>
        /// 기본 폰트(LegacyRuntime)에는 한글이 없어서, 운영체제에 설치된 한글 폰트를 찾아 적용한다.
        /// </summary>
        void ApplyKoreanFont()
        {
            Font font = null;
            try
            {
                font = Font.CreateDynamicFontFromOSFont(
                    new[] { "Malgun Gothic", "맑은 고딕", "Apple SD Gothic Neo", "Noto Sans CJK KR", "NanumGothic", "Arial" }, 48);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[Suika] 한글 폰트를 불러오지 못했습니다: " + e.Message);
            }
            if (font == null) return;
            foreach (var t in GetComponentsInChildren<Text>(true)) t.font = font;
        }

        void BuildEvolutionRing()
        {
            if (evolutionRing == null || game == null || game.Database == null) return;
            for (int i = evolutionRing.childCount - 1; i >= 0; i--)
                Destroy(evolutionRing.GetChild(i).gameObject);

            var db = game.Database;
            int n = db.Count;
            for (int i = 0; i < n; i++)
            {
                var go = new GameObject($"Evo_{i:00}", typeof(RectTransform), typeof(Image));
                var rt = (RectTransform)go.transform;
                rt.SetParent(evolutionRing, false);
                // 12시 방향에서 시작해 시계 방향으로 작은 과일 → 큰 과일
                float angle = (90f - i * 360f / n) * Mathf.Deg2Rad;
                rt.anchoredPosition = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * evolutionRadius;
                float size = Mathf.Lerp(evolutionMinIcon, evolutionMaxIcon, n > 1 ? i / (float)(n - 1) : 0f);
                rt.sizeDelta = new Vector2(size, size);
                var img = go.GetComponent<Image>();
                img.sprite = db.Get(i).sprite;
                img.preserveAspect = true;
                img.raycastTarget = false;
            }
        }

        public void SetScore(int score, bool bump)
        {
            if (scoreText == null) return;
            scoreText.text = score.ToString("N0");
            if (!bump) return;
            if (bumpRoutine != null) StopCoroutine(bumpRoutine);
            bumpRoutine = StartCoroutine(Bump());
        }

        IEnumerator Bump()
        {
            const float duration = 0.18f;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / duration);
                scoreText.transform.localScale = scoreBaseScale * (1f + 0.18f * Mathf.Sin(k * Mathf.PI));
                yield return null;
            }
            scoreText.transform.localScale = scoreBaseScale;
            bumpRoutine = null;
        }

        public void SetBest(int best)
        {
            if (bestText != null) bestText.text = "최고 " + best.ToString("N0");
        }

        public void SetNext(FruitDef def)
        {
            if (nextImage == null || def == null) return;
            nextImage.sprite = def.sprite;
            nextImage.enabled = def.sprite != null;
            // 다음 과일 크기를 실제 크기 비율대로 보여 준다(0~4단계 기준).
            float k = Mathf.InverseLerp(0.26f, 0.70f, def.radius);
            float size = Mathf.Lerp(nextMinSize, nextMaxSize, k);
            nextImage.rectTransform.sizeDelta = new Vector2(size, size);
        }

        public void ShowGameOver(int score, int best, bool newBest)
        {
            if (gameOverPanel == null) return;
            gameOverPanel.SetActive(true);
            if (finalScoreText != null) finalScoreText.text = "점수 " + score.ToString("N0");
            if (newBestLabel != null) newBestLabel.SetActive(newBest);
        }

        public void HideGameOver()
        {
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
        }
    }
}
