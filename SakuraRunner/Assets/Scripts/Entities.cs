using UnityEngine;

namespace SakuraRunner
{
    public enum ObstacleKind { Low, High, Full }   // Low = jump over, High = slide under, Full = change lane
    public enum PickupKind { Flower, Magnet, Shield, Speed }

    /// <summary>Axis-aligned hazard. Collision box is defined relative to the transform.</summary>
    public class Obstacle : MonoBehaviour
    {
        public ObstacleKind Kind;
        public int Variant;
        public Vector3 Center;
        public Vector3 Half;
        public Vector3 WorldCenter => transform.position + Center;
    }

    /// <summary>Collectible: flower or power-up orb. Spins and bobs.</summary>
    public class Pickup : MonoBehaviour
    {
        public PickupKind Kind;
        public float Radius = 0.5f;
        public bool Attracted;
        public Transform Visual;
        float phase;

        void OnEnable()
        {
            phase = Random.value * 6.28f;
            Attracted = false;
            if (Visual != null) Visual.localPosition = Vector3.zero;
        }

        void Update()
        {
            if (Visual == null) return;
            float t = Time.time;
            Visual.localRotation = Quaternion.Euler(0f, t * (Kind == PickupKind.Flower ? 140f : 90f) + phase * 40f, 0f);
            Visual.localPosition = new Vector3(0f, Mathf.Sin(t * 3f + phase) * 0.1f, 0f);
        }
    }

