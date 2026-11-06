using System.Collections.Generic;
using UnityEngine;

namespace SakuraRunner
{
    /// <summary>
    /// Endless world. The player stays at the origin; road segments scroll towards -Z and are recycled.
    /// Scenery of each segment is built once and reused; obstacles and pickups are pooled and re-laid every recycle.
    /// </summary>
    public class WorldGenerator : MonoBehaviour
    {
        public const float SegLen = 36f;
        const int SegCount = 7;
        const int DecorVariants = 9;
        const int Slots = 4;
        const float SlotGap = 9f;
        const float RoadW = PlayerController.LaneW * 3f + 1.8f;

        class Segment
        {
            public Transform Root;
            public int Decor;
            public readonly List<Obstacle> Obstacles = new List<Obstacle>();
            public readonly List<Pickup> Pickups = new List<Pickup>();
        }

        public PlayerController Player;

        readonly List<Segment> segments = new List<Segment>();
        readonly List<Transform> decorPool = new List<Transform>();
        readonly Dictionary<string, Stack<GameObject>> pools = new Dictionary<string, Stack<GameObject>>();
        Transform poolRoot;
        Transform backdrop;
        int safeSegments;
        int pathLane = 1;
        bool built;

        // ------------------------------------------------------------------ setup
        public void Build()
        {
            if (built) return;
            built = true;
            poolRoot = Prim.Empty("Pool", transform, Vector3.zero);
            poolRoot.gameObject.SetActive(false);

            for (int i = 0; i < DecorVariants; i++)
            {
                var d = BuildDecor(i, new System.Random(1000 + i * 37));
                d.SetParent(poolRoot, false);
                decorPool.Add(d);
            }
            BuildBackdrop();
            for (int i = 0; i < SegCount; i++) segments.Add(new Segment());
            ResetWorld();
        }

        public void ResetWorld()
        {
            if (!built) return;
            for (int i = 0; i < segments.Count; i++)
            {
                var s = segments[i];
                ReleaseSegmentContent(s);
                if (s.Root != null) ReleaseDecor(s);
            }
            pathLane = 1;
            safeSegments = 2;
            for (int i = 0; i < segments.Count; i++)
            {
                Fill(segments[i], -SegLen * 0.5f + i * SegLen);
            }
        }

        // ------------------------------------------------------------------ update
        void Update()
        {
            var gm = GameManager.I;
            if (gm == null || !built) return;

            float dz = gm.Speed * Time.deltaTime;
            for (int i = 0; i < segments.Count; i++)
                segments[i].Root.position += new Vector3(0, 0, -dz);

            // recycle segments that have passed behind the player
            var first = segments[0];
            if (first.Root.position.z + SegLen < -SegLen * 0.6f)
            {
                segments.RemoveAt(0);
                ReleaseSegmentContent(first);
                ReleaseDecor(first);
                float z = segments[segments.Count - 1].Root.position.z + SegLen;
                Fill(first, z);
                segments.Add(first);
            }

            // slow parallax on the far backdrop
            if (backdrop != null)
                backdrop.position = new Vector3(Mathf.Lerp(backdrop.position.x, -Player.X * 0.4f, Time.deltaTime), 0, 0);

            if (gm.State == GameState.Playing && Player != null && !Player.Dead)
                Collisions(dz);
            else
                MagnetStep(false);
        }

