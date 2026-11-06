using UnityEngine;

namespace SakuraRunner
{
    /// <summary>Builders for decorative scenery: sakura trees, lanterns, torii, pagoda, temple, Mount Fuji.</summary>
    public static class Scenery
    {
        static Material[] blossom;

        static Material[] Blossom()
        {
            if (blossom == null)
                blossom = new[]
                {
                    Mats.Lit("FFB7D5", 0.15f, 0.1f), Mats.Lit("FFC9DE", 0.2f, 0.1f),
                    Mats.Lit("F9A8CE", 0.12f, 0.1f), Mats.Lit("FFDCEB", 0.25f, 0.1f)
                };
            return blossom;
        }

        static GameObject Rot(GameObject g, float x, float y, float z)
        {
            g.transform.localRotation = Quaternion.Euler(x, y, z);
            return g;
        }

        public static Transform SakuraTree(Transform parent, Vector3 pos, float s)
        {
            var r = Prim.Empty("Sakura", parent, pos);
            r.localScale = Vector3.one * s;
            r.localRotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
            var bark = Mats.Lit(Pal.Bark);
            var b = Blossom();
            Prim.Make(PrimitiveType.Cylinder, r, new Vector3(0, 1.5f, 0), new Vector3(0.45f, 1.5f, 0.45f), bark, "Trunk");
            Rot(Prim.Make(PrimitiveType.Cylinder, r, new Vector3(0.55f, 3.1f, 0), new Vector3(0.2f, 0.9f, 0.2f), bark, "Branch"), 0, 0, -40);
            Rot(Prim.Make(PrimitiveType.Cylinder, r, new Vector3(-0.55f, 3.0f, 0.1f), new Vector3(0.2f, 0.9f, 0.2f), bark, "Branch"), 0, 0, 40);
            Prim.Make(PrimitiveType.Sphere, r, new Vector3(0, 4.6f, 0), Vector3.one * 4.4f, b[0], "Crown");
            Prim.Make(PrimitiveType.Sphere, r, new Vector3(1.6f, 4.0f, 0.4f), Vector3.one * 3.2f, b[1], "Crown");
            Prim.Make(PrimitiveType.Sphere, r, new Vector3(-1.7f, 4.1f, -0.3f), Vector3.one * 3.4f, b[2], "Crown");
            Prim.Make(PrimitiveType.Sphere, r, new Vector3(0.3f, 5.6f, 0.5f), Vector3.one * 3.0f, b[3], "Crown");
            Prim.Make(PrimitiveType.Sphere, r, new Vector3(-0.4f, 3.9f, 1.4f), Vector3.one * 2.8f, b[Random.Range(0, 4)], "Crown");
            Prim.Make(PrimitiveType.Cylinder, r, new Vector3(0, 0.03f, 0), new Vector3(6.5f, 0.01f, 6.5f),
                Mats.Glass(new Color(1f, 0.78f, 0.88f, 0.55f)), "FallenPetals", false);
            return r;
        }

        public static Transform StoneLantern(Transform parent, Vector3 pos, float s = 1f)
        {
            var r = Prim.Empty("Lantern", parent, pos);
            r.localScale = Vector3.one * s;
            var stone = Mats.Lit(Pal.Stone);
            Prim.Make(PrimitiveType.Cube, r, new Vector3(0, 0.09f, 0), new Vector3(0.55f, 0.18f, 0.55f), stone);
            Prim.Make(PrimitiveType.Cylinder, r, new Vector3(0, 0.5f, 0), new Vector3(0.2f, 0.45f, 0.2f), stone);
            Prim.Make(PrimitiveType.Cube, r, new Vector3(0, 1.15f, 0), new Vector3(0.44f, 0.38f, 0.44f), Mats.Lit("FFD9A0", 1.4f), "Glow", false);
            Prim.FromMesh(MeshUtil.Pyramid(), r, new Vector3(0, 1.34f, 0), new Vector3(0.8f, 0.36f, 0.8f), stone, "Roof");
            Prim.Make(PrimitiveType.Sphere, r, new Vector3(0, 1.72f, 0), Vector3.one * 0.14f, stone);
            return r;
        }

