using UnityEngine;
using UnityEngine.Rendering;

namespace SakuraRunner
{
    /// <summary>
    /// Zero-setup entry point: builds the whole game at runtime in any scene (an empty scene works).
    /// </summary>
    public static class GameBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            if (Object.FindObjectOfType<GameManager>() != null) return;

            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;

            var root = new GameObject("SakuraRunner").transform;

            SetupEnvironment(root);
            var cam = SetupCamera(root, out var rig);

            AudioManager.Create(root);

            var gmGo = new GameObject("GameManager");
            gmGo.transform.SetParent(root, false);
            var gm = gmGo.AddComponent<GameManager>();

            var playerGo = new GameObject("Player");
            playerGo.transform.SetParent(root, false);
            var player = playerGo.AddComponent<PlayerController>();
            player.Rig = rig;
            player.Build();
            rig.Player = player;

            var worldGo = new GameObject("World");
            worldGo.transform.SetParent(root, false);
            var world = worldGo.AddComponent<WorldGenerator>();
            world.Player = player;

            gm.Player = player;
            gm.World = world;
            world.Build();

            Fx.Petals(rig.transform);
            Fx.Motes(rig.transform);

            var uiGo = new GameObject("UI");
            uiGo.transform.SetParent(root, false);
            var ui = uiGo.AddComponent<UIManager>();
            // GameManager.Start() runs next frame; UI needs the instance now.
            ui.Build();
        }

        static Camera SetupCamera(Transform root, out CameraRig rig)
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var cg = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = cg.AddComponent<Camera>();
            }
            foreach (var l in Object.FindObjectsOfType<AudioListener>()) Object.Destroy(l);

            cam.clearFlags = CameraClearFlags.Skybox;
            cam.backgroundColor = Pal.Hex("F2C6DD");
            cam.nearClipPlane = 0.2f;
            cam.farClip = 700f;
            cam.fieldOfView = 58f;
            cam.allowHDR = true;

            rig = cam.gameObject.GetComponent<CameraRig>();
            if (rig == null) rig = cam.gameObject.AddComponent<CameraRig>();
            rig.Cam = cam;
            return cam;
        }

        static void SetupEnvironment(Transform root)
        {
            // pink-lavender sunset sky
            var sky = Shader.Find("Skybox/Procedural");
            if (sky != null)
            {
                var m = new Material(sky);
                m.SetColor("_SkyTint", Pal.Hex("E8A9D6"));
                m.SetColor("_GroundColor", Pal.Hex("F4D0DE"));
                m.SetFloat("_AtmosphereThickness", 1.6f);
                m.SetFloat("_Exposure", 1.25f);
                m.SetFloat("_SunSize", 0.0f);
                RenderSettings.skybox = m;
            }

            // soft ambient + fog tinted like the horizon
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = Pal.Hex("D9C2EA");
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Pal.Hex("F3C8DE");
            RenderSettings.fogStartDistance = 55f;
            RenderSettings.fogEndDistance = 330f;

            // warm low sun, lighting the runner from the camera side
            foreach (var old in Object.FindObjectsOfType<Light>())
                if (old.type == LightType.Directional) Object.Destroy(old.gameObject);

            var lg = new GameObject("Sun");
            lg.transform.SetParent(root, false);
            lg.transform.rotation = Quaternion.Euler(38f, 28f, 0f);
            var light = lg.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = Pal.Hex("FFDCC4");
            light.intensity = 1.25f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.55f;

            // gentle cool fill from behind for the dreamy rim
            var fg = new GameObject("Fill");
            fg.transform.SetParent(root, false);
            fg.transform.rotation = Quaternion.Euler(20f, 200f, 0f);
            var fill = fg.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = Pal.Hex("C7B3F5");
            fill.intensity = 0.45f;
            fill.shadows = LightShadows.None;

            QualitySettings.shadowDistance = 45f;
        }
    }
}