    /// <summary>Factory for all gameplay props (obstacles and pickups).</summary>
    public static class Props
    {
        // ------------------------------------------------------------------ obstacles
        public static Obstacle MakeObstacle(ObstacleKind kind, int variant)
        {
            var g = new GameObject("Obstacle_" + kind + variant);
            var o = g.AddComponent<Obstacle>();
            o.Kind = kind;
            o.Variant = variant;
            var t = g.transform;

            var wood = Mats.Lit(Pal.Wood);
            var coral = Mats.Lit(Pal.Coral);
            var stone = Mats.Lit(Pal.Stone);
            var bark = Mats.Lit(Pal.Bark);
            var pink = Mats.Lit(Pal.Pink, 0.12f);
            var glow = Mats.Lit(Pal.Hex("FFD9A0"), 1.3f);
            var lantern = Mats.Lit(Pal.Hex("FF9AA8"), 1.0f);

            switch (kind)
            {
                case ObstacleKind.Low:
                    o.Center = new Vector3(0, 0.5f, 0); o.Half = new Vector3(0.9f, 0.5f, 0.4f);
                    if (variant == 0)
                    {   // wooden fence with a ribbon
                        for (int i = -1; i <= 1; i++)
                            Prim.Make(PrimitiveType.Cylinder, t, new Vector3(i * 0.85f, 0.5f, 0), new Vector3(0.14f, 0.5f, 0.14f), wood);
                        Prim.Make(PrimitiveType.Cube, t, new Vector3(0, 0.38f, 0), new Vector3(1.9f, 0.1f, 0.12f), wood);
                        Prim.Make(PrimitiveType.Cube, t, new Vector3(0, 0.78f, 0), new Vector3(1.9f, 0.1f, 0.12f), wood);
                        Prim.Make(PrimitiveType.Cube, t, new Vector3(0, 0.58f, 0.08f), new Vector3(0.5f, 0.3f, 0.04f), coral);
                    }
                    else
                    {   // stone planter with a blossom bush
                        Prim.Make(PrimitiveType.Cube, t, new Vector3(0, 0.25f, 0), new Vector3(1.7f, 0.5f, 0.8f), stone);
                        Prim.Make(PrimitiveType.Cube, t, new Vector3(0, 0.55f, 0), new Vector3(1.85f, 0.1f, 0.9f), stone);
                        Prim.Make(PrimitiveType.Sphere, t, new Vector3(-0.45f, 0.78f, 0), Vector3.one * 0.7f, pink);
                        Prim.Make(PrimitiveType.Sphere, t, new Vector3(0.2f, 0.82f, 0.05f), Vector3.one * 0.75f, pink);
                        Prim.Make(PrimitiveType.Sphere, t, new Vector3(0.6f, 0.72f, -0.05f), Vector3.one * 0.55f, Mats.Lit(Pal.PinkLight, 0.2f));
                    }
                    break;

                case ObstacleKind.High:
                    o.Center = new Vector3(0, 1.45f, 0); o.Half = new Vector3(0.95f, 0.3f, 0.3f);
                    if (variant == 0)
                    {   // lantern bar
                        Prim.Make(PrimitiveType.Cylinder, t, new Vector3(-1f, 1.1f, 0), new Vector3(0.12f, 1.1f, 0.12f), wood);
                        Prim.Make(PrimitiveType.Cylinder, t, new Vector3(1f, 1.1f, 0), new Vector3(0.12f, 1.1f, 0.12f), wood);
                        Prim.Make(PrimitiveType.Cube, t, new Vector3(0, 1.72f, 0), new Vector3(2.2f, 0.16f, 0.2f), coral);
                        for (int i = -1; i <= 1; i++)
                            Prim.Make(PrimitiveType.Sphere, t, new Vector3(i * 0.62f, 1.4f, 0), new Vector3(0.42f, 0.55f, 0.42f), lantern, "Lantern", false);
                    }
                    else
                    {   // low blossom branch
                        Prim.Make(PrimitiveType.Cylinder, t, new Vector3(-1.05f, 1.1f, 0), new Vector3(0.22f, 1.1f, 0.22f), bark);
                        Prim.Make(PrimitiveType.Cylinder, t, new Vector3(1.05f, 1.1f, 0), new Vector3(0.22f, 1.1f, 0.22f), bark);
                        var br = Prim.Make(PrimitiveType.Cylinder, t, new Vector3(0, 1.62f, 0), new Vector3(0.16f, 1.05f, 0.16f), bark);
                        br.transform.localRotation = Quaternion.Euler(0, 0, 90f);
                        for (int i = -1; i <= 1; i++)
                            Prim.Make(PrimitiveType.Sphere, t, new Vector3(i * 0.65f, 1.4f + (i == 0 ? 0.08f : 0f), 0), Vector3.one * 0.6f, pink);
                    }
                    break;

                default: // Full
                    o.Center = new Vector3(0, 1.3f, 0); o.Half = new Vector3(0.75f, 1.3f, 0.55f);
                    if (variant == 0)
                    {   // giant stone lantern
                        Prim.Make(PrimitiveType.Cube, t, new Vector3(0, 0.2f, 0), new Vector3(1.1f, 0.4f, 1.1f), stone);
                        Prim.Make(PrimitiveType.Cylinder, t, new Vector3(0, 0.9f, 0), new Vector3(0.45f, 0.55f, 0.45f), stone);
                        Prim.Make(PrimitiveType.Cube, t, new Vector3(0, 1.65f, 0), new Vector3(0.95f, 0.8f, 0.95f), glow);
                        Prim.FromMesh(MeshUtil.Pyramid(), t, new Vector3(0, 2.05f, 0), new Vector3(1.6f, 0.65f, 1.6f), stone, "Roof");
                        Prim.Make(PrimitiveType.Sphere, t, new Vector3(0, 2.75f, 0), Vector3.one * 0.25f, stone);
                    }
                    else
                    {   // festival stall
                        Prim.Make(PrimitiveType.Cube, t, new Vector3(0, 0.6f, 0), new Vector3(1.6f, 1.2f, 1.0f), wood);
                        Prim.Make(PrimitiveType.Cylinder, t, new Vector3(-0.7f, 1.5f, 0.4f), new Vector3(0.1f, 0.75f, 0.1f), wood);
                        Prim.Make(PrimitiveType.Cylinder, t, new Vector3(0.7f, 1.5f, 0.4f), new Vector3(0.1f, 0.75f, 0.1f), wood);
                        Prim.FromMesh(MeshUtil.Pyramid(), t, new Vector3(0, 1.95f, 0), new Vector3(1.95f, 0.6f, 1.4f), Mats.Lit(Pal.PinkDeep), "Awning");
                        Prim.Make(PrimitiveType.Sphere, t, new Vector3(0, 1.45f, 0.5f), new Vector3(0.4f, 0.5f, 0.4f), lantern, "Lantern", false);
                        Prim.Make(PrimitiveType.Cube, t, new Vector3(0, 2.4f, 0), new Vector3(0.2f, 0.45f, 0.2f), Mats.Lit(Pal.Cream));
                    }
                    break;
            }
            return o;
        }