        /// <summary>Big torii gate spanning the road. Beams are high enough to clear the camera.</summary>
        public static Transform Torii(Transform parent, Vector3 pos)
        {
            var r = Prim.Empty("Torii", parent, pos);
            var red = Mats.Lit(Pal.Coral, 0.08f);
            var dark = Mats.Lit(Pal.Ink);
            for (int s = -1; s <= 1; s += 2)
            {
                Prim.Make(PrimitiveType.Cylinder, r, new Vector3(s * 4.6f, 3.3f, 0), new Vector3(0.55f, 3.3f, 0.55f), red, "Pillar");
                Prim.Make(PrimitiveType.Cylinder, r, new Vector3(s * 4.6f, 0.2f, 0), new Vector3(0.75f, 0.2f, 0.75f), dark, "PillarBase");
            }
            Prim.Make(PrimitiveType.Cube, r, new Vector3(0, 5.35f, 0), new Vector3(9.8f, 0.4f, 0.4f), red, "Nuki");
            Prim.Make(PrimitiveType.Cube, r, new Vector3(0, 6.55f, 0), new Vector3(12f, 0.5f, 0.7f), dark, "Kasagi");
            Prim.Make(PrimitiveType.Cube, r, new Vector3(0, 6.1f, 0), new Vector3(10.4f, 0.35f, 0.5f), red, "Shimaki");
            Rot(Prim.Make(PrimitiveType.Cube, r, new Vector3(-6.1f, 6.7f, 0), new Vector3(1.0f, 0.4f, 0.7f), dark, "TipL"), 0, 0, -15);
            Rot(Prim.Make(PrimitiveType.Cube, r, new Vector3(6.1f, 6.7f, 0), new Vector3(1.0f, 0.4f, 0.7f), dark, "TipR"), 0, 0, 15);
            Prim.Make(PrimitiveType.Cube, r, new Vector3(0, 5.85f, 0), new Vector3(0.7f, 0.9f, 0.18f), Mats.Lit(Pal.Cream, 0.3f), "Plaque");
            return r;
        }

        public static Transform Pagoda(Transform parent, Vector3 pos, float s, float yaw)
        {
            var r = Prim.Empty("Pagoda", parent, pos);
            r.localScale = Vector3.one * s;
            r.localRotation = Quaternion.Euler(0, yaw, 0);
            var wall = Mats.Lit(Pal.Cream);
            var post = Mats.Lit(Pal.Coral);
            var roof = Mats.Lit(Pal.LavenderDeep, 0.05f);
            var trim = Mats.Lit(Pal.PinkDeep);
            Prim.Make(PrimitiveType.Cube, r, new Vector3(0, 0.4f, 0), new Vector3(8f, 0.8f, 8f), Mats.Lit(Pal.Stone), "Base");
            float y = 0.8f;
            for (int i = 0; i < 5; i++)
            {
                float w = 4.6f - i * 0.6f;
                Prim.Make(PrimitiveType.Cube, r, new Vector3(0, y + 0.8f, 0), new Vector3(w, 1.6f, w), wall, "Tier");
                Prim.Make(PrimitiveType.Cube, r, new Vector3(0, y + 0.8f, 0), new Vector3(w + 0.15f, 0.25f, w + 0.15f), post, "Band", false);
                Prim.FromMesh(MeshUtil.Pyramid(), r, new Vector3(0, y + 1.5f, 0), new Vector3(w + 2.6f, 1.1f, w + 2.6f), roof, "Roof");
                Prim.Make(PrimitiveType.Cube, r, new Vector3(0, y + 1.5f, 0), new Vector3(w + 2.6f, 0.12f, w + 2.6f), trim, "Eave", false);
                y += 1.9f;
            }
            Prim.Make(PrimitiveType.Cylinder, r, new Vector3(0, y + 1.3f, 0), new Vector3(0.14f, 1.3f, 0.14f), Mats.Lit(Pal.Hex("FFD27A"), 0.4f), "Spire");
            for (int i = 0; i < 4; i++)
                Prim.Make(PrimitiveType.Cylinder, r, new Vector3(0, y + 0.6f + i * 0.5f, 0), new Vector3(0.5f - i * 0.08f, 0.04f, 0.5f - i * 0.08f), Mats.Lit(Pal.Hex("FFD27A"), 0.4f), "Ring", false);
            return r;
        }

