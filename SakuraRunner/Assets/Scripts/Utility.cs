using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SakuraRunner
{
    /// <summary>Pastel pink / lavender / peach palette.</summary>
    public static class Pal
    {
        public static Color Hex(string h)
        {
            ColorUtility.TryParseHtmlString(h.StartsWith("#") ? h : "#" + h, out var c);
            return c;
        }

        public static readonly Color Pink = Hex("FFB7D5");
        public static readonly Color PinkDeep = Hex("FF8FBF");
        public static readonly Color PinkLight = Hex("FFDDEA");
        public static readonly Color Lavender = Hex("CDB4F0");
        public static readonly Color LavenderDeep = Hex("9D86D6");
        public static readonly Color Peach = Hex("FFD3B6");
        public static readonly Color PeachDeep = Hex("FFAE8F");
        public static readonly Color Cream = Hex("FFF3E8");
        public static readonly Color Wood = Hex("C99A82");
        public static readonly Color Bark = Hex("7D5A66");
        public static readonly Color Stone = Hex("BDB3CF");
        public static readonly Color Coral = Hex("EF7F86");
        public static readonly Color Ink = Hex("4B3F68");
        public static readonly Color Skin = Hex("FFE2D3");
        public static readonly Color Mint = Hex("BFEBE0");
        public static readonly Color Sky = Hex("9ADCF0");
    }

    /// <summary>Cached materials that work in Built-in, URP (and fall back gracefully).</summary>
    public static class Mats
    {
        static Shader _lit, _sprite;
        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();

        static Shader LitShader
        {
            get
            {
                if (_lit == null) _lit = Shader.Find("Universal Render Pipeline/Lit");
                if (_lit == null) _lit = Shader.Find("Standard");
                if (_lit == null) _lit = Shader.Find("Sprites/Default");
                return _lit;
            }
        }

        public static Shader SpriteShader
        {
            get
            {
                if (_sprite == null) _sprite = Shader.Find("Sprites/Default");
                return _sprite;
            }
        }

        public static Material Lit(Color c, float emission = 0f, float smooth = 0.2f)
        {
            string key = "L" + ColorUtility.ToHtmlStringRGBA(c) + emission.ToString("F2") + smooth.ToString("F2");
            if (cache.TryGetValue(key, out var m) && m != null) return m;

            m = new Material(LitShader) { name = key, enableInstancing = true };
            m.color = c;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smooth);
            if (emission > 0f && m.HasProperty("_EmissionColor"))
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", c * emission);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            cache[key] = m;
            return m;
        }

        public static Material Lit(string hex, float emission = 0f, float smooth = 0.2f)
        {
            return Lit(Pal.Hex(hex), emission, smooth);
        }

        /// <summary>Translucent unlit material (shield bubble, glows).</summary>
        public static Material Glass(Color c)
        {
            string key = "G" + ColorUtility.ToHtmlStringRGBA(c);
            if (cache.TryGetValue(key, out var m) && m != null) return m;
            m = new Material(SpriteShader) { name = key, color = c };
            cache[key] = m;
            return m;
        }
    }

    public static class Prim
    {
        public static GameObject Make(PrimitiveType t, Transform parent, Vector3 pos, Vector3 scale,
            Material m, string name = null, bool shadows = true)
        {
            var g = GameObject.CreatePrimitive(t);
            var col = g.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);
            if (name != null) g.name = name;
            var tr = g.transform;
            tr.SetParent(parent, false);
            tr.localPosition = pos;
            tr.localScale = scale;
            var r = g.GetComponent<Renderer>();
            r.sharedMaterial = m;
            if (!shadows) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return g;
        }

        public static GameObject FromMesh(Mesh mesh, Transform parent, Vector3 pos, Vector3 scale, Material m,
            string name = "Mesh", bool shadows = true)
        {
            var g = new GameObject(name);
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            g.transform.localScale = scale;
            g.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = g.AddComponent<MeshRenderer>();
            r.sharedMaterial = m;
            if (!shadows) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return g;
        }

        public static Transform Empty(string name, Transform parent, Vector3 pos)
        {
            var g = new GameObject(name);
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            return g.transform;
        }
    }

    public static class MeshUtil
    {
        static Mesh _pyramid;

        /// <summary>Unit pyramid: base 1x1 centred on origin, apex at y=1. Flat shaded.</summary>
        public static Mesh Pyramid()
        {
            if (_pyramid != null) return _pyramid;
            var apex = new Vector3(0, 1, 0);
            var c = new[]
            {
                new Vector3(-.5f, 0, -.5f), new Vector3(-.5f, 0, .5f),
                new Vector3(.5f, 0, .5f), new Vector3(.5f, 0, -.5f)
            };
            var verts = new List<Vector3>();
            var tris = new List<int>();
            var center = new Vector3(0, 0.33f, 0);
            for (int i = 0; i < 4; i++)
            {
                Vector3 a = c[i], b = c[(i + 1) % 4];
                Vector3 n = Vector3.Cross(b - apex, a - apex);
                if (Vector3.Dot(n, (a + b + apex) / 3f - center) < 0) { var t = a; a = b; b = t; }
                int s = verts.Count;
                verts.Add(apex); verts.Add(a); verts.Add(b);
                tris.Add(s); tris.Add(s + 1); tris.Add(s + 2);
            }
            // base
            int bs = verts.Count;
            verts.AddRange(new[] { c[0], c[3], c[2], c[1] });
            tris.AddRange(new[] { bs, bs + 1, bs + 2, bs, bs + 2, bs + 3 });
            _pyramid = new Mesh { name = "Pyramid" };
            _pyramid.SetVertices(verts);
            _pyramid.SetTriangles(tris, 0);
            _pyramid.RecalculateNormals();
            _pyramid.RecalculateBounds();
            return _pyramid;
        }

        /// <summary>Surface of revolution around Y. profile = (radius, y) pairs bottom to top.</summary>
        public static Mesh Lathe(Vector2[] profile, int segments, string name = "Lathe")
        {
            var verts = new List<Vector3>();
            var tris = new List<int>();
            int rings = profile.Length;
            for (int i = 0; i < rings; i++)
                for (int j = 0; j <= segments; j++)
                {
                    float a = (float)j / segments * Mathf.PI * 2f;
                    verts.Add(new Vector3(Mathf.Cos(a) * profile[i].x, profile[i].y, Mathf.Sin(a) * profile[i].x));
                }
            int stride = segments + 1;
            for (int i = 0; i < rings - 1; i++)
                for (int j = 0; j < segments; j++)
                {
                    int a = i * stride + j, b = a + 1, c = a + stride, d = c + 1;
                    AddTri(verts, tris, a, c, b);
                    AddTri(verts, tris, b, c, d);
                }
            var m = new Mesh { name = name };
            if (verts.Count > 65000) m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            m.SetVertices(verts);
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        // Adds a triangle wound so that its normal points away from the Y axis.
        static void AddTri(List<Vector3> v, List<int> t, int a, int b, int c)
        {
            Vector3 n = Vector3.Cross(v[b] - v[a], v[c] - v[a]);
            Vector3 mid = (v[a] + v[b] + v[c]) / 3f;
            mid.y = 0;
            if (n.sqrMagnitude > 1e-12f && Vector3.Dot(n, mid) < 0) { var x = b; b = c; c = x; }
            t.Add(a); t.Add(b); t.Add(c);
        }
    }

    /// <summary>Keyboard, mouse and touch swipe input working with old and new Input System.</summary>
    public static class InputHelper
    {
        public static bool Left, Right, Up, Down, Pause;
        static Vector2 downPos;
        static bool tracking;
        const float SwipePx = 55f;

        public static void Poll()
        {
            Left = Right = Up = Down = Pause = false;
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k != null)
            {
                Left = k.leftArrowKey.wasPressedThisFrame || k.aKey.wasPressedThisFrame;
                Right = k.rightArrowKey.wasPressedThisFrame || k.dKey.wasPressedThisFrame;
                Up = k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame;
                Down = k.downArrowKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame;
                Pause = k.escapeKey.wasPressedThisFrame || k.pKey.wasPressedThisFrame;
            }
            var p = Pointer.current;
            if (p != null)
            {
                if (p.press.wasPressedThisFrame) { downPos = p.position.ReadValue(); tracking = true; }
                else if (tracking && p.press.isPressed) Swipe(p.position.ReadValue() - downPos);
                if (p.press.wasReleasedThisFrame) tracking = false;
            }
#else
            Left = Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A);
            Right = Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D);
            Up = Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space);
            Down = Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S);
            Pause = Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P);
            if (Input.GetMouseButtonDown(0)) { downPos = Input.mousePosition; tracking = true; }
            else if (tracking && Input.GetMouseButton(0)) Swipe((Vector2)Input.mousePosition - downPos);
            if (Input.GetMouseButtonUp(0)) tracking = false;
#endif
        }

        static void Swipe(Vector2 d)
        {
            if (d.magnitude < SwipePx) return;
            if (Mathf.Abs(d.x) > Mathf.Abs(d.y)) { if (d.x > 0) Right = true; else Left = true; }
            else { if (d.y > 0) Up = true; else Down = true; }
            tracking = false;
        }
    }
}