        // ------------------------------------------------------------------ collisions
        void Collisions(float dz)
        {
            Vector3 pc = new Vector3(Player.X, Player.Y + Player.Height * 0.5f, 0f);
            Vector3 ph = new Vector3(PlayerController.HalfWidth * 0.85f, Player.Height * 0.5f * 0.92f, PlayerController.HalfDepth);

            MagnetStep(Player.MagnetT > 0f);

            for (int i = 0; i < segments.Count; i++)
            {
                var s = segments[i];
                float sz = s.Root.position.z;
                if (sz > 40f || sz + SegLen < -6f) continue;

                for (int k = s.Pickups.Count - 1; k >= 0; k--)
                {
                    var p = s.Pickups[k];
                    if (!p.gameObject.activeSelf) continue;
                    Vector3 d = p.transform.position - new Vector3(Player.X, Player.Y + 0.9f, 0f);
                    float r = p.Radius + 0.55f;
                    if (d.sqrMagnitude < r * r)
                    {
                        Player.Collect(p);
                        Return(p.gameObject, "P_" + p.Kind);
                        s.Pickups.RemoveAt(k);
                    }
                }

                for (int k = s.Obstacles.Count - 1; k >= 0; k--)
                {
                    var o = s.Obstacles[k];
                    if (!o.gameObject.activeSelf) continue;
                    Vector3 oc = o.WorldCenter;
                    if (Mathf.Abs(oc.z) > o.Half.z + ph.z + 0.5f) continue;
                    bool hit = Mathf.Abs(oc.x - pc.x) < o.Half.x + ph.x &&
                               Mathf.Abs(oc.y - pc.y) < o.Half.y + ph.y &&
                               Mathf.Abs(oc.z - pc.z) < o.Half.z + ph.z;
                    if (!hit) continue;

                    bool absorbed = Player.TryHit();
                    if (absorbed)
                    {
                        Fx.Burst(oc, Pal.Wood, 22, 6f, 0.3f);
                        Fx.Burst(oc, Pal.Pink, 10, 4f, 0.3f, true);
                        Return(o.gameObject, ObstacleKey(o.Kind, o.Variant));
                        s.Obstacles.RemoveAt(k);
                    }
                    else return;
                }
            }
        }

        void MagnetStep(bool active)
        {
            if (!active) return;
            Vector3 target = new Vector3(Player.X, Player.Y + 0.9f, 0f);
            for (int i = 0; i < segments.Count; i++)
            {
                var s = segments[i];
                if (s.Root.position.z > 40f) continue;
                for (int k = 0; k < s.Pickups.Count; k++)
                {
                    var p = s.Pickups[k];
                    if (p.Kind != PickupKind.Flower || !p.gameObject.activeSelf) continue;
                    Vector3 d = target - p.transform.position;
                    if (p.Attracted || d.sqrMagnitude < 11f * 11f)
                    {
                        p.Attracted = true;
                        p.transform.position += d.normalized * Mathf.Min(d.magnitude, (GameManager.I.Speed + 14f) * Time.deltaTime);
                    }
                }
            }
        }

        // ------------------------------------------------------------------ segment content
        void Fill(Segment s, float z)
        {
            int idx = Random.Range(0, decorPool.Count);
            var d = decorPool[idx];
            decorPool.RemoveAt(idx);
            s.Decor = idx;
            s.Root = d;
            d.SetParent(transform, false);
            d.position = new Vector3(0, 0, z);

            if (safeSegments > 0) { safeSegments--; LayFlowersOnly(s); return; }
            Populate(s);
        }

        void ReleaseDecor(Segment s)
        {
            if (s.Root == null) return;
            s.Root.SetParent(poolRoot, false);
            decorPool.Add(s.Root);
            s.Root = null;
        }

        void ReleaseSegmentContent(Segment s)
        {
            for (int i = 0; i < s.Obstacles.Count; i++)
                if (s.Obstacles[i] != null) Return(s.Obstacles[i].gameObject, ObstacleKey(s.Obstacles[i].Kind, s.Obstacles[i].Variant));
            for (int i = 0; i < s.Pickups.Count; i++)
                if (s.Pickups[i] != null) Return(s.Pickups[i].gameObject, "P_" + s.Pickups[i].Kind);
            s.Obstacles.Clear();
            s.Pickups.Clear();
        }

        void LayFlowersOnly(Segment s)
        {
            int lane = 1;
            for (int slot = 0; slot < Slots; slot++)
            {
                float z = 4f + slot * SlotGap;
                for (int f = 0; f < 5; f++)
                    AddPickup(s, PickupKind.Flower, lane, z + f * 1.4f, 1.0f);
                lane = Mathf.Clamp(lane + Random.Range(-1, 2), 0, 2);
            }
        }

