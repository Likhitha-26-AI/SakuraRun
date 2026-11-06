using UnityEngine;

namespace SakuraRunner
{
    /// <summary>
    /// The anime girl: built from primitives, animated procedurally.
    /// Handles lane switching, jumping, sliding and power-up state.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        public const float LaneW = 2.3f;
        public const float RunHeight = 1.65f;
        public const float SlideHeight = 0.8f;
        public const float HalfWidth = 0.32f;
        public const float HalfDepth = 0.3f;
        public const float MagnetDur = 8f;
        public const float BoostDur = 6f;

        const float Gravity = 28f;
        const float JumpV = 9.8f;
        const float SlideTime = 0.75f;
        const float HipY = 0.7f;

        public CameraRig Rig;

        // ---- state exposed to other systems ----
        public bool Dead { get; private set; }
        public bool Shield { get; private set; }
        public float MagnetT { get; private set; }
        public float BoostT { get; private set; }
        public float X => x;
        public float Y => y;
        public bool IsSliding => slideTimer > 0f;
        public float Height => IsSliding ? SlideHeight : RunHeight;

        int lane = 1;
        float x, y, vy, slideTimer, invuln;
        bool grounded = true, wantSlide;
        float runPhase;
        int lastStepHalf;
        int combo;
        float comboTimer;

        // ---- visual parts ----
        Transform visual, model, armL, armR, legL, legR, tailL, tailR, shieldBubble, magnetRing;
        ParticleSystem dust, trail;
        float pitch, yOff, legLA, legRA, armLA, armRA, armZ, tailA, tilt, yaw;
        bool snapPose = true;

