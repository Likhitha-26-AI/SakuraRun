using UnityEngine;

namespace SakuraRunner
{
    /// <summary>Procedural textures and particle helpers (petals, sparkles, bursts).</summary>
    public static class Fx
    {
        static Texture2D soft, petal;
        static Sprite flowerSprite;
        static Material softMat, petalMat;

        public static Texture2D SoftTex()
        {
            if (soft != null) return soft;
            const int n = 64;
            soft = NewTex(n);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + .5f) / n * 2f - 1f, dy = (y + .5f) / n * 2f - 1f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1f - r);
                    soft.SetPixel(x, y, new Color(1, 1, 1, a * a));
                }
            soft.Apply();
            return soft;
        }

        public static Texture2D PetalTex()
        {
            if (petal != null) return petal;
            const int n = 64;
            petal = NewTex(n);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = (x + .5f) / n * 2f - 1f, v = (y + .5f) / n * 2f - 1f;
                    float d = (u / .5f) * (u / .5f) + (v / .95f) * (v / .95f);
                    float a = Mathf.Clamp01((1f - d) * 6f);
                    if (v > 0.72f && Mathf.Abs(u) < 0.14f * (v - 0.72f) / 0.28f) a = 0f; // notch
                    Color c = Color.Lerp(Color.white, new Color(1f, .82f, .9f), (v + 1f) / 2f);
                    c.a = a;
                    petal.SetPixel(x, y, c);
                }
            petal.Apply();
            return petal;
        }

        /// <summary>Five-petal flower sprite used by the HUD.</summary>
        public static Sprite FlowerSprite()
        {
            if (flowerSprite != null) return flowerSprite;
            const int n = 128;
            var t = NewTex(n);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + .5f) / n * 2f - 1f, dy = (y + .5f) / n * 2f - 1f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float th = Mathf.Atan2(dy, dx) + Mathf.PI / 2f;
                    if (th < 0) th += Mathf.PI * 2f;
                    float lim = 0.95f * (0.4f + 0.6f * Mathf.Abs(Mathf.Cos(2.5f * th)));
                    float a = Mathf.Clamp01((lim - r) * 30f);
                    Color c = Color.Lerp(new Color(1f, .62f, .78f), new Color(1f, .9f, .95f), Mathf.Clamp01(r / 0.9f));
                    if (r < 0.16f) c = new Color(1f, .78f, .5f);
                    c.a = a;
                    t.SetPixel(x, y, c);
                }
            t.Apply();
            flowerSprite = Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(.5f, .5f), 100f);
            return flowerSprite;
        }

        static Texture2D NewTex(int n)
        {
            return new Texture2D(n, n, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
        }

        public static Material SoftMat
        {
            get
            {
                if (softMat == null) softMat = new Material(Mats.SpriteShader) { mainTexture = SoftTex() };
                return softMat;
            }
        }

        public static Material PetalMat
        {
            get
            {
                if (petalMat == null) petalMat = new Material(Mats.SpriteShader) { mainTexture = PetalTex() };
                return petalMat;
            }
        }

        /// <summary>Creates a stopped particle system so modules can be configured before Play().</summary>
        public static ParticleSystem Create(string name, Transform parent, Vector3 localPos, Material mat)
        {
            var g = new GameObject(name);
            if (parent != null) g.transform.SetParent(parent, false);
            g.transform.localPosition = localPos;
            var ps = g.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var r = g.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return ps;
        }

        static Gradient FadeOut(Color c)
        {
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(c, 0f), new GradientColorKey(c, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.1f), new GradientAlphaKey(0f, 1f) });
            return g;
        }

        /// <summary>One-shot sparkle burst, destroyed automatically.</summary>
        public static void Burst(Vector3 pos, Color c, int count = 16, float speed = 4f, float size = 0.25f, bool petals = false)
        {
            var ps = Create("Burst", null, pos, petals ? PetalMat : SoftMat);
            var main = ps.main;
            main.duration = 1f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.4f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.6f, size);
            main.startColor = c;
            main.gravityModifier = 0.5f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = count;
            main.stopAction = ParticleSystemStopAction.Destroy;
            if (petals) main.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);

            var em = ps.emission;
            em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Sphere;
            sh.radius = 0.15f;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(FadeOut(Color.white));

            var sz = ps.sizeOverLifetime;
            sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));

            ps.Play();
        }

        /// <summary>Endless drifting cherry blossom petals. Parent it to the camera rig.</summary>
        public static ParticleSystem Petals(Transform parent)
        {
            var ps = Create("FallingPetals", parent, new Vector3(0, 13f, 18f), PetalMat);
            var main = ps.main;
            main.loop = true;
            main.prewarm = true;
            main.duration = 5f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(7f, 10f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.14f, 0.3f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);
            main.startColor = new ParticleSystem.MinMaxGradient(Pal.Pink, Pal.PinkLight);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 500;

            var em = ps.emission;
            em.rateOverTime = 55f;

            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Box;
            sh.scale = new Vector3(44f, 1f, 70f);

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(-0.6f, 0.6f);
            vel.y = new ParticleSystem.MinMaxCurve(-1.6f, -0.9f);
            vel.z = new ParticleSystem.MinMaxCurve(-5f, -3f);

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.9f;
            noise.frequency = 0.25f;
            noise.scrollSpeed = 0.4f;

            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-2.5f, 2.5f);

            ps.Play();
            return ps;
        }

        /// <summary>Soft glowing motes floating upward for a dreamy atmosphere.</summary>
        public static ParticleSystem Motes(Transform parent)
        {
            var ps = Create("DreamMotes", parent, new Vector3(0, 3f, 14f), SoftMat);
            var main = ps.main;
            main.loop = true;
            main.prewarm = true;
            main.duration = 5f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 8f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.5f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, .85f, .7f, .6f), new Color(.85f, .75f, 1f, .6f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 150;

            var em = ps.emission;
            em.rateOverTime = 16f;

            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Box;
            sh.scale = new Vector3(30f, 5f, 50f);

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);
            vel.y = new ParticleSystem.MinMaxCurve(0.15f, 0.5f);
            vel.z = new ParticleSystem.MinMaxCurve(-3f, -1.5f);

            var col = ps.colorOverLifetime;
            col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(FadeOut(Color.white));

            ps.Play();
            return ps;
        }
    }
}