        public static Transform Temple(Transform parent, Vector3 pos, float s, float yaw)
        {
            var r = Prim.Empty("Temple", parent, pos);
            r.localScale = Vector3.one * s;
            r.localRotation = Quaternion.Euler(0, yaw, 0);
            var wall = Mats.Lit(Pal.Cream);
            var post = Mats.Lit(Pal.Coral);
            var roof = Mats.Lit(Pal.LavenderDeep, 0.05f);
            Prim.Make(PrimitiveType.Cube, r, new Vector3(0, 0.35f, 0), new Vector3(11f, 0.7f, 8.5f), Mats.Lit(Pal.Stone), "Platform");
            for (int i = 0; i < 3; i++)
                Prim.Make(PrimitiveType.Cube, r, new Vector3(0, 0.12f + i * 0.2f, 4.9f + (2 - i) * 0.5f), new Vector3(3f, 0.24f, 0.6f), Mats.Lit(Pal.Stone), "Step", false);
            Prim.Make(PrimitiveType.Cube, r, new Vector3(0, 2.3f, 0), new Vector3(8f, 3.2f, 5.6f), wall, "Hall");
            for (int i = 0; i < 6; i++)
                Prim.Make(PrimitiveType.Cylinder, r, new Vector3(-3.75f + i * 1.5f, 2.3f, 2.95f), new Vector3(0.32f, 1.6f, 0.32f), post, "Column");
            Prim.Make(PrimitiveType.Cube, r, new Vector3(0, 2.0f, 2.85f), new Vector3(2.2f, 2.2f, 0.12f), Mats.Lit("FFD9A0", 0.9f), "Door", false);
            Prim.FromMesh(MeshUtil.Pyramid(), r, new Vector3(0, 3.7f, 0), new Vector3(12.2f, 2.4f, 9.2f), roof, "Roof");
            Prim.Make(PrimitiveType.Cube, r, new Vector3(0, 3.72f, 0), new Vector3(12.2f, 0.18f, 9.2f), Mats.Lit(Pal.PinkDeep), "Eave", false);
            Prim.FromMesh(MeshUtil.Pyramid(), r, new Vector3(0, 5.5f, 0), new Vector3(6.5f, 1.6f, 4.5f), roof, "UpperRoof");
            StoneLantern(r, new Vector3(-4.5f, 0.7f, 4.5f), 1.1f);
            StoneLantern(r, new Vector3(4.5f, 0.7f, 4.5f), 1.1f);
            return r;
        }

        public static Transform Rocks(Transform parent, Vector3 pos, float s)
        {
            var r = Prim.Empty("Rocks", parent, pos);
            var m = Mats.Lit(Pal.Stone, 0f, 0.1f);
            for (int i = 0; i < 3; i++)
                Prim.Make(PrimitiveType.Sphere, r, new Vector3(i * 0.9f - 0.9f, 0.25f * s, Random.Range(-.4f, .4f)),
                    new Vector3(Random.Range(.9f, 1.5f), Random.Range(.6f, 1f), Random.Range(.9f, 1.4f)) * s, m, "Rock");
            return r;
        }

        // ---- backdrop (unlit, vertex coloured so fog does not wash it out) --------------------

        /// <summary>Mount Fuji with a soft snow line. Unit-sized: scale it up when placing.</summary>
        public static GameObject Fuji(Transform parent, Vector3 pos, float height, float radius)
        {
            var prof = new Vector2[28];
            for (int i = 0; i < prof.Length; i++)
            {
                float t = i / (float)(prof.Length - 1);
                float rr = Mathf.Pow(1f - t, 1.9f) * 0.92f + 0.03f * (1f - t);
                prof[i] = new Vector2(rr, t);
            }
            var mesh = MeshUtil.Lathe(prof, 48, "Fuji");
            var verts = mesh.vertices;
            var cols = new Color[verts.Length];
            Color low = Pal.Hex("C9B8EE"), high = Pal.Hex("8F7BC8"), snow = Pal.Hex("FFF1F6");
            for (int i = 0; i < verts.Length; i++)
            {
                float h = verts[i].y;
                float ang = Mathf.Atan2(verts[i].z, verts[i].x);
                float line = 0.66f + 0.05f * Mathf.Sin(ang * 6f) + 0.03f * Mathf.Sin(ang * 13f + 1f) - (1f - h) * 0f;
                Color c = Color.Lerp(low, high, Mathf.Clamp01(h * 1.3f));
                float sn = Mathf.Clamp01((h - line) * 18f);
                cols[i] = Color.Lerp(c, snow, sn);
            }
            mesh.colors = cols;
            var g = Prim.FromMesh(mesh, parent, pos, new Vector3(radius, height, radius), Mats.Glass(Color.white), "MountFuji", false);
            return g;
        }

        /// <summary>A soft rounded hill silhouette.</summary>
        public static GameObject Hill(Transform parent, Vector3 pos, float height, float radius, Color low, Color high)
        {
            var prof = new Vector2[12];
            for (int i = 0; i < prof.Length; i++)
            {
                float t = i / (float)(prof.Length - 1);
                prof[i] = new Vector2(Mathf.Cos(t * Mathf.PI * 0.5f) * 0.98f + 0.02f, t * t * 0.4f + t * 0.6f);
            }
            var mesh = MeshUtil.Lathe(prof, 32, "Hill");
            var verts = mesh.vertices;
            var cols = new Color[verts.Length];
            for (int i = 0; i < verts.Length; i++) cols[i] = Color.Lerp(low, high, verts[i].y);
            mesh.colors = cols;
            return Prim.FromMesh(mesh, parent, pos, new Vector3(radius, height, radius * 0.6f), Mats.Glass(Color.white), "Hill", false);
        }
    }
}