        void Populate(Segment s)
        {
            var gm = GameManager.I;
            float diff = gm != null ? Mathf.Clamp01(gm.Distance / 1800f) : 0f;
            float pObs = 0.28f + 0.38f * diff;

            for (int slot = 0; slot < Slots; slot++)
            {
                float z = 4f + slot * SlotGap;
                pathLane = Mathf.Clamp(pathLane + Random.Range(-1, 2), 0, 2);
                bool rest = Random.value < 0.18f - 0.08f * diff;   // calm moments with a long flower trail

                ObstacleKind? pathKind = null;
                for (int lane = 0; lane < 3; lane++)
                {
                    if (rest) break;
                    if (Random.value > pObs && !(lane != pathLane && Random.value < 0.15f)) continue;

                    ObstacleKind kind;
                    float r = Random.value;
                    if (lane == pathLane)
                        kind = r < 0.55f ? ObstacleKind.Low : ObstacleKind.High;       // path lane is never blocked
                    else
                        kind = r < 0.4f ? ObstacleKind.Full : (r < 0.72f ? ObstacleKind.Low : ObstacleKind.High);

                    AddObstacle(s, kind, lane, z);
                    if (lane == pathLane) pathKind = kind;
                }

                // flowers along the safe lane
                for (int f = 0; f < 4; f++)
                {
                    float fz = z + 2.4f + f * 1.5f;
                    AddPickup(s, PickupKind.Flower, pathLane, fz, 1.0f);
                }
                if (pathKind == ObstacleKind.Low)                                       // arc over a jumpable obstacle
                {
                    AddPickup(s, PickupKind.Flower, pathLane, z - 1.2f, 1.5f);
                    AddPickup(s, PickupKind.Flower, pathLane, z, 2.15f);
                    AddPickup(s, PickupKind.Flower, pathLane, z + 1.2f, 1.5f);
                }
                else if (pathKind == null && Random.value < 0.35f)
                {
                    int l2 = Mathf.Clamp(pathLane + (Random.value < 0.5f ? -1 : 1), 0, 2);
                    for (int f = 0; f < 3; f++) AddPickup(s, PickupKind.Flower, l2, z + 3f + f * 1.5f, 1.0f);
                }

                if (!rest && Random.value < 0.07f)
                {
                    float r = Random.value;
                    var k = r < 0.34f ? PickupKind.Magnet : (r < 0.67f ? PickupKind.Shield : PickupKind.Speed);
                    AddPickup(s, k, pathLane, z + 7.5f, 1.1f);
                }
            }
        }

        static string ObstacleKey(ObstacleKind k, int v) { return "O_" + k + "_" + v; }

        void AddObstacle(Segment s, ObstacleKind kind, int lane, float z)
        {
            int variant = Random.Range(0, 2);
            var go = Take(ObstacleKey(kind, variant), () => Props.MakeObstacle(kind, variant).gameObject);
            go.transform.SetParent(s.Root, false);
            go.transform.localPosition = new Vector3((lane - 1) * PlayerController.LaneW, 0f, z);
            go.transform.localRotation = Quaternion.identity;
            s.Obstacles.Add(go.GetComponent<Obstacle>());
        }

        void AddPickup(Segment s, PickupKind kind, int lane, float z, float y)
        {
            var go = Take("P_" + kind, () => kind == PickupKind.Flower ? Props.MakeFlower().gameObject : Props.MakePowerUp(kind).gameObject);
            go.transform.SetParent(s.Root, false);
            go.transform.localPosition = new Vector3((lane - 1) * PlayerController.LaneW, y, z);
            var p = go.GetComponent<Pickup>();
            p.Attracted = false;
            s.Pickups.Add(p);
        }

        // ------------------------------------------------------------------ pooling
        GameObject Take(string key, System.Func<GameObject> make)
        {
            if (pools.TryGetValue(key, out var st) && st.Count > 0)
            {
                var g = st.Pop();
                g.SetActive(true);
                return g;
            }
            return make();
        }

        void Return(GameObject g, string key)
        {
            g.SetActive(false);
            g.transform.SetParent(poolRoot, false);
            if (!pools.TryGetValue(key, out var st)) pools[key] = st = new Stack<GameObject>();
            st.Push(g);
        }