        // =====================================================================================
        //  Construction
        // =====================================================================================
        public void Build()
        {
            visual = Prim.Empty("Visual", transform, new Vector3(0, HipY, 0));
            model = Prim.Empty("Model", visual, new Vector3(0, -HipY, 0));

            var skin = Mats.Lit(Pal.Skin, 0f, 0.35f);
            var hair = Mats.Lit(Pal.Hex("FF9CC4"), 0.06f, 0.5f);
            var hairDark = Mats.Lit(Pal.Hex("E884B0"), 0f, 0.5f);
            var top = Mats.Lit(Pal.PinkLight);
            var sleeve = Mats.Lit(Pal.Hex("FFC2DA"));
            var skirtM = Mats.Lit(Pal.Lavender, 0.05f);
            var trim = Mats.Lit(Pal.Peach);
            var obi = Mats.Lit(Pal.PeachDeep, 0.05f);
            var ribbon = Mats.Lit(Pal.Hex("FFE0B8"), 0.25f);
            var sock = Mats.Lit(Pal.Cream);
            var shoe = Mats.Lit(Pal.Hex("B79BE8"));
            var eye = Mats.Lit(Pal.Hex("5B3A9E"), 0.1f, 0.8f);
            var white = Mats.Lit(Color.white, 0.8f, 0.9f);
            var blush = Mats.Lit(Pal.Hex("FF9AAE"));
            var mouth = Mats.Lit(Pal.Hex("D9647F"));
            var petalM = Mats.Lit(Pal.Hex("FFB7D5"), 0.5f);

            // torso, obi, bow
            Prim.Make(PrimitiveType.Capsule, model, new Vector3(0, 1.12f, 0), new Vector3(0.3f, 0.2f, 0.2f), top, "Torso");
            Prim.Make(PrimitiveType.Cylinder, model, new Vector3(0, 0.97f, 0), new Vector3(0.34f, 0.05f, 0.25f), obi, "Obi");
            Prim.Make(PrimitiveType.Sphere, model, new Vector3(-0.1f, 1.0f, -0.17f), new Vector3(0.15f, 0.11f, 0.08f), obi, "BowL");
            Prim.Make(PrimitiveType.Sphere, model, new Vector3(0.1f, 1.0f, -0.17f), new Vector3(0.15f, 0.11f, 0.08f), obi, "BowR");
            Prim.Make(PrimitiveType.Sphere, model, new Vector3(0, 1.0f, -0.18f), new Vector3(0.07f, 0.07f, 0.07f), trim, "BowKnot");

            // skirt (flared lathe) + hem trim
            var skirtMesh = MeshUtil.Lathe(new[]
            {
                new Vector2(0.37f, 0.52f), new Vector2(0.31f, 0.62f),
                new Vector2(0.22f, 0.80f), new Vector2(0.17f, 0.93f)
            }, 20, "Skirt");
            Prim.FromMesh(skirtMesh, model, Vector3.zero, Vector3.one, skirtM, "Skirt");
            Prim.Make(PrimitiveType.Cylinder, model, new Vector3(0, 0.525f, 0), new Vector3(0.78f, 0.012f, 0.78f), trim, "Hem");

            // legs
            legL = BuildLeg(-0.1f, skin, sock, shoe);
            legR = BuildLeg(0.1f, skin, sock, shoe);

            // arms
            armL = BuildArm(-0.2f, -1f, sleeve, skin);
            armR = BuildArm(0.2f, 1f, sleeve, skin);

            // head
            var head = Prim.Empty("Head", model, new Vector3(0, 1.5f, 0));
            Prim.Make(PrimitiveType.Sphere, head, new Vector3(0, 0.02f, 0), Vector3.one * 0.44f, skin, "Face");
            Prim.Make(PrimitiveType.Sphere, head, new Vector3(0, 0.08f, -0.07f), Vector3.one * 0.5f, hair, "HairBack");
            Prim.Make(PrimitiveType.Sphere, head, new Vector3(-0.11f, 0.16f, 0.15f), new Vector3(0.2f, 0.12f, 0.13f), hair, "BangL");
            Prim.Make(PrimitiveType.Sphere, head, new Vector3(0f, 0.18f, 0.16f), new Vector3(0.18f, 0.1f, 0.12f), hair, "BangM");
            Prim.Make(PrimitiveType.Sphere, head, new Vector3(0.11f, 0.16f, 0.15f), new Vector3(0.2f, 0.12f, 0.13f), hair, "BangR");
            Prim.Make(PrimitiveType.Capsule, head, new Vector3(-0.21f, -0.02f, 0.02f), new Vector3(0.08f, 0.14f, 0.08f), hairDark, "LockL");
            Prim.Make(PrimitiveType.Capsule, head, new Vector3(0.21f, -0.02f, 0.02f), new Vector3(0.08f, 0.14f, 0.08f), hairDark, "LockR");
            for (int s = -1; s <= 1; s += 2)
            {
                Prim.Make(PrimitiveType.Sphere, head, new Vector3(0.085f * s, 0f, 0.183f), new Vector3(0.085f, 0.12f, 0.04f), eye, "Eye");
                Prim.Make(PrimitiveType.Sphere, head, new Vector3(0.085f * s + 0.02f, 0.035f, 0.2f), Vector3.one * 0.035f, white, "Shine", false);
                Prim.Make(PrimitiveType.Sphere, head, new Vector3(0.135f * s, -0.065f, 0.17f), new Vector3(0.07f, 0.035f, 0.03f), blush, "Blush", false);
            }
            Prim.Make(PrimitiveType.Sphere, head, new Vector3(0, -0.095f, 0.195f), new Vector3(0.04f, 0.02f, 0.015f), mouth, "Mouth", false);

            // sakura hairpin
            var pin = Prim.Empty("Hairpin", head, new Vector3(0.17f, 0.13f, 0.1f));
            for (int i = 0; i < 5; i++)
            {
                float a = i * Mathf.PI * 2f / 5f;
                Prim.Make(PrimitiveType.Sphere, pin, new Vector3(Mathf.Cos(a) * 0.035f, Mathf.Sin(a) * 0.035f, 0),
                    Vector3.one * 0.045f, petalM, "PinPetal", false);
            }
            Prim.Make(PrimitiveType.Sphere, pin, new Vector3(0, 0, 0.01f), Vector3.one * 0.03f, ribbon, "PinCore", false);

            // twin tails
            tailL = BuildTail(-0.22f, hair, ribbon);
            tailR = BuildTail(0.22f, hair, ribbon);

            // shield bubble + magnet ring
            shieldBubble = Prim.Make(PrimitiveType.Sphere, transform, new Vector3(0, 0.9f, 0), Vector3.one * 2.4f,
                Mats.Glass(new Color(0.78f, 0.9f, 1f, 0.28f)), "Shield", false).transform;
            shieldBubble.gameObject.SetActive(false);

            magnetRing = Prim.Empty("MagnetRing", transform, new Vector3(0, 0.9f, 0));
            var ringMat = Mats.Lit(Pal.Hex("C9A8FF"), 1.2f);
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI * 2f / 8f;
                Prim.Make(PrimitiveType.Sphere, magnetRing, new Vector3(Mathf.Cos(a) * 1.15f, 0, Mathf.Sin(a) * 1.15f),
                    Vector3.one * 0.14f, ringMat, "Orb", false);
            }
            magnetRing.gameObject.SetActive(false);

