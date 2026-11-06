using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace SakuraRunner
{
    /// <summary>Code-built uGUI: main menu, HUD, pause menu and game-over screen in the pastel palette.</summary>
    public class UIManager : MonoBehaviour
    {
        Font font;
        Sprite round, pill, circle;
        Canvas canvas;

        CanvasGroup menuG, hudG, pauseG, overG;
        Text bestText, scoreText, flowerText, overScore, overFlowers, overBest, overTitle, hintText, musicLabel;
        RectTransform flowerPanel, titleRt;
        float flowerPunch;

        class PowerBar { public CanvasGroup G; public Image Fill; }
        PowerBar magnetBar, shieldBar, boostBar;

        // ------------------------------------------------------------------ setup
        public void Build()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 32);
            round = MakeRounded(64, 24, 0);
            pill = MakeRounded(64, 32, 0);
            circle = MakeRounded(64, 32, 0);

            var cg = new GameObject("Canvas");
            cg.transform.SetParent(transform, false);
            canvas = cg.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = cg.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            cg.AddComponent<GraphicRaycaster>();

            if (FindObjectOfType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.transform.SetParent(transform, false);
                es.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
                es.AddComponent<InputSystemUIInputModule>();
#else
                es.AddComponent<StandaloneInputModule>();
#endif
            }

            BuildMenu();
            BuildHud();
            BuildPause();
            BuildGameOver();

            var gm = GameManager.I;
            gm.StateChanged += OnState;
            gm.FlowerCollected += _ => flowerPunch = 1f;
            OnState(gm.State);
        }

        void OnState(GameState s)
        {
            target[0] = s == GameState.Menu ? 1f : 0f;
            target[1] = (s == GameState.Playing || s == GameState.Paused) ? 1f : 0f;
            target[2] = s == GameState.Paused ? 1f : 0f;
            target[3] = s == GameState.GameOver ? 1f : 0f;
            if (s == GameState.GameOver) FillGameOver();
            if (s == GameState.Menu) bestText.text = "Best  " + GameManager.I.Best;
        }

        readonly float[] target = new float[4];
        readonly float[] cur = new float[4];

        // ------------------------------------------------------------------ update
        void Update()
        {
            var gm = GameManager.I;
            if (gm == null) return;
            float dt = Time.unscaledDeltaTime;

            Fade(menuG, 0, dt); Fade(hudG, 1, dt); Fade(pauseG, 2, dt); Fade(overG, 3, dt);

            if (gm.State == GameState.Menu)
            {
                if (InputHelper.Up) { AudioManager.Play(Sfx.Click); gm.StartRun(); }
                float s = 1f + Mathf.Sin(Time.unscaledTime * 1.6f) * 0.025f;
                titleRt.localScale = Vector3.one * s;
                titleRt.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.unscaledTime * 0.9f) * 1.2f);
            }

            if (gm.State == GameState.Playing || gm.State == GameState.Paused)
            {
                scoreText.text = gm.Score.ToString("N0");
                flowerText.text = gm.Flowers.ToString();
                flowerPunch = Mathf.MoveTowards(flowerPunch, 0f, dt * 4f);
                flowerPanel.localScale = Vector3.one * (1f + 0.18f * flowerPunch);

                var p = gm.Player;
                if (p != null)
                {
                    Bar(magnetBar, p.MagnetT > 0f, p.MagnetT / PlayerController.MagnetDur);
                    Bar(shieldBar, p.Shield, 1f);
                    Bar(boostBar, p.BoostT > 0f, p.BoostT / PlayerController.BoostDur);
                }
            }
        }

        void Fade(CanvasGroup g, int i, float dt)
        {
            cur[i] = Mathf.MoveTowards(cur[i], target[i], dt * 4f);
            g.alpha = Mathf.SmoothStep(0, 1, cur[i]);
            bool on = target[i] > 0.5f;
            g.interactable = on;
            g.blocksRaycasts = on;
        }

        void Bar(PowerBar b, bool active, float frac)
        {
            b.G.alpha = Mathf.MoveTowards(b.G.alpha, active ? 1f : 0f, Time.unscaledDeltaTime * 6f);
            b.Fill.fillAmount = Mathf.Clamp01(frac);
        }

        // ------------------------------------------------------------------ screens
        void BuildMenu()
        {
            menuG = Screen("Menu");
            var root = menuG.transform;

            var title = Label(root, "Sakura Run", 150, Pal.Ink, TextAnchor.MiddleCenter, true);
            titleRt = title.rectTransform;
            Place(titleRt, new Vector2(0.5f, 0.68f), new Vector2(1400, 220));

            var sub = Label(root, "A petal-strewn dash through the village", 42, Pal.LavenderDeep, TextAnchor.MiddleCenter, false);
            Place(sub.rectTransform, new Vector2(0.5f, 0.56f), new Vector2(1200, 70));

            var play = Btn(root, "PLAY", new Vector2(0.5f, 0.40f), new Vector2(460, 130), Pal.PinkDeep, Color.white, 64, () => { gmStart(); });
            var mus = Btn(root, "", new Vector2(0.5f, 0.27f), new Vector2(380, 90), Pal.Lavender, Pal.Ink, 38, ToggleMusic);
            musicLabel = mus.GetComponentInChildren<Text>();
            RefreshMusicLabel();

            bestText = Label(root, "Best  0", 48, Pal.PeachDeep, TextAnchor.MiddleCenter, true);
            Place(bestText.rectTransform, new Vector2(0.5f, 0.17f), new Vector2(800, 80));

            hintText = Label(root, "A / D or Left / Right: change lane     W / Space / Up: jump     S / Down: slide     Esc: pause     (or swipe)", 32, Pal.Ink, TextAnchor.MiddleCenter, false);
            Place(hintText.rectTransform, new Vector2(0.5f, 0.07f), new Vector2(1700, 60));
        }

        void BuildHud()
        {
            hudG = Screen("HUD");
            var root = hudG.transform;

            // score (top-left)
            var sp = Panel(root, new Vector2(0, 1), new Vector2(40, -40), new Vector2(380, 110), new Color(1f, 0.95f, 0.97f, 0.85f));
            var cap = Label(sp, "SCORE", 26, Pal.LavenderDeep, TextAnchor.UpperLeft, false);
            Fill(cap.rectTransform, new Vector4(28, 0, 0, 10));
            scoreText = Label(sp, "0", 60, Pal.Ink, TextAnchor.LowerLeft, false);
            Fill(scoreText.rectTransform, new Vector4(28, 6, 0, 0));

            // flowers (top-right)
            var fp = Panel(root, new Vector2(1, 1), new Vector2(-40, -40), new Vector2(290, 110), new Color(1f, 0.95f, 0.97f, 0.85f));
            fp.pivot = new Vector2(1, 1);
            flowerPanel = fp;
            var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            icon.transform.SetParent(fp, false);
            icon.sprite = Fx.FlowerSprite();
            icon.preserveAspect = true;
            var irt = icon.rectTransform;
            irt.anchorMin = irt.anchorMax = new Vector2(0, 0.5f);
            irt.pivot = new Vector2(0, 0.5f);
            irt.anchoredPosition = new Vector2(22, 0);
            irt.sizeDelta = new Vector2(80, 80);
            flowerText = Label(fp, "0", 64, Pal.PinkDeep, TextAnchor.MiddleRight, false);
            Fill(flowerText.rectTransform, new Vector4(110, 0, 28, 0));

            // pause button
            var pb = Btn(root, "II", new Vector2(1, 1), new Vector2(96, 96), Pal.Lavender, Pal.Ink, 46, () => { AudioManager.Play(Sfx.Click); GameManager.I.Pause(); });
            pb.pivot = new Vector2(1, 1);
            pb.anchoredPosition = new Vector2(-40, -170);

            // power-up bars (top-centre)
            magnetBar = MakeBar(root, "MAGNET", Pal.Hex("C9A8FF"), -230f);
            shieldBar = MakeBar(root, "SHIELD", Pal.Sky, 0f);
            boostBar = MakeBar(root, "SPEED", Pal.PeachDeep, 230f);
        }

        PowerBar MakeBar(Transform parent, string name, Color c, float x)
        {
            var rt = Panel(parent, new Vector2(0.5f, 1), new Vector2(x, -50), new Vector2(210, 70), new Color(1f, 0.95f, 0.97f, 0.85f));
            rt.pivot = new Vector2(0.5f, 1);
            var g = rt.gameObject.AddComponent<CanvasGroup>();
            g.alpha = 0;
            var l = Label(rt, name, 24, Pal.Ink, TextAnchor.UpperCenter, false);
            Fill(l.rectTransform, new Vector4(0, 0, 0, 6));

            var bg = Img(rt, pill, new Color(c.r, c.g, c.b, 0.3f));
            var brt = bg.rectTransform;
            brt.anchorMin = new Vector2(0, 0); brt.anchorMax = new Vector2(1, 0);
            brt.pivot = new Vector2(0.5f, 0);
            brt.anchoredPosition = new Vector2(0, 12);
            brt.sizeDelta = new Vector2(-28, 16);

            var fill = Img(brt, null, c);
            fill.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillAmount = 1f;
            Fill(fill.rectTransform, Vector4.zero);
            return new PowerBar { G = g, Fill = fill };
        }

        void BuildPause()
        {
            pauseG = Screen("Pause");
            var dim = Img(pauseG.transform, null, new Color(0.30f, 0.24f, 0.41f, 0.55f));
            Fill(dim.rectTransform, Vector4.zero);

            var panel = Panel(pauseG.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(640, 700), new Color(1f, 0.95f, 0.97f, 0.97f));
            var t = Label(panel, "Paused", 90, Pal.Ink, TextAnchor.MiddleCenter, false);
            Place(t.rectTransform, new Vector2(0.5f, 0.85f), new Vector2(560, 120));
            Btn(panel, "RESUME", new Vector2(0.5f, 0.62f), new Vector2(420, 110), Pal.PinkDeep, Color.white, 50, () => { AudioManager.Play(Sfx.Click); GameManager.I.Resume(); });
            Btn(panel, "RESTART", new Vector2(0.5f, 0.42f), new Vector2(420, 110), Pal.Peach, Pal.Ink, 50, () => { AudioManager.Play(Sfx.Click); GameManager.I.StartRun(); });
            Btn(panel, "MAIN MENU", new Vector2(0.5f, 0.22f), new Vector2(420, 110), Pal.Lavender, Pal.Ink, 46, () => { AudioManager.Play(Sfx.Click); GameManager.I.EnterMenu(); });
        }

        void BuildGameOver()
        {
            overG = Screen("GameOver");
            var dim = Img(overG.transform, null, new Color(0.30f, 0.24f, 0.41f, 0.45f));
            Fill(dim.rectTransform, Vector4.zero);

            var panel = Panel(overG.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(780, 860), new Color(1f, 0.95f, 0.97f, 0.97f));
            overTitle = Label(panel, "Petals Fall...", 84, Pal.Ink, TextAnchor.MiddleCenter, false);
            Place(overTitle.rectTransform, new Vector2(0.5f, 0.89f), new Vector2(700, 120));

            var c1 = Label(panel, "FINAL SCORE", 32, Pal.LavenderDeep, TextAnchor.MiddleCenter, false);
            Place(c1.rectTransform, new Vector2(0.5f, 0.76f), new Vector2(600, 50));
            overScore = Label(panel, "0", 120, Pal.PinkDeep, TextAnchor.MiddleCenter, false);
            Place(overScore.rectTransform, new Vector2(0.5f, 0.67f), new Vector2(700, 150));

            var row = Label(panel, "FLOWERS COLLECTED", 32, Pal.LavenderDeep, TextAnchor.MiddleCenter, false);
            Place(row.rectTransform, new Vector2(0.5f, 0.55f), new Vector2(600, 50));
            overFlowers = Label(panel, "0", 76, Pal.Ink, TextAnchor.MiddleCenter, false);
            Place(overFlowers.rectTransform, new Vector2(0.5f, 0.48f), new Vector2(600, 100));

            overBest = Label(panel, "", 42, Pal.PeachDeep, TextAnchor.MiddleCenter, true);
            Place(overBest.rectTransform, new Vector2(0.5f, 0.38f), new Vector2(700, 70));

            Btn(panel, "RESTART", new Vector2(0.5f, 0.24f), new Vector2(460, 120), Pal.PinkDeep, Color.white, 54, () => { AudioManager.Play(Sfx.Click); GameManager.I.StartRun(); });
            Btn(panel, "MAIN MENU", new Vector2(0.5f, 0.09f), new Vector2(460, 110), Pal.Lavender, Pal.Ink, 46, () => { AudioManager.Play(Sfx.Click); GameManager.I.EnterMenu(); });
        }

        void FillGameOver()
        {
            var gm = GameManager.I;
            overScore.text = gm.Score.ToString("N0");
            overFlowers.text = gm.Flowers.ToString();
            overBest.text = gm.NewBest ? "* New best! *" : "Best  " + gm.Best.ToString("N0");
        }

        // ------------------------------------------------------------------ actions
        void gmStart() { AudioManager.Play(Sfx.Click); GameManager.I.StartRun(); }

        void ToggleMusic()
        {
            AudioManager.MusicVolume = AudioManager.MusicVolume > 0.01f ? 0f : 0.5f;
            AudioManager.Play(Sfx.Click);
            RefreshMusicLabel();
        }

        void RefreshMusicLabel() { if (musicLabel != null) musicLabel.text = AudioManager.MusicVolume > 0.01f ? "Music: On" : "Music: Off"; }

        // ------------------------------------------------------------------ widget helpers
        CanvasGroup Screen(string name)
        {
            var g = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup));
            g.transform.SetParent(canvas.transform, false);
            var rt = (RectTransform)g.transform;
            Fill(rt, Vector4.zero);
            var cg = g.GetComponent<CanvasGroup>();
            cg.alpha = 0;
            return cg;
        }

        static void Fill(RectTransform rt, Vector4 pad) // left, bottom, right, top
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(pad.x, pad.y);
            rt.offsetMax = new Vector2(-pad.z, -pad.w);
        }

        static void Place(RectTransform rt, Vector2 anchor, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = size;
        }

        Image Img(Transform parent, Sprite sprite, Color c)
        {
            var g = new GameObject("Image", typeof(RectTransform), typeof(Image));
            g.transform.SetParent(parent, false);
            var im = g.GetComponent<Image>();
            im.sprite = sprite;
            if (sprite != null) im.type = Image.Type.Sliced;
            im.color = c;
            im.raycastTarget = false;
            return im;
        }

        RectTransform Panel(Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, Color c)
        {
            var im = Img(parent, round, c);
            var rt = im.rectTransform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            if (anchor == new Vector2(0.5f, 0.5f)) rt.pivot = anchor;
            // soft outline for the anime "sticker" look
            var o = rt.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(Pal.Pink.r, Pal.Pink.g, Pal.Pink.b, 0.9f);
            o.effectDistance = new Vector2(4, -4);
            return rt;
        }

        Text Label(Transform parent, string text, int size, Color c, TextAnchor anchor, bool shadow)
        {
            var g = new GameObject("Text", typeof(RectTransform), typeof(Text));
            g.transform.SetParent(parent, false);
            var t = g.GetComponent<Text>();
            t.font = font;
            t.text = text;
            t.fontSize = size;
            t.fontStyle = FontStyle.Bold;
            t.color = c;
            t.alignment = anchor;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            if (shadow || size >= 100)
            {
                var s = g.AddComponent<Shadow>();
                s.effectColor = new Color(1f, 1f, 1f, 0.9f);
                s.effectDistance = new Vector2(size > 100 ? 6 : 3, size > 100 ? -6 : -3);
            }
            return t;
        }

        RectTransform Btn(Transform parent, string text, Vector2 anchor, Vector2 size, Color bg, Color fg, int fontSize, UnityEngine.Events.UnityAction onClick)
        {
            var im = Img(parent, pill, bg);
            im.raycastTarget = true;
            var rt = im.rectTransform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = size;

            var o = rt.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(1f, 1f, 1f, 0.8f);
            o.effectDistance = new Vector2(3, -3);

            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = im;
            var cb = b.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.06f, 1.06f, 1.06f);
            cb.pressedColor = new Color(0.82f, 0.78f, 0.88f);
            cb.fadeDuration = 0.08f;
            b.colors = cb;
            b.onClick.AddListener(onClick);

            var t = Label(rt, text, fontSize, fg, TextAnchor.MiddleCenter, false);
            Fill(t.rectTransform, Vector4.zero);
            return rt;
        }

        // 9-slice rounded rectangle texture
        static Sprite MakeRounded(int size, int radius, int unused)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float cx = Mathf.Clamp(x + 0.5f, radius, size - radius);
                    float cy = Mathf.Clamp(y + 0.5f, radius, size - radius);
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                    float a = Mathf.Clamp01(radius - d + 0.5f);
                    px[y * size + x] = new Color(1, 1, 1, a);
                }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        }
    }
}