        // ------------------------------------------------------------------ scenery construction
        Transform BuildDecor(int v, System.Random rng)
        {
            float F() { return (float)rng.NextDouble(); }
            var root = Prim.Empty("Segment" + v, null, Vector3.zero);
            float half = RoadW * 0.5f;

            // road + edges
            Prim.Make(PrimitiveType.Cube, root, new Vector3(0, -0.25f, SegLen * 0.5f), new Vector3(RoadW, 0.5f, SegLen + 0.05f), Mats.Lit("EDCFC9", 0f, 0.15f), "Road", false);
            for (int s = -1; s <= 1; s += 2)
            {
                Prim.Make(PrimitiveType.Cube, root, new Vector3(s * (half + 0.2f), -0.12f, SegLen * 0.5f), new Vector3(0.5f, 0.3f, SegLen + 0.05f), Mats.Lit(Pal.Wood), "Curb", false);
                Prim.Make(PrimitiveType.Cube, root, new Vector3(s * (half + 18f), -0.4f, SegLen * 0.5f), new Vector3(36f, 0.6f, SegLen + 0.05f),
                    Mats.Lit(v % 2 == 0 ? "F3D5E8" : "DDCBF2", 0.05f, 0.05f), "Ground", false);
            }
            // lane dashes
            var line = Mats.Lit(Pal.Cream, 0.15f);
            for (int s = -1; s <= 1; s += 2)
                for (int i = 0; i < 9; i++)
                    Prim.Make(PrimitiveType.Cube, root, new Vector3(s * PlayerController.LaneW * 0.5f, 0.012f, i * 4f + 1.2f), new Vector3(0.1f, 0.02f, 1.8f), line, "Dash", false);
            // stepping stones
            var stone = Mats.Lit(Pal.Stone);
            for (int i = 0; i < 4; i++)
                Prim.Make(PrimitiveType.Cube, root, new Vector3((F() - 0.5f) * (RoadW - 1.5f), 0.01f, F() * SegLen), new Vector3(0.7f, 0.03f, 0.5f), stone, "Pebble", false);

            bool torii = v % 3 == 0;
            float toriiZ = SegLen * 0.5f;
            if (torii) Scenery.Torii(root, new Vector3(0, 0, toriiZ));

            // avenue of sakura + lanterns
            for (int i = 0; i < 4; i++)
            {
                float z = 4.5f + i * SlotGap;
                if (torii && Mathf.Abs(z - toriiZ) < 3f) continue;
                for (int s = -1; s <= 1; s += 2)
                    Scenery.SakuraTree(root, new Vector3(s * (half + 1.6f + F() * 1.2f), 0, z + (F() - 0.5f) * 2f), 1.2f + F() * 0.5f);
                if (i % 2 == v % 2)
                    for (int s = -1; s <= 1; s += 2)
                        Scenery.StoneLantern(root, new Vector3(s * (half + 0.5f), 0, z + 4.5f), 1f);
            }
            // deeper woods
            for (int i = 0; i < 6; i++)
            {
                int s = i % 2 == 0 ? -1 : 1;
                Scenery.SakuraTree(root, new Vector3(s * (half + 6f + F() * 14f), 0, F() * SegLen), 1.3f + F() * 0.9f);
            }
            for (int i = 0; i < 2; i++)
                Scenery.Rocks(root, new Vector3((i == 0 ? -1 : 1) * (half + 3f + F() * 8f), 0, F() * SegLen), 0.8f + F());

            // landmarks
            if (v % 3 == 1)
            {
                int s = v % 2 == 0 ? -1 : 1;
                Scenery.Pagoda(root, new Vector3(s * (half + 12f + F() * 4f), 0, SegLen * 0.5f), 1.5f, s * -20f);
            }
            else if (v % 3 == 2)
            {
                int s = v % 2 == 0 ? 1 : -1;
                Scenery.Temple(root, new Vector3(s * (half + 14f), 0, SegLen * 0.4f), 1.4f, s * -90f);
            }
            if (v % 4 == 3)
                Scenery.Hill(root, new Vector3((v % 2 == 0 ? -1 : 1) * 45f, -0.5f, SegLen * 0.5f), 9f, 22f, Pal.Hex("D9C6EE"), Pal.Hex("F2D5EA"));

            return root.transform;
        }

        void BuildBackdrop()
        {
            backdrop = Prim.Empty("Backdrop", transform, Vector3.zero);
            Scenery.Fuji(backdrop, new Vector3(-8f, -1f, 235f), 70f, 95f);
            Scenery.Hill(backdrop, new Vector3(-90f, -1f, 215f), 22f, 70f, Pal.Hex("C9B3E6"), Pal.Hex("E9C9EA"));
            Scenery.Hill(backdrop, new Vector3(85f, -1f, 205f), 18f, 60f, Pal.Hex("CDB4F0"), Pal.Hex("F4CFE2"));
            var sun = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.DestroyImmediate(sun.GetComponent<Collider>());
            sun.name = "SunDisc";
            sun.transform.SetParent(backdrop, false);
            sun.transform.position = new Vector3(40f, 38f, 270f);
            sun.transform.localScale = Vector3.one * 34f;
            var r = sun.GetComponent<Renderer>();
            r.sharedMaterial = Mats.Glass(new Color(1f, 0.82f, 0.72f, 0.9f));
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }
}
