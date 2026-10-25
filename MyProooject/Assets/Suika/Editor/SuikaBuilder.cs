using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Suika.EditorTools
{
    /// <summary>
    /// 메뉴 Tools > Suika > Build Game 한 번으로 과일 그림, 머티리얼, 프리팹, 씬, UI를 전부 만든다.
    /// 다시 실행하면 같은 경로의 에셋을 덮어써서 처음 상태로 재생성한다.
    /// </summary>
    public static class SuikaBuilder
    {
        const string Root = "Assets/Suika";
        const string ArtDir = Root + "/Art";
        const string DataDir = Root + "/Data";
        const string PrefabDir = Root + "/Prefabs";
        const string MatDir = Root + "/Materials";
        const string SceneDir = Root + "/Scenes";
        const string ScenePath = SceneDir + "/SuikaGame.unity";

        // 상자 크기 (SuikaGame / CameraFit 값과 맞춘다)
        const float InnerLeft = -3.3f;
        const float InnerRight = 3.3f;
        const float FloorY = -4.6f;
        const float DangerLineY = 3.3f;
        const float WallTop = 3.6f;
        const float SpawnY = 4.5f;
        const float WallThickness = 0.35f;

        enum Pattern { None, Seeds, Bubbles, Pores, Speckles, Crease, Crosshatch, Net, Stripes }
        enum Topper { None, Stem, Leaf, StemLeaf, Calyx, Crown }

        struct Spec
        {
            public string id, name;
            public Color color, accent;
            public float radius;
            public int score;
            public Pattern pattern;
            public Topper topper;

            public Spec(string id, string name, string hex, string accentHex, float radius, int score, Pattern p, Topper t)
            {
                this.id = id; this.name = name;
                color = Hex(hex); accent = Hex(accentHex);
                this.radius = radius; this.score = score;
                pattern = p; topper = t;
            }
        }

        static readonly Spec[] Specs =
        {
            new Spec("cherry",     "체리",     "E8323C", "B01E28", 0.26f,  1, Pattern.None,       Topper.Stem),
            new Spec("strawberry", "딸기",     "FF4D5A", "FFE48A", 0.36f,  3, Pattern.Seeds,      Topper.Calyx),
            new Spec("grape",      "포도",     "9257D6", "6A35AE", 0.48f,  6, Pattern.Bubbles,    Topper.Stem),
            new Spec("dekopon",    "한라봉",   "FFA92E", "E0861A", 0.58f, 10, Pattern.Pores,      Topper.Leaf),
            new Spec("persimmon",  "감",       "FF7F27", "D9611A", 0.70f, 15, Pattern.None,       Topper.Calyx),
            new Spec("apple",      "사과",     "EE3B36", "FFD1C2", 0.86f, 21, Pattern.Speckles,   Topper.StemLeaf),
            new Spec("pear",       "배",       "E9DD6A", "B79A3A", 0.98f, 28, Pattern.Speckles,   Topper.Stem),
            new Spec("peach",      "복숭아",   "FFB4C2", "FF7F9A", 1.14f, 36, Pattern.Crease,     Topper.Leaf),
            new Spec("pineapple",  "파인애플", "F6C640", "C98F1E", 1.30f, 45, Pattern.Crosshatch, Topper.Crown),
            new Spec("melon",      "멜론",     "9EDB6E", "E9FFD6", 1.56f, 55, Pattern.Net,        Topper.Stem),
            new Spec("watermelon", "수박",     "3DAF4A", "1E6B29", 1.82f, 66, Pattern.Stripes,    Topper.None),
        };

        [MenuItem("Tools/Suika/Build Game")]
        public static void BuildAll()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[Suika] 플레이 모드에서는 빌드할 수 없습니다. 플레이를 멈춘 뒤 다시 실행하세요.");
                return;
            }

            EnsureFolders();

            // 과일이 많이 쌓였을 때 겹침·떨림을 줄이기 위해 2D 물리 반복 횟수를 프로젝트 설정에 한 번 올려 둔다.
            Physics2D.velocityIterations = Mathf.Max(Physics2D.velocityIterations, 10);
            Physics2D.positionIterations = Mathf.Max(Physics2D.positionIterations, 8);

            Sprite[] fruitSprites = BuildSprites(out Sprite square, out Sprite dot, out Sprite roundRect);
            Material spriteMat = BuildSpriteMaterial();
            Material particleMat = BuildParticleMaterial(dot.texture);
            PhysicsMaterial2D fruitPhys = BuildPhysicsMaterial("FruitPhysics", 0.25f, 0.05f);
            PhysicsMaterial2D wallPhys = BuildPhysicsMaterial("WallPhysics", 0.4f, 0f);
            FruitDatabase db = BuildDatabase(fruitSprites);
            Fruit prefab = BuildFruitPrefab(spriteMat, fruitPhys, fruitSprites[0]);

            BuildScene(db, prefab, square, roundRect, spriteMat, particleMat, wallPhys);

            AssetDatabase.SaveAssets();
            Debug.Log("[Suika] 빌드 완료: " + ScenePath);
        }

        // ───────────── 폴더 ─────────────

        static void EnsureFolders()
        {
            foreach (var dir in new[] { ArtDir, DataDir, PrefabDir, MatDir, SceneDir })
            {
                if (AssetDatabase.IsValidFolder(dir)) continue;
                string parent = Path.GetDirectoryName(dir).Replace('\\', '/');
                AssetDatabase.CreateFolder(parent, Path.GetFileName(dir));
            }
        }

        static string FullPath(string assetPath)
        {
            return Path.Combine(Directory.GetParent(Application.dataPath).FullName, assetPath);
        }

        // ───────────── 스프라이트 ─────────────

        static Sprite[] BuildSprites(out Sprite square, out Sprite dot, out Sprite roundRect)
        {
            var fruitPaths = new string[Specs.Length];
            for (int i = 0; i < Specs.Length; i++)
            {
                fruitPaths[i] = $"{ArtDir}/fruit_{i:00}_{Specs[i].id}.png";
                WritePng(fruitPaths[i], DrawFruit(Specs[i]));
            }
            string squarePath = ArtDir + "/square.png";
            string dotPath = ArtDir + "/dot.png";
            string roundPath = ArtDir + "/roundrect.png";
            WritePng(squarePath, DrawSquare());
            WritePng(dotPath, DrawDot());
            WritePng(roundPath, DrawRoundRect(64, 22));

            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            var sprites = new Sprite[Specs.Length];
            for (int i = 0; i < Specs.Length; i++)
            {
                ConfigureSprite(fruitPaths[i], 128f, Vector4.zero);
                sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(fruitPaths[i]);
            }
            ConfigureSprite(squarePath, 8f, Vector4.zero);
            ConfigureSprite(dotPath, 64f, Vector4.zero);
            ConfigureSprite(roundPath, 64f, new Vector4(22, 22, 22, 22));
            square = AssetDatabase.LoadAssetAtPath<Sprite>(squarePath);
            dot = AssetDatabase.LoadAssetAtPath<Sprite>(dotPath);
            roundRect = AssetDatabase.LoadAssetAtPath<Sprite>(roundPath);
            return sprites;
        }

        static void WritePng(string assetPath, Texture2D tex)
        {
            File.WriteAllBytes(FullPath(assetPath), tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        static void ConfigureSprite(string path, float ppu, Vector4 border)
        {
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = ppu;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = false;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.filterMode = FilterMode.Bilinear;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.spriteBorder = border;
            ti.SaveAndReimport();
        }

        static Texture2D NewTex(int w, int h)
        {
            return new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        }

        static Texture2D DrawSquare()
        {
            var tex = NewTex(8, 8);
            var px = new Color[64];
            for (int i = 0; i < px.Length; i++) px[i] = Color.white;
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        static Texture2D DrawDot()
        {
            const int s = 64;
            var tex = NewTex(s, s);
            var px = new Color[s * s];
            float c = (s - 1) * 0.5f;
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / (s * 0.5f);
                    float a = Mathf.Clamp01((1f - d) * 3f);
                    px[y * s + x] = new Color(1f, 1f, 1f, a);
                }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        static Texture2D DrawRoundRect(int size, int radius)
        {
            var tex = NewTex(size, size);
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float fx = x + 0.5f, fy = y + 0.5f;
                    float cx = Mathf.Clamp(fx, radius, size - radius);
                    float cy = Mathf.Clamp(fy, radius, size - radius);
                    float d = Mathf.Sqrt((fx - cx) * (fx - cx) + (fy - cy) * (fy - cy));
                    float a = Mathf.Clamp01(radius - d + 0.5f);
                    px[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        // ───────────── 과일 그림 (코드로 직접 그림) ─────────────

        const int TexSize = 256;
        const float CircleR = 124f; // 256px 텍스처, PPU 128 → 반지름 0.97 단위 (Fruit.VisualRadius)

        static Texture2D DrawFruit(Spec s)
        {
            var tex = NewTex(TexSize, TexSize);
            var px = new Color[TexSize * TexSize];
            float c = (TexSize - 1) * 0.5f;
            Color dark = Mul(s.color, 0.72f);
            Color light = Color.Lerp(s.color, Color.white, 0.28f);
            Color rimCol = Mul(s.color, 0.55f);

            for (int y = 0; y < TexSize; y++)
            {
                for (int x = 0; x < TexSize; x++)
                {
                    float dx = x - c, dy = y - c;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float cov = Mathf.Clamp01(CircleR - d + 0.5f);
                    if (cov <= 0f) { px[y * TexSize + x] = Color.clear; continue; }

                    float u = dx / CircleR, v = dy / CircleR;

                    // 왼쪽 위에서 빛이 오는 입체감
                    float lightAmt = Mathf.Clamp01(0.55f + 0.5f * (-u * 0.5f + v * 0.6f) - 0.25f * (u * u + v * v));
                    Color col = Color.Lerp(dark, light, Mathf.SmoothStep(0f, 1f, lightAmt));

                    col = ApplyPattern(s, col, u, v);
                    col = ApplyTopper(s, col, u, v);
                    col = ApplyFace(col, u, v);

                    // 테두리
                    float rim = Mathf.InverseLerp(CircleR - 7f, CircleR - 1.5f, d);
                    col = Color.Lerp(col, rimCol, rim * 0.9f);

                    col.a = cov;
                    px[y * TexSize + x] = col;
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        static Color ApplyPattern(Spec s, Color col, float u, float v)
        {
            float aa = 1.5f / CircleR; // 약 1.5px 폭의 부드러운 경계
            switch (s.pattern)
            {
                case Pattern.Seeds:
                {
                    if (v > 0.55f) break;
                    float cell = 0.24f;
                    float row = Mathf.Floor(v / cell);
                    float gu = u / cell + (Mod(row, 2f) * 0.5f);
                    float lu = Frac(gu) - 0.5f, lv = Frac(v / cell) - 0.5f;
                    float e = Mathf.Sqrt((lu / 0.11f) * (lu / 0.11f) + (lv / 0.17f) * (lv / 0.17f));
                    col = Color.Lerp(col, s.accent, Mathf.Clamp01((1f - e) * 4f) * 0.9f);
                    break;
                }
                case Pattern.Bubbles:
                {
                    float cell = 0.42f;
                    float row = Mathf.Floor(v / cell);
                    float gu = u / cell + (Mod(row, 2f) * 0.5f);
                    float lu = Frac(gu) - 0.5f, lv = Frac(v / cell) - 0.5f;
                    float dd = Mathf.Sqrt(lu * lu + lv * lv);
                    col = Color.Lerp(col, s.accent, Mathf.Clamp01((dd - 0.36f) * 8f) * 0.55f);
                    float hl = Mathf.Sqrt((lu + 0.14f) * (lu + 0.14f) + (lv - 0.14f) * (lv - 0.14f));
                    col = Color.Lerp(col, Color.white, Mathf.Clamp01((0.08f - hl) * 30f) * 0.5f);
                    break;
                }
                case Pattern.Pores:
                {
                    float cell = 0.1f;
                    float cu = Mathf.Floor(u / cell), cv = Mathf.Floor(v / cell);
                    if (Hash(cu, cv) > 0.4f) break;
                    float lu = Frac(u / cell) - 0.5f, lv = Frac(v / cell) - 0.5f;
                    float dd = Mathf.Sqrt(lu * lu + lv * lv);
                    col = Color.Lerp(col, s.accent, Mathf.Clamp01((0.2f - dd) * 12f) * 0.45f);
                    break;
                }
                case Pattern.Speckles:
                {
                    float cell = 0.14f;
                    float cu = Mathf.Floor(u / cell), cv = Mathf.Floor(v / cell);
                    if (Hash(cu, cv) > 0.45f) break;
                    float lu = Frac(u / cell) - 0.5f, lv = Frac(v / cell) - 0.5f;
                    float dd = Mathf.Sqrt(lu * lu + lv * lv);
                    col = Color.Lerp(col, s.accent, Mathf.Clamp01((0.13f - dd) * 14f) * 0.6f);
                    break;
                }
                case Pattern.Crease:
                {
                    // 오른쪽 아래로 갈수록 더 진한 분홍
                    float blush = Mathf.Clamp01((u - v) * 0.45f + 0.15f);
                    col = Color.Lerp(col, s.accent, blush * 0.45f);
                    if (v > -0.8f && v < 0.85f)
                    {
                        float curve = -0.12f + 0.08f * Mathf.Sin(v * 2.4f);
                        float dist = Mathf.Abs(u - curve);
                        col = Color.Lerp(col, Mul(s.accent, 0.85f), Mathf.Clamp01((0.025f - dist) / aa + 0.5f) * 0.5f);
                    }
                    break;
                }
                case Pattern.Crosshatch:
                {
                    float a = (u + v) / 0.3f, b = (u - v) / 0.3f;
                    float la = Mathf.Abs(Frac(a) - 0.5f), lb = Mathf.Abs(Frac(b) - 0.5f);
                    float line = Mathf.Max(Mathf.Clamp01((la - 0.43f) * 25f), Mathf.Clamp01((lb - 0.43f) * 25f));
                    col = Color.Lerp(col, s.accent, line * 0.8f);
                    float center = Mathf.Sqrt(la * la + lb * lb);
                    col = Color.Lerp(col, Color.Lerp(s.color, Color.white, 0.4f), Mathf.Clamp01((0.12f - center) * 15f) * 0.6f);
                    break;
                }
                case Pattern.Net:
                {
                    float n1 = Mathf.Abs(Mathf.Sin((u * 1.0f + v * 0.8f) * 11f + Mathf.Sin(v * 7f) * 0.8f));
                    float n2 = Mathf.Abs(Mathf.Sin((-u * 0.9f + v * 1.0f) * 11f + Mathf.Sin(u * 6f) * 0.8f));
                    float n = Mathf.Min(n1, n2);
                    col = Color.Lerp(col, s.accent, Mathf.Clamp01((0.14f - n) * 18f) * 0.75f);
                    break;
                }
                case Pattern.Stripes:
                {
                    float st = Mathf.Sin((u + 0.05f * Mathf.Sin(v * 20f)) * 9f);
                    col = Color.Lerp(col, s.accent, Mathf.Clamp01((st - 0.3f) * 6f) * 0.9f);
                    break;
                }
            }
            return col;
        }

        static Color ApplyTopper(Spec s, Color col, float u, float v)
        {
            Color brown = Hex("6B4226");
            Color green = Hex("4CAF50");
            Color darkGreen = Hex("2E7D32");
            switch (s.topper)
            {
                case Topper.Stem:
                    col = Blend(col, brown, Segment(u, v, 0f, 0.62f, 0.12f, 0.95f, 0.045f));
                    break;
                case Topper.Leaf:
                    col = Blend(col, green, Ellipse(u, v, 0.2f, 0.8f, 0.22f, 0.09f, 25f));
                    col = Blend(col, darkGreen, Segment(u, v, 0.02f, 0.72f, 0.36f, 0.88f, 0.012f) * 0.7f);
                    break;
                case Topper.StemLeaf:
                    col = Blend(col, brown, Segment(u, v, 0f, 0.62f, 0.06f, 0.95f, 0.04f));
                    col = Blend(col, green, Ellipse(u, v, 0.25f, 0.83f, 0.2f, 0.08f, 20f));
                    break;
                case Topper.Calyx:
                    for (int k = 0; k < 5; k++)
                    {
                        float ang = 90f + k * 72f;
                        float rad = ang * Mathf.Deg2Rad;
                        float cx = Mathf.Cos(rad) * 0.11f, cy = 0.78f + Mathf.Sin(rad) * 0.06f;
                        col = Blend(col, green, Ellipse(u, v, cx, cy, 0.15f, 0.05f, ang));
                    }
                    col = Blend(col, darkGreen, Ellipse(u, v, 0f, 0.79f, 0.05f, 0.05f, 0f));
                    break;
                case Topper.Crown:
                    for (int k = 0; k < 5; k++)
                    {
                        float ang = 55f + k * 17.5f;
                        float rad = ang * Mathf.Deg2Rad;
                        float cx = Mathf.Cos(rad) * 0.2f, cy = 0.68f + Mathf.Sin(rad) * 0.16f;
                        Color leaf = k % 2 == 0 ? green : darkGreen;
                        col = Blend(col, leaf, Ellipse(u, v, cx, cy, 0.22f, 0.065f, ang));
                    }
                    break;
            }
            return col;
        }

        static Color ApplyFace(Color col, float u, float v)
        {
            Color ink = Hex("2B2B2B");
            Color blush = Hex("FF6F91");
            // 하이라이트
            col = Blend(col, Color.white, Ellipse(u, v, -0.45f, 0.42f, 0.2f, 0.1f, 40f) * 0.5f);
            // 볼터치
            col = Blend(col, blush, Ellipse(u, v, -0.44f, -0.14f, 0.11f, 0.06f, 0f) * 0.35f);
            col = Blend(col, blush, Ellipse(u, v, 0.44f, -0.14f, 0.11f, 0.06f, 0f) * 0.35f);
            // 눈
            col = Blend(col, ink, Ellipse(u, v, -0.25f, 0.02f, 0.065f, 0.085f, 0f));
            col = Blend(col, ink, Ellipse(u, v, 0.25f, 0.02f, 0.065f, 0.085f, 0f));
            col = Blend(col, Color.white, Ellipse(u, v, -0.27f, 0.05f, 0.025f, 0.025f, 0f));
            col = Blend(col, Color.white, Ellipse(u, v, 0.23f, 0.05f, 0.025f, 0.025f, 0f));
            // 입 (아래쪽 반원 호)
            float mcx = 0f, mcy = -0.06f, mr = 0.11f;
            if (v < mcy - 0.02f)
            {
                float md = Mathf.Abs(Mathf.Sqrt((u - mcx) * (u - mcx) + (v - mcy) * (v - mcy)) - mr) * CircleR;
                col = Blend(col, ink, Mathf.Clamp01(2.2f - md));
            }
            return col;
        }

        // 회전된 타원 내부 정도(0~1, 경계는 부드럽게)
        static float Ellipse(float u, float v, float cx, float cy, float rx, float ry, float angleDeg)
        {
            float a = -angleDeg * Mathf.Deg2Rad;
            float px = u - cx, py = v - cy;
            float rx2 = px * Mathf.Cos(a) - py * Mathf.Sin(a);
            float ry2 = px * Mathf.Sin(a) + py * Mathf.Cos(a);
            float e = Mathf.Sqrt((rx2 / rx) * (rx2 / rx) + (ry2 / ry) * (ry2 / ry));
            float edgePx = Mathf.Min(rx, ry) * CircleR;
            return Mathf.Clamp01((1f - e) * edgePx + 0.5f);
        }

        // 선분(두께 width) 내부 정도
        static float Segment(float u, float v, float x0, float y0, float x1, float y1, float width)
        {
            Vector2 p = new Vector2(u, v), a = new Vector2(x0, y0), b = new Vector2(x1, y1);
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            float dist = Vector2.Distance(p, a + ab * t);
            return Mathf.Clamp01((width - dist) * CircleR + 0.5f);
        }

        static Color Blend(Color baseCol, Color over, float a)
        {
            var c = Color.Lerp(baseCol, over, Mathf.Clamp01(a));
            c.a = baseCol.a;
            return c;
        }

        static Color Mul(Color c, float k) => new Color(c.r * k, c.g * k, c.b * k, c.a);
        static float Frac(float x) => x - Mathf.Floor(x);
        static float Mod(float x, float m) => x - m * Mathf.Floor(x / m);
        static float Hash(float x, float y) => Frac(Mathf.Sin(x * 127.1f + y * 311.7f) * 43758.5453f);

        static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var c);
            return c;
        }

        // ───────────── 머티리얼 / 물리 / 데이터 ─────────────

        static Shader SpriteShader()
        {
            return Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ?? Shader.Find("Sprites/Default");
        }

        static Material BuildSpriteMaterial()
        {
            string path = MatDir + "/SpriteUnlit.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(SpriteShader());
                AssetDatabase.CreateAsset(mat, path);
            }
            else mat.shader = SpriteShader();
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static Material BuildParticleMaterial(Texture2D dotTex)
        {
            string path = MatDir + "/MergeParticle.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(SpriteShader());
                AssetDatabase.CreateAsset(mat, path);
            }
            else mat.shader = SpriteShader();
            mat.SetTexture("_MainTex", dotTex);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static PhysicsMaterial2D BuildPhysicsMaterial(string name, float friction, float bounciness)
        {
            string path = $"{MatDir}/{name}.physicsMaterial2D";
            var pm = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(path);
            if (pm == null)
            {
                pm = new PhysicsMaterial2D(name);
                AssetDatabase.CreateAsset(pm, path);
            }
            pm.friction = friction;
            pm.bounciness = bounciness;
            EditorUtility.SetDirty(pm);
            return pm;
        }

        static FruitDatabase BuildDatabase(Sprite[] sprites)
        {
            string path = DataDir + "/FruitDatabase.asset";
            var db = AssetDatabase.LoadAssetAtPath<FruitDatabase>(path);
            if (db == null)
            {
                db = ScriptableObject.CreateInstance<FruitDatabase>();
                AssetDatabase.CreateAsset(db, path);
            }
            db.fruits = new FruitDef[Specs.Length];
            for (int i = 0; i < Specs.Length; i++)
            {
                db.fruits[i] = new FruitDef
                {
                    displayName = Specs[i].name,
                    radius = Specs[i].radius,
                    color = Specs[i].color,
                    score = Specs[i].score,
                    sprite = sprites[i],
                };
            }
            db.spawnableCount = 5;
            EditorUtility.SetDirty(db);
            return db;
        }

        static Fruit BuildFruitPrefab(Material spriteMat, PhysicsMaterial2D fruitPhys, Sprite defaultSprite)
        {
            string path = PrefabDir + "/Fruit.prefab";
            var go = new GameObject("Fruit");
            // 그림은 자식에 둔다: 합치기 연출로 그림만 키우고 물리 몸체(루트) 크기는 고정하기 위해서.
            var visualGo = new GameObject("Visual");
            visualGo.transform.SetParent(go.transform, false);
            var sr = visualGo.AddComponent<SpriteRenderer>();
            sr.sprite = defaultSprite;
            sr.sharedMaterial = spriteMat;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 1.6f;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.sleepMode = RigidbodySleepMode2D.NeverSleep; // 붙어 있는 같은 과일의 Stay 이벤트가 끊기지 않게
            rb.linearDamping = 0.05f;
            rb.angularDamping = 0.6f;
            rb.sharedMaterial = fruitPhys;

            // useAutoMass를 먼저 켜야 콜라이더 density 설정이 적용된다(순서가 바뀌면 경고 발생).
            rb.useAutoMass = true; // 큰 과일일수록 무겁게
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = Fruit.VisualRadius;
            col.density = 1f;

            go.AddComponent<Fruit>();

            var saved = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return saved.GetComponent<Fruit>();
        }

        // ───────────── 씬 ─────────────

        static void BuildScene(FruitDatabase db, Fruit prefab, Sprite square, Sprite roundRect,
            Material spriteMat, Material particleMat, PhysicsMaterial2D wallPhys)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 카메라
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            camGo.transform.position = new Vector3(0f, 0f, -10f);
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6.5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Hex("F7E9C8");
            camGo.AddComponent<AudioListener>();
            var camFit = camGo.AddComponent<CameraFit>();

            // 상자
            float innerW = InnerRight - InnerLeft;
            float boxH = WallTop - FloorY;
            var container = new GameObject("Container").transform;

            MakeSprite("Background", container, square, spriteMat, Hex("F2D49B"),
                new Vector2(0f, (FloorY + WallTop) * 0.5f), new Vector2(innerW, boxH), -10);
            MakeSprite("WallLeft", container, square, spriteMat, Hex("C98A4B"),
                new Vector2(InnerLeft - WallThickness * 0.5f, (FloorY - WallThickness + WallTop) * 0.5f),
                new Vector2(WallThickness, boxH + WallThickness), 5);
            MakeSprite("WallRight", container, square, spriteMat, Hex("C98A4B"),
                new Vector2(InnerRight + WallThickness * 0.5f, (FloorY - WallThickness + WallTop) * 0.5f),
                new Vector2(WallThickness, boxH + WallThickness), 5);
            var floor = MakeSprite("Floor", container, square, spriteMat, Hex("B57A3E"),
                new Vector2(0f, FloorY - WallThickness * 0.5f), new Vector2(innerW + WallThickness * 2f, WallThickness), 6);
            // 보이는 바닥은 얇지만 충돌용 바닥은 두껍게 따로 둔다. 얇으면 무거운 과일에 눌린 작은 과일이 파고들거나 빠져나간다.
            MakeBarrier("FloorCollider", container, new Vector2(0f, FloorY - 1.5f), new Vector2(innerW + 4f, 3f), wallPhys);

            // 보이지 않는 높은 벽: 과일이 상자 위로 쌓여도 옆으로 빠져나가지 않게 한다.
            float barrierH = 30f;
            MakeBarrier("BarrierLeft", container, new Vector2(InnerLeft - 0.5f, FloorY + barrierH * 0.5f - 1f), new Vector2(1f, barrierH), wallPhys);
            MakeBarrier("BarrierRight", container, new Vector2(InnerRight + 0.5f, FloorY + barrierH * 0.5f - 1f), new Vector2(1f, barrierH), wallPhys);

            var danger = MakeSprite("DangerLine", container, square, spriteMat, new Color(1f, 0.3f, 0.3f, 0.12f),
                new Vector2(0f, DangerLineY), new Vector2(innerW, 0.06f), 50);
            var guide = MakeSprite("GuideLine", container, square, spriteMat, new Color(1f, 1f, 1f, 0.45f),
                new Vector2(0f, 0f), new Vector2(0.05f, 1f), 1);

            var fruits = new GameObject("Fruits").transform;

            // 이펙트
            var fxGo = new GameObject("MergeFx");
            var ps = fxGo.AddComponent<ParticleSystem>();
            var psMain = ps.main;
            psMain.playOnAwake = false;
            var psr = fxGo.GetComponent<ParticleSystemRenderer>();
            psr.sharedMaterial = particleMat;
            psr.sortingOrder = 100;
            var fx = fxGo.AddComponent<SuikaFx>();

            // 사운드
            var audioGo = new GameObject("Audio");
            var sfx = audioGo.AddComponent<SuikaAudio>();

            // UI
            var hud = BuildUI(roundRect);

            // 게임
            var gameGo = new GameObject("SuikaGame");
            var game = gameGo.AddComponent<SuikaGame>();
            var so = new SerializedObject(game);
            so.FindProperty("database").objectReferenceValue = db;
            // 저장 직후 받은 참조는 재임포트로 무효화될 수 있어서, 경로로 다시 불러와 연결한다.
            AssetDatabase.SaveAssets();
            var prefabGo = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/Fruit.prefab");
            var prefabFruit = prefabGo != null ? prefabGo.GetComponent<Fruit>() : prefab;
            so.FindProperty("fruitPrefab").objectReferenceValue = prefabFruit;
            if (prefabFruit == null) Debug.LogError("[Suika] Fruit 프리팹을 찾지 못했습니다.");
            so.FindProperty("fruitRoot").objectReferenceValue = fruits;
            so.FindProperty("innerLeft").floatValue = InnerLeft;
            so.FindProperty("innerRight").floatValue = InnerRight;
            so.FindProperty("floorY").floatValue = FloorY;
            so.FindProperty("spawnY").floatValue = SpawnY;
            so.FindProperty("dangerLineY").floatValue = DangerLineY;
            // 필드 타입이 SpriteRenderer라서 Transform을 넣으면 오류 없이 null로 저장된다. 컴포넌트를 꺼내서 넣는다.
            so.FindProperty("dangerLine").objectReferenceValue = danger.GetComponent<SpriteRenderer>();
            so.FindProperty("guideLine").objectReferenceValue = guide.GetComponent<SpriteRenderer>();
            so.FindProperty("hud").objectReferenceValue = hud;
            so.FindProperty("sfx").objectReferenceValue = sfx;
            so.FindProperty("fx").objectReferenceValue = fx;
            so.FindProperty("cameraFit").objectReferenceValue = camFit;
            so.ApplyModifiedPropertiesWithoutUndo();
            ValidateReferences(game);

            var camSo = new SerializedObject(camFit);
            float areaBottom = FloorY - WallThickness - 1.6f;
            float areaTop = SpawnY + 1.6f;
            camSo.FindProperty("worldArea").rectValue = new Rect(InnerLeft - WallThickness - 0.65f, areaBottom,
                innerW + (WallThickness + 0.65f) * 2f, areaTop - areaBottom);
            camSo.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);

            // 빌드 설정에 씬 추가(중복 방지)
            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!list.Exists(s => s.path == ScenePath))
            {
                list.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
                EditorBuildSettings.scenes = list.ToArray();
            }
        }

        /// <summary>
        /// 연결되지 않은(null) 오브젝트 참조 필드가 있으면 에러로 알려 준다.
        /// SerializedProperty에 타입이 다른 값을 넣으면 조용히 null이 되기 때문에 빌드 직후 검사한다.
        /// </summary>
        static void ValidateReferences(Object target)
        {
            var so = new SerializedObject(target);
            var it = so.GetIterator();
            bool enterChildren = true;
            while (it.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (it.propertyType != SerializedPropertyType.ObjectReference || it.name == "m_Script") continue;
                if (it.objectReferenceValue == null)
                    Debug.LogError($"[Suika] {target.GetType().Name}.{it.name} 참조가 비어 있습니다.");
            }
        }

        static Transform MakeSprite(string name, Transform parent, Sprite sprite, Material mat, Color color,
            Vector2 pos, Vector2 scale, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = new Vector3(scale.x, scale.y, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sharedMaterial = mat;
            sr.color = color;
            sr.sortingOrder = order;
            return go.transform;
        }

        static void AddBox(GameObject go, PhysicsMaterial2D mat)
        {
            var box = go.AddComponent<BoxCollider2D>();
            box.size = Vector2.one;
            box.sharedMaterial = mat;
        }

        static void MakeBarrier(string name, Transform parent, Vector2 pos, Vector2 size, PhysicsMaterial2D mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var box = go.AddComponent<BoxCollider2D>();
            box.size = size;
            box.sharedMaterial = mat;
        }

        // ───────────── UI ─────────────

        static Font DefaultFont() => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        static SuikaHUD BuildUI(Sprite roundRect)
        {
            var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            es.transform.SetAsLastSibling();
            // 코드로 추가하면 기본 입력 액션이 비어 있을 수 있어 직접 지정한다(버튼 클릭이 안 먹는 문제 방지).
            var uiModule = es.GetComponent<InputSystemUIInputModule>();
            if (uiModule.actionsAsset == null) uiModule.AssignDefaultActions();

            Color panelCol = new Color(1f, 0.965f, 0.89f, 0.92f);
            Color brown = Hex("5A3415");
            Color softBrown = Hex("9A6A3C");

            // 점수 패널 (왼쪽 위)
            var scorePanel = MakeRect("ScorePanel", canvasGo.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(36, -36), new Vector2(380, 270));
            AddImage(scorePanel.gameObject, roundRect, panelCol);
            MakeText("Label", scorePanel, "점수", 40, softBrown, TextAnchor.MiddleCenter, FontStyle.Bold, -16, 50);
            var scoreText = MakeText("Score", scorePanel, "0", 96, brown, TextAnchor.MiddleCenter, FontStyle.Bold, -70, 110);
            var bestText = MakeText("Best", scorePanel, "최고 0", 38, softBrown, TextAnchor.MiddleCenter, FontStyle.Normal, -205, 46);

            // 다음 과일 (오른쪽 위)
            var nextPanel = MakeRect("NextPanel", canvasGo.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1),
                new Vector2(-36, -36), new Vector2(260, 270));
            AddImage(nextPanel.gameObject, roundRect, panelCol);
            MakeText("Label", nextPanel, "다음", 40, softBrown, TextAnchor.MiddleCenter, FontStyle.Bold, -16, 50);
            var nextRt = MakeRect("NextFruit", nextPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, -28), new Vector2(150, 150));
            var nextImg = nextRt.gameObject.AddComponent<Image>();
            nextImg.preserveAspect = true;
            nextImg.raycastTarget = false;

            // 진화 순서 (오른쪽 아래)
            var evoPanel = MakeRect("EvolutionPanel", canvasGo.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0),
                new Vector2(-30, 30), new Vector2(400, 400));
            AddImage(evoPanel.gameObject, roundRect, new Color(1f, 0.965f, 0.89f, 0.6f));
            var ring = MakeRect("Ring", evoPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(10, 10));

            // 게임 오버
            var over = MakeRect("GameOverPanel", canvasGo.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            AddImage(over.gameObject, null, new Color(0f, 0f, 0f, 0.55f));
            var box = MakeRect("Box", over, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(760, 640));
            AddImage(box.gameObject, roundRect, new Color(1f, 0.965f, 0.89f, 1f));
            MakeCenteredText("Title", box, "게임 오버", 110, Hex("D9483B"), FontStyle.Bold, 190, 140);
            var finalScore = MakeCenteredText("FinalScore", box, "점수 0", 72, brown, FontStyle.Bold, 40, 100);
            var newBest = MakeCenteredText("NewBest", box, "최고 기록 갱신!", 50, Hex("E8A317"), FontStyle.Bold, -60, 70);

            var btnRt = MakeRect("RestartButton", box, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, -200), new Vector2(440, 140));
            var btnImg = AddImage(btnRt.gameObject, roundRect, Hex("5DBB63"));
            btnImg.raycastTarget = true;
            var btn = btnRt.gameObject.AddComponent<Button>();
            btn.targetGraphic = btnImg;
            MakeCenteredText("Text", btnRt, "다시 하기", 60, Color.white, FontStyle.Bold, 0, 120);
            over.gameObject.SetActive(false);

            var hud = canvasGo.AddComponent<SuikaHUD>();
            var so = new SerializedObject(hud);
            so.FindProperty("scoreText").objectReferenceValue = scoreText;
            so.FindProperty("bestText").objectReferenceValue = bestText;
            so.FindProperty("nextImage").objectReferenceValue = nextImg;
            so.FindProperty("evolutionPanel").objectReferenceValue = evoPanel;
            so.FindProperty("evolutionRing").objectReferenceValue = ring;
            so.FindProperty("gameOverPanel").objectReferenceValue = over.gameObject;
            so.FindProperty("finalScoreText").objectReferenceValue = finalScore;
            so.FindProperty("newBestLabel").objectReferenceValue = newBest.gameObject;
            so.FindProperty("restartButton").objectReferenceValue = btn;
            so.ApplyModifiedPropertiesWithoutUndo();
            ValidateReferences(hud);
            return hud;
        }

        static RectTransform MakeRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchoredPos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            return rt;
        }

        static Image AddImage(GameObject go, Sprite sprite, Color color)
        {
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.type = sprite != null && sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            img.raycastTarget = false;
            return img;
        }

        // 부모 위쪽 기준으로 가로를 꽉 채우는 텍스트
        static Text MakeText(string name, Transform parent, string content, int size, Color color, TextAnchor align,
            FontStyle style, float topOffset, float height)
        {
            var rt = MakeRect(name, parent, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1),
                new Vector2(0, topOffset), new Vector2(0, height));
            return SetupText(rt.gameObject, content, size, color, align, style);
        }

        static Text MakeCenteredText(string name, Transform parent, string content, int size, Color color, FontStyle style,
            float y, float height)
        {
            var rt = MakeRect(name, parent, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, y), new Vector2(0, height));
            return SetupText(rt.gameObject, content, size, color, TextAnchor.MiddleCenter, style);
        }

        static Text SetupText(GameObject go, string content, int size, Color color, TextAnchor align, FontStyle style)
        {
            var t = go.AddComponent<Text>();
            t.font = DefaultFont();
            t.text = content;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.fontStyle = style;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }
    }
}