            BuildParticles();
        }

        Transform BuildLeg(float px, Material skin, Material sock, Material shoe)
        {
            var p = Prim.Empty("Leg", model, new Vector3(px, 0.62f, 0));
            Prim.Make(PrimitiveType.Capsule, p, new Vector3(0, -0.28f, 0), new Vector3(0.12f, 0.28f, 0.12f), skin, "Leg");
            Prim.Make(PrimitiveType.Capsule, p, new Vector3(0, -0.34f, 0), new Vector3(0.13f, 0.2f, 0.13f), sock, "Sock");
            Prim.Make(PrimitiveType.Sphere, p, new Vector3(0, -0.55f, 0.04f), new Vector3(0.15f, 0.1f, 0.24f), shoe, "Shoe");
            return p.transform;
        }

        Transform BuildArm(float px, float side, Material sleeve, Material skin)
        {
            var p = Prim.Empty("Arm", model, new Vector3(px, 1.27f, 0));
            Prim.Make(PrimitiveType.Capsule, p, new Vector3(0, -0.16f, 0), new Vector3(0.12f, 0.2f, 0.12f), sleeve, "Sleeve");
            Prim.Make(PrimitiveType.Capsule, p, new Vector3(0, -0.35f, 0), new Vector3(0.07f, 0.12f, 0.07f), skin, "Forearm");
            Prim.Make(PrimitiveType.Sphere, p, new Vector3(0, -0.46f, 0), Vector3.one * 0.09f, skin, "Hand");
            return p.transform;
        }

        Transform BuildTail(float px, Material hair, Material ribbon)
        {
            var p = Prim.Empty("Tail", model, new Vector3(px, 1.58f, -0.08f));
            Prim.Make(PrimitiveType.Sphere, p, Vector3.zero, new Vector3(0.11f, 0.09f, 0.11f), ribbon, "TailRibbon");
            Prim.Make(PrimitiveType.Capsule, p, new Vector3(0, -0.3f, -0.02f), new Vector3(0.11f, 0.3f, 0.11f), hair, "TailHair");
            Prim.Make(PrimitiveType.Sphere, p, new Vector3(0, -0.6f, -0.02f), Vector3.one * 0.1f, hair, "TailTip");
            return p.transform;
        }

        void BuildParticles()
        {
            dust = Fx.Create("RunDust", transform, new Vector3(0, 0.08f, -0.2f), Fx.SoftMat);
            var m = dust.main;
            m.loop = true;
            m.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
            m.startSpeed = 0f;
            m.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.4f);
            m.startColor = new Color(1f, 0.9f, 0.9f, 0.5f);
            m.simulationSpace = ParticleSystemSimulationSpace.World;
            m.maxParticles = 60;
            var e = dust.emission; e.rateOverTime = 14f;
            var v = dust.velocityOverLifetime; v.enabled = true; v.space = ParticleSystemSimulationSpace.World;
            v.x = new ParticleSystem.MinMaxCurve(-0.4f, 0.4f);
            v.y = new ParticleSystem.MinMaxCurve(0.2f, 0.8f);
            v.z = new ParticleSystem.MinMaxCurve(-8f, -5f);
            var sh = dust.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.15f;
            var sz = dust.sizeOverLifetime; sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.5f, 1f, 1.6f));
            var c = dust.colorOverLifetime; c.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                      new[] { new GradientAlphaKey(0.6f, 0), new GradientAlphaKey(0f, 1) });
            c.color = new ParticleSystem.MinMaxGradient(g);
            dust.Play();

            trail = Fx.Create("BoostTrail", transform, new Vector3(0, 0.9f, -0.4f), Fx.SoftMat);
            var tm = trail.main;
            tm.loop = true;
            tm.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.6f);
            tm.startSpeed = 0f;
            tm.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.7f);
            tm.startColor = new ParticleSystem.MinMaxGradient(Pal.PeachDeep, Pal.Pink);
            tm.simulationSpace = ParticleSystemSimulationSpace.World;
            tm.maxParticles = 120;
            var te = trail.emission; te.rateOverTime = 90f;
            var tv = trail.velocityOverLifetime; tv.enabled = true; tv.space = ParticleSystemSimulationSpace.World;
            tv.x = new ParticleSystem.MinMaxCurve(-0.5f, 0.5f);
            tv.y = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);
            tv.z = new ParticleSystem.MinMaxCurve(-22f, -16f);
            var tsh = trail.shape; tsh.shapeType = ParticleSystemShapeType.Sphere; tsh.radius = 0.45f;
            var tc = trail.colorOverLifetime; tc.enabled = true;
            tc.color = new ParticleSystem.MinMaxGradient(g);
            trail.Play();
            var tem = trail.emission; tem.enabled = false;
        }

        // =====================================================================================
        //  Lifecycle
        // =====================================================================================
        public void ResetPlayer()
        {
            lane = 1; x = 0; y = 0; vy = 0; slideTimer = 0; invuln = 0;
            grounded = true; wantSlide = false;
            Dead = false; Shield = false; MagnetT = 0; BoostT = 0;
            combo = 0; comboTimer = 0;
            snapPose = true;
            transform.position = Vector3.zero;
        }

        void Update()
        {
            var gm = GameManager.I;
            if (gm == null || visual == null) return;
            float dt = Time.deltaTime;

            if (gm.State == GameState.Playing && !Dead)
            {
                HandleInput();
                Simulate(dt);
            }
            Animate(dt, gm);
            transform.position = new Vector3(x, y, 0f);
        }

        void HandleInput()
        {
            if (InputHelper.Left && lane > 0) { lane--; AudioManager.Play(Sfx.Swish); }
            if (InputHelper.Right && lane < 2) { lane++; AudioManager.Play(Sfx.Swish); }
            if (InputHelper.Up && grounded) Jump();
            if (InputHelper.Down)
            {
                if (grounded) StartSlide();
                else { vy = -22f; wantSlide = true; }
            }
        }

        void Jump()
        {
            vy = JumpV;
            grounded = false;
            slideTimer = 0f;
            wantSlide = false;
            AudioManager.Play(Sfx.Jump);
            Fx.Burst(transform.position + Vector3.up * 0.1f, new Color(1f, 0.85f, 0.9f, 0.8f), 8, 2f, 0.3f);
        }

        void StartSlide()
        {
            slideTimer = SlideTime;
            AudioManager.Play(Sfx.Slide);
        }

        void Simulate(float dt)
        {
            x = Mathf.MoveTowards(x, (lane - 1) * LaneW, 26f * dt);

            if (!grounded)
            {
                vy -= Gravity * dt;
                y += vy * dt;
                if (y <= 0f)
                {
                    y = 0f; vy = 0f; grounded = true;
                    AudioManager.Play(Sfx.Land);
                    Fx.Burst(transform.position + Vector3.up * 0.1f, new Color(1f, 0.9f, 0.92f, 0.8f), 7, 2.5f, 0.3f);
                    if (wantSlide) { StartSlide(); wantSlide = false; }
                }
            }
            if (slideTimer > 0f) slideTimer -= dt;

            if (MagnetT > 0f) MagnetT -= dt;
            if (BoostT > 0f)
            {
                BoostT -= dt;
                if (BoostT <= 0f) { BoostT = 0f; invuln = 1.0f; }
            }
            if (invuln > 0f) invuln -= dt;
            if (comboTimer > 0f) { comboTimer -= dt; if (comboTimer <= 0f) combo = 0; }
        }

        // =====================================================================================
        //  Gameplay events (called by WorldGenerator)
        // =====================================================================================
        /// <summary>Returns true if the hit was absorbed (obstacle should be smashed), false if the player died.</summary>
        public bool TryHit()
        {
            if (Dead) return true;
            if (invuln > 0f || BoostT > 0f) { AudioManager.Play(Sfx.Smash); return true; }
            if (Shield)
            {
                Shield = false;
                invuln = 1.5f;
                Fx.Burst(transform.position + Vector3.up * 0.9f, new Color(0.7f, 0.9f, 1f), 28, 7f, 0.4f);
                AudioManager.Play(Sfx.ShieldBreak);
                if (Rig != null) Rig.Shake(0.25f, 0.3f);
                return true;
            }
            Die();
            return false;
        }

        void Die()
        {
            Dead = true;
            AudioManager.Play(Sfx.Hit);
            Fx.Burst(transform.position + Vector3.up * 1f, Pal.Pink, 30, 6f, 0.3f, true);
            if (Rig != null) Rig.Shake(0.5f, 0.5f);
            if (GameManager.I != null) GameManager.I.PlayerDied();
        }

        public void Collect(Pickup p)
        {
            Vector3 pos = p.transform.position;
            switch (p.Kind)
            {
                case PickupKind.Flower:
                    combo++;
                    comboTimer = 0.7f;
                    AudioManager.Play(Sfx.Flower, 1f + Mathf.Min(combo, 12) * 0.045f);
                    Fx.Burst(pos, Pal.Pink, 8, 3f, 0.22f);
                    if (GameManager.I != null) GameManager.I.AddFlower(1);
                    break;
                case PickupKind.Magnet:
                    MagnetT = MagnetDur;
                    PowerFx(pos, Pal.Hex("C9A8FF"));
                    break;
                case PickupKind.Shield:
                    Shield = true;
                    PowerFx(pos, Pal.Sky);
                    break;
                case PickupKind.Speed:
                    BoostT = BoostDur;
                    PowerFx(pos, Pal.PeachDeep);
                    break;
            }
        }

        void PowerFx(Vector3 pos, Color c)
        {
            AudioManager.Play(Sfx.PowerUp);
            Fx.Burst(pos, c, 30, 6f, 0.4f);
            Fx.Burst(pos, Color.white, 14, 3f, 0.25f, true);
            if (Rig != null) Rig.Shake(0.12f, 0.2f);
        }

        // =====================================================================================
        //  Procedural animation
        // =====================================================================================
        void Animate(float dt, GameManager gm)
        {
            bool menu = gm.State == GameState.Menu;
            bool running = (gm.State == GameState.Playing && !Dead) || menu;
            if (running && gm.State != GameState.Paused)
                runPhase += dt * (menu ? 8f : 7f + gm.Speed * 0.38f);

            float s = Mathf.Sin(runPhase);
            float bob = Mathf.Abs(Mathf.Cos(runPhase)) * 0.07f;

            float tPitch, tY, tLegL, tLegR, tArmL, tArmR, tArmZ, tTail;
            bool airborne = !grounded && !Dead;

            if (Dead || gm.State == GameState.GameOver)
            {
                tPitch = 80f; tY = -0.36f; tLegL = 35f; tLegR = -25f; tArmL = -120f; tArmR = -140f; tArmZ = 35f; tTail = 70f;
            }
            else if (IsSliding)
            {
                tPitch = -75f; tY = -0.38f; tLegL = -15f; tLegR = -25f; tArmL = -80f; tArmR = -60f; tArmZ = 30f; tTail = 70f;
            }
            else if (airborne)
            {
                tPitch = 10f; tY = 0f; tLegL = -40f; tLegR = 30f; tArmL = -150f; tArmR = -150f; tArmZ = 22f; tTail = 55f;
            }
            else
            {
                tPitch = 9f; tY = bob;
                tLegL = s * 60f; tLegR = -s * 60f;
                tArmL = -s * 55f; tArmR = s * 55f; tArmZ = 8f;
                tTail = 22f + Mathf.Clamp01(gm.Speed / GameManager.MaxSpeed) * 35f + Mathf.Sin(runPhase * 2f) * 8f;
            }

            float tTilt = 0f;
            if (gm.State == GameState.Playing && !Dead)
                tTilt = -Mathf.Clamp(((lane - 1) * LaneW - x) * 14f, -18f, 18f);
            float tYaw = (menu || gm.State == GameState.GameOver) ? 25f : 0f;

            float a = snapPose ? 1f : 1f - Mathf.Exp(-18f * dt);
            snapPose = false;
            pitch = Mathf.Lerp(pitch, tPitch, a);
            yOff = Mathf.Lerp(yOff, tY, a);
            legLA = Mathf.Lerp(legLA, tLegL, a);
            legRA = Mathf.Lerp(legRA, tLegR, a);
            armLA = Mathf.Lerp(armLA, tArmL, a);
            armRA = Mathf.Lerp(armRA, tArmR, a);
            armZ = Mathf.Lerp(armZ, tArmZ, a);
            tailA = Mathf.Lerp(tailA, tTail, a);
            tilt = Mathf.Lerp(tilt, tTilt, a);
            yaw = Mathf.Lerp(yaw, tYaw, 1f - Mathf.Exp(-4f * dt));

            visual.localPosition = new Vector3(0, HipY + yOff, 0);
            visual.localRotation = Quaternion.Euler(pitch, yaw, tilt);
            legL.localRotation = Quaternion.Euler(legLA, 0, 0);
            legR.localRotation = Quaternion.Euler(legRA, 0, 0);
            armL.localRotation = Quaternion.Euler(armLA, 0, -armZ);
            armR.localRotation = Quaternion.Euler(armRA, 0, armZ);
            float flutter = Mathf.Sin(runPhase * 1.7f + 1f) * 6f;
            tailL.localRotation = Quaternion.Euler(tailA + flutter, 0, 8f);
            tailR.localRotation = Quaternion.Euler(tailA - flutter, 0, -8f);

            // footsteps
            if (gm.State == GameState.Playing && grounded && !Dead && !IsSliding)
            {
                int half = Mathf.FloorToInt(runPhase / Mathf.PI);
                if (half != lastStepHalf) { lastStepHalf = half; AudioManager.Play(Sfx.Step, 0.9f + Random.value * 0.2f); }
            }

            // effects
            var de = dust.emission;
            de.enabled = running && grounded && !Dead;
            var te = trail.emission;
            te.enabled = BoostT > 0f;

            shieldBubble.gameObject.SetActive(Shield);
            if (Shield)
            {
                float pulse = 2.4f + Mathf.Sin(Time.time * 4f) * 0.08f;
                shieldBubble.localScale = Vector3.one * pulse;
            }
            bool mag = MagnetT > 0f;
            magnetRing.gameObject.SetActive(mag);
            if (mag) magnetRing.Rotate(0f, 220f * dt, 0f);

            // blink the whole model while briefly invulnerable
            model.gameObject.SetActive(!(invuln > 0f && BoostT <= 0f && Mathf.FloorToInt(Time.time * 14f) % 2 == 0));
        }
    }
}