        // ------------------------------------------------------------------ pickups
        public static Pickup MakeFlower()
        {
            var g = new GameObject("Flower");
            var p = g.AddComponent<Pickup>();
            p.Kind = PickupKind.Flower;
            p.Radius = 0.5f;
            var v = Prim.Empty("Visual", g.transform, Vector3.zero);
            p.Visual = v;
            var petal = Mats.Lit(Pal.Hex("FFC4DD"), 0.8f);
            var core = Mats.Lit(Pal.PeachDeep, 1f);
            for (int i = 0; i < 5; i++)
            {
                float a = i * 72f;
                float r = a * Mathf.Deg2Rad;
                var pt = Prim.Make(PrimitiveType.Sphere, v, new Vector3(Mathf.Sin(r) * 0.17f, Mathf.Cos(r) * 0.17f, 0),
                    new Vector3(0.22f, 0.3f, 0.07f), petal, "Petal", false);
                pt.transform.localRotation = Quaternion.Euler(0, 0, -a);
            }
            Prim.Make(PrimitiveType.Sphere, v, new Vector3(0, 0, 0.02f), Vector3.one * 0.13f, core, "Core", false);
            Prim.Make(PrimitiveType.Sphere, v, Vector3.zero, Vector3.one * 0.95f, Mats.Glass(new Color(1f, 0.8f, 0.9f, 0.16f)), "Halo", false);
            return p;
        }

        public static Pickup MakePowerUp(PickupKind kind)
        {
            var g = new GameObject("PowerUp_" + kind);
            var p = g.AddComponent<Pickup>();
            p.Kind = kind;
            p.Radius = 0.75f;
            var v = Prim.Empty("Visual", g.transform, Vector3.zero);
            p.Visual = v;

            Color c = kind == PickupKind.Magnet ? Pal.Hex("C9A8FF") : kind == PickupKind.Shield ? Pal.Sky : Pal.PeachDeep;
            var emis = Mats.Lit(c, 1.1f);
            var white = Mats.Lit(Color.white, 0.9f);
            Prim.Make(PrimitiveType.Sphere, v, Vector3.zero, Vector3.one * 1.1f, Mats.Glass(new Color(c.r, c.g, c.b, 0.35f)), "Orb", false);

            switch (kind)
            {
                case PickupKind.Magnet:
                    Prim.Make(PrimitiveType.Cube, v, new Vector3(-0.15f, -0.02f, 0), new Vector3(0.11f, 0.4f, 0.11f), Mats.Lit(Pal.Coral, 0.6f), "Arm", false);
                    Prim.Make(PrimitiveType.Cube, v, new Vector3(0.15f, -0.02f, 0), new Vector3(0.11f, 0.4f, 0.11f), Mats.Lit(Pal.Coral, 0.6f), "Arm", false);
                    Prim.Make(PrimitiveType.Cube, v, new Vector3(0, -0.2f, 0), new Vector3(0.41f, 0.11f, 0.11f), Mats.Lit(Pal.Coral, 0.6f), "Base", false);
                    Prim.Make(PrimitiveType.Cube, v, new Vector3(-0.15f, 0.22f, 0), new Vector3(0.12f, 0.1f, 0.12f), white, "Tip", false);
                    Prim.Make(PrimitiveType.Cube, v, new Vector3(0.15f, 0.22f, 0), new Vector3(0.12f, 0.1f, 0.12f), white, "Tip", false);
                    break;
                case PickupKind.Shield:
                    Prim.Make(PrimitiveType.Sphere, v, Vector3.zero, new Vector3(0.42f, 0.5f, 0.12f), emis, "Plate", false);
                    Prim.Make(PrimitiveType.Sphere, v, new Vector3(0, 0, 0.04f), new Vector3(0.24f, 0.3f, 0.1f), white, "Inner", false);
                    break;
                default: // Speed: two chevrons
                    for (int k = 0; k < 2; k++)
                        for (int s = -1; s <= 1; s += 2)
                        {
                            var b = Prim.Make(PrimitiveType.Cube, v, new Vector3(s * 0.09f, -0.12f + k * 0.2f, 0),
                                new Vector3(0.28f, 0.08f, 0.09f), k == 0 ? emis : white, "Chevron", false);
                            b.transform.localRotation = Quaternion.Euler(0, 0, -s * 40f);
                        }
                    break;
            }
            return p;
        }
    }
}
