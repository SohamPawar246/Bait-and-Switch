using System.Collections.Generic;
using System.IO;
using BaitSwitch.Net;
using BaitSwitch.UI;
using BaitSwitch.View;
using kcp2k;
using Mirror;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Video;

namespace BaitSwitch.EditorTools
{
    /// <summary>
    /// Tools > Bait & Switch > Build Scene: builds Assets/Scenes/Sea.unity from code — the side-on sea (shader backdrop,
    /// seabed, kit terrain matching GameConfig's rock/kelp rects, set dressing, marine snow, post stack), the network
    /// objects, the main-menu diorama and the front end (AppFlow). Also writes Resources/ArtLibrary.
    /// </summary>
    public static class GreyboxBuilder
    {
        const string SceneDir = "Assets/Scenes";
        const string PrefabDir = "Assets/_Game/Prefabs";
        const string MatDir = "Assets/_Game/Materials";
        const string PartsDir = "Assets/_Game/Art/Parts";
        const string ScenePath = SceneDir + "/Sea.unity";
        const string FishPath = PrefabDir + "/FishGreybox.prefab";
        const string BoatPath = PrefabDir + "/BoatGreybox.prefab";
        const string HookPath = PrefabDir + "/HookGreybox.prefab";
        static readonly Vector3 MenuOrigin = new Vector3(1000f, 0f, 0f);

        [MenuItem("Tools/Bait & Switch/Build Scene")]
        public static void BuildScene() => Build();

        [MenuItem("Tools/Bait & Switch/Build Greybox")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var cfg = Resources.Load<GameConfig>("GameConfig");
            if (cfg == null) { Debug.LogError("[Scene] Resources/GameConfig missing. Run Build Part Catalog first."); return; }
            foreach (var d in new[] { SceneDir, PrefabDir, MatDir, "Assets/Resources" }) Directory.CreateDirectory(d);

            // ---------------------------------------------------------------- materials + library
            var lib = Library(cfg);
            var fishMat = Mat("Greybox_Fish", new Color(0.95f, 0.62f, 0.35f));
            var eyeMat = Mat("Greybox_Eye", new Color(0.08f, 0.08f, 0.1f));
            var hookMat = Mat("Greybox_Hook", new Color(0.75f, 0.77f, 0.8f));
            var baitMat = Mat("Greybox_Bait", new Color(0.85f, 0.2f, 0.25f));
            var fishPrefab = FishPrefab(fishMat, eyeMat, cfg.fishBaseSize);
            var boatPrefab = BoatPrefab(Mat("Greybox_Boat", new Color(0.95f, 0.8f, 0.2f)), hookMat, cfg.rodHeight);
            var hookPrefab = HookPrefab(hookMat, baitMat);
            var inkPrefab = BallPrefab("InkCloud", ToonMat("Toon_Ink", new Color(0.03f, 0.02f, 0.05f), Color.black, 0f));
            var markPrefab = BallPrefab("Mark", ToonMat("Toon_Mark", new Color(1f, 0.85f, 0.2f), new Color(1f, 0.7f, 0.1f), 0.01f));
            var outlinePrefab = BallPrefab("SonarOutline", ToonMat("Toon_Sonar", new Color(0.3f, 0.9f, 1f), new Color(0.2f, 0.8f, 1f), 0.01f));
            var sand = ToonMat("Toon_Sand", new Color(0.62f, 0.55f, 0.42f), Color.black, 0f);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            float midY = (cfg.surfaceY + cfg.seabedY) * 0.5f, depth = cfg.surfaceY - cfg.seabedY;

            // ---------------------------------------------------------------- game camera, light, post
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = false;
            cam.fieldOfView = 30f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.06f, 0.1f);
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 140f;
            camGo.transform.position = new Vector3(0f, midY + 1f, -22f);
            camGo.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;
            camGo.AddComponent<GameCamera>();   // Scale-the-Depths follow framing, shake, mouse ray

            var uiCam = new GameObject("UICamera").AddComponent<Camera>();
            uiCam.cullingMask = 0;
            uiCam.clearFlags = CameraClearFlags.SolidColor;
            uiCam.backgroundColor = Color.black;
            uiCam.depth = -100;
            uiCam.gameObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = false;

            var light = new GameObject("Sun").AddComponent<Light>();
            light.type = LightType.Directional;
            light.shadows = LightShadows.None;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.32f, 0.36f, 0.45f);
            PostVolume();

            // ---------------------------------------------------------------- the sea
            const float sky = 6f;
            var sea = Prim(PrimitiveType.Quad, "SeaBackdrop", lib.seaBackdrop);
            float w = cfg.seaSize.x + 34f, h = depth + sky;
            sea.transform.position = new Vector3(0f, midY, 40f);
            sea.transform.localScale = new Vector3(190f, 110f, 1f);
            lib.seaBackdrop.SetFloat("_SurfaceV", depth / h);
            lib.seaBackdrop.SetFloat("_Aspect", w / h);
            EditorUtility.SetDirty(lib.seaBackdrop);

            var bed = Prim(PrimitiveType.Cube, "Seabed", sand);
            bed.transform.position = new Vector3(0f, cfg.seabedY - 0.6f, 16f);
            bed.transform.localScale = new Vector3(190f, 1.2f, 50f);

            var terrain = new GameObject("Terrain").transform;
            Rocks(cfg, lib, terrain);
            Kelp(cfg, lib, terrain);
            Dressing(cfg, lib, terrain);
            Layers(cfg, lib, new GameObject("Parallax").transform);
            MarineSnow(cfg, depth);

            // ---------------------------------------------------------------- network + views
            var nmGo = new GameObject("NetworkManager");
            var kcp = nmGo.AddComponent<KcpTransport>();
            var lat = nmGo.AddComponent<LatencySimulation>();
            lat.wrap = kcp;
            lat.latency = 0f; lat.jitter = 0f; lat.unreliableLoss = 0f; lat.unreliableScramble = 0f; // set 150 / 2 for the lag test
            // browser players: WebSockets on 7778 (Edgegap's TLS sidecar makes it wss://). The server listens on both;
            // each client uses the first transport its platform supports (KCP on Windows, WebSockets in WebGL).
            var sws = nmGo.AddComponent<Mirror.SimpleWeb.SimpleWebTransport>();
            sws.port = 7778;
            sws.sslEnabled = false;
            sws.clientUseWss = true;
            sws.clientWebsocketSettings = new Mirror.SimpleWeb.ClientWebsocketSettings { ClientPortOption = Mirror.SimpleWeb.WebsocketPortOption.SpecifyPort, CustomClientPort = 7778 };
            var mux = nmGo.AddComponent<MultiplexTransport>();
            mux.transports = new Transport[] { lat, sws };
            nmGo.AddComponent<SeaServer>();
            nmGo.AddComponent<LocalPredictor>();
            nmGo.AddComponent<SeaClient>();
            var sv = nmGo.AddComponent<SeaView>();
            sv.fishPrefab = fishPrefab; sv.inkCloudPrefab = inkPrefab; sv.markPrefab = markPrefab; sv.outlinePrefab = outlinePrefab;
            var hv = nmGo.AddComponent<HookView>();
            hv.boatPrefab = boatPrefab; hv.hookPrefab = hookPrefab; hv.lineMaterial = LineMat();
            nmGo.AddComponent<GameHud>();
            var dn = nmGo.AddComponent<DayNight>();
            dn.sun = light; dn.cam = cam; dn.seaBackdrop = sea.GetComponent<Renderer>();
            var nm = nmGo.AddComponent<BSNetworkManager>();
            nm.transport = mux;
            nm.autoCreatePlayer = false;
            nm.headlessStartMode = HeadlessStartOptions.AutoStartServer;
            var broker = nmGo.AddComponent<EdgegapBroker>();
            var brokerUrl = System.IO.File.Exists("worker/broker_url.txt") ? System.IO.File.ReadAllText("worker/broker_url.txt").Trim() : "";
            if (brokerUrl.StartsWith("https://")) broker.brokerUrl = brokerUrl;
            nmGo.AddComponent<EdgegapAgent>();

            // ---------------------------------------------------------------- menu diorama + front end
            var diorama = MenuDiorama(lib, light);
            var flowGo = new GameObject("AppFlow");
            var flow = flowGo.AddComponent<AppFlow>();
            flow.diorama = diorama;
            flow.gameCamera = cam;
            flow.intro = AssetDatabase.LoadAssetAtPath<VideoClip>("Assets/_Game/Video/intro.mp4");

            EditorSceneManager.SaveScene(scene, ScenePath);
            var scenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(ScenePath, true) };
            foreach (var s in EditorBuildSettings.scenes) if (s.path != ScenePath) scenes.Add(s);
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("[Bait & Switch] Scene built: " + ScenePath);
        }

        // ==================================================================== library

        static ArtLibrary Library(GameConfig cfg)
        {
            const string path = "Assets/Resources/ArtLibrary.asset";
            var lib = AssetDatabase.LoadAssetAtPath<ArtLibrary>(path);
            if (lib == null) { lib = ScriptableObject.CreateInstance<ArtLibrary>(); AssetDatabase.CreateAsset(lib, path); }
            lib.toon = ShaderMat("Toon", "BaitSwitch/Toon");
            lib.seaBackdrop = ShaderMat("SeaBackdrop", "BaitSwitch/SeaBackdrop");
            lib.menuWater = ShaderMat("MenuWater", "BaitSwitch/MenuWater");
            lib.ditherImage = ShaderMat("DitherImage", "BaitSwitch/DitherImage");
            lib.ditherFade = ShaderMat("DitherFade", "BaitSwitch/DitherFade");
            lib.fx = ShaderMat("Fx", "BaitSwitch/Fx");
            lib.display = AssetDatabase.LoadAssetAtPath<Font>("Assets/_Game/Fonts/PirataOne-Regular.ttf");
            lib.body = AssetDatabase.LoadAssetAtPath<Font>("Assets/_Game/Fonts/CrimsonText-Regular.ttf");
            lib.bodyBold = AssetDatabase.LoadAssetAtPath<Font>("Assets/_Game/Fonts/CrimsonText-Bold.ttf");
            lib.boat = Part("boat_dinghy");
            lib.gull = Part("prop_seagull");
            lib.raincoat = Part("angler_raincoat");
            lib.lantern = Part("angler_lantern");
            lib.hats = Parts("hat_sou_wester", "hat_bucket", "hat_pointed_hood");
            lib.rods = Parts("rod_short", "rod_long", "rod_whip", "rod_heavy");
            lib.hooks = Parts("hook_j_hook", "hook_barbed", "hook_circle", "hook_treble", "hook_big", "hook_spiky");
            lib.baits = Parts("bait_worm", "bait_glow_orb", "bait_spinner", "bait_chunky_lure", "bait_stink_bait");
            lib.rocks = Parts("env_rock_small", "env_rock_medium", "env_rock_large");
            lib.kelp = Parts("env_kelp_a", "env_kelp_b", "env_kelp_c");
            lib.coral = Parts("env_coral_a", "env_coral_b");
            lib.arch = Part("env_rock_arch");
            lib.shrine = Part("prop_shrine");
            lib.candle = Part("prop_candle");
            lib.crate = Part("prop_offerings_crate");
            lib.skeleton = Part("prop_fish_skeleton");
            lib.eggSac = Part("prop_egg_sac");
            EditorUtility.SetDirty(lib);
            return lib;
        }

        static GameObject Part(string name) => AssetDatabase.LoadAssetAtPath<GameObject>($"{PartsDir}/{name}.fbx");

        static GameObject[] Parts(params string[] names)
        {
            var list = new List<GameObject>();
            foreach (var n in names) { var p = Part(n); if (p != null) list.Add(p); }
            return list.ToArray();
        }

        // ==================================================================== sea dressing

        static GameObject Dress(GameObject prefab, Transform parent, Vector3 pos, Vector3 scale, Quaternion rot, Color fill)
        {
            if (prefab == null) return null;
            var g = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            g.transform.SetPositionAndRotation(pos, rot);
            g.transform.localScale = scale;
            var td = g.AddComponent<ToonDressing>();
            td.fill = fill;
            return g;
        }

        static Bounds MeshBounds(GameObject prefab)
        {
            var mf = prefab != null ? prefab.GetComponentInChildren<MeshFilter>() : null;
            return mf != null && mf.sharedMesh != null ? mf.sharedMesh.bounds : new Bounds(Vector3.zero, Vector3.one);
        }

        /// <summary>Kit rocks stretched onto each collision rect, so what you see is what blocks the hook.</summary>
        static void Rocks(GameConfig cfg, ArtLibrary lib, Transform parent)
        {
            if (lib.rocks == null || lib.rocks.Length == 0) return;
            int i = 0;
            foreach (var r in cfg.rocks)
            {
                var prefab = lib.rocks[Mathf.Min(lib.rocks.Length - 1, 1 + (i++ % 2))];
                var b = MeshBounds(prefab);
                var scale = new Vector3(r.width / b.size.x * 1.08f, r.height / b.size.y * 1.08f, 2.2f / b.size.z);
                var pos = new Vector3(r.center.x, r.center.y, 0.6f) - Vector3.Scale(b.center, scale);
                Dress(prefab, parent, pos, scale, Quaternion.identity, Color.white);
            }
        }

        /// <summary>Kelp forests: fronds across each kelp rect, alternating in front of and behind the fish.</summary>
        static void Kelp(GameConfig cfg, ArtLibrary lib, Transform parent)
        {
            if (lib.kelp == null || lib.kelp.Length == 0) return;
            var rng = new System.Random(11);
            foreach (var r in cfg.kelp)
            {
                int n = Mathf.Max(3, Mathf.RoundToInt(r.width / 0.7f));
                for (int k = 0; k < n; k++)
                {
                    var prefab = lib.kelp[rng.Next(lib.kelp.Length)];
                    var b = MeshBounds(prefab);
                    float hgt = r.height * (0.7f + 0.3f * (float)rng.NextDouble());
                    float s = hgt / Mathf.Max(0.1f, b.size.y);
                    float x = r.xMin + (k + 0.5f) * r.width / n + ((float)rng.NextDouble() - 0.5f) * 0.3f;
                    float z = k % 2 == 0 ? -0.9f : 1.1f;
                    var g = Dress(prefab, parent, new Vector3(x, cfg.seabedY - b.min.y * s, z), Vector3.one * s, Quaternion.Euler(0f, rng.Next(360), 0f), Color.white);
                    if (g != null) g.GetComponent<ToonDressing>().sway = 0.35f;
                }
            }
        }

        /// <summary>Cult set dressing on the seabed (behind the fish): coral, a shrine with candles, a skeleton, a crate, an egg sac.</summary>
        static void Dressing(GameConfig cfg, ArtLibrary lib, Transform parent)
        {
            float y = cfg.seabedY;
            var back = Quaternion.Euler(0f, 180f, 0f);
            void At(GameObject p, float x, float z, float s, Quaternion r)
            {
                if (p == null) return;
                var b = MeshBounds(p);
                Dress(p, parent, new Vector3(x, y - b.min.y * s, z), Vector3.one * s, r, Color.white);
            }
            At(lib.shrine, 4.5f, 2.2f, 2.2f, back);
            At(lib.candle, 3.4f, 1.6f, 2.4f, Quaternion.identity);
            At(lib.candle, 5.7f, 1.6f, 2.0f, Quaternion.identity);
            At(lib.candle, 6.3f, 2.4f, 1.6f, Quaternion.identity);
            At(lib.skeleton, -5.2f, 1.4f, 2.2f, back);
            At(lib.crate, 16.5f, 2.4f, 1.8f, back);
            At(lib.eggSac, -16.5f, 2.0f, 2.2f, back);
            if (lib.coral != null)
                for (int i = 0; i < 6; i++)
                    if (lib.coral.Length > 0) At(lib.coral[i % lib.coral.Length], -18f + i * 7.1f, 2.6f + (i % 2) * 0.6f, 1.4f + (i % 3) * 0.3f, Quaternion.Euler(0f, i * 57f, 0f));
            if (lib.rocks != null && lib.rocks.Length > 0)
                for (int i = 0; i < 5; i++) At(lib.rocks[0], -14f + i * 7.3f, 2.8f, 1.2f + (i % 2) * 0.6f, Quaternion.Euler(0f, i * 83f, 0f));
        }

        /// <summary>
        /// Depth for the perspective camera: far rock ridges and kelp forests that dissolve into the water colour,
        /// a mid layer, a few dark foreground fronds and boulders, and two enormous silhouettes drifting very slowly.
        /// None of it is gameplay (collision lives in cfg.rocks / cfg.kelp); the shader fogs it by depth.
        /// </summary>
        static void Layers(GameConfig cfg, ArtLibrary lib, Transform parent)
        {
            var rng = new System.Random(23);
            float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
            void Ground(GameObject p, float x, float z, float s, float sway, float yaw)
            {
                if (p == null) return;
                var b = MeshBounds(p);
                var g = Dress(p, parent, new Vector3(x, cfg.seabedY - b.min.y * s - 0.05f, z), Vector3.one * s, Quaternion.Euler(0f, yaw, 0f), Color.white);
                if (g != null && sway > 0f) g.GetComponent<ToonDressing>().sway = sway;
            }
            bool rocks = lib.rocks != null && lib.rocks.Length > 0, kelp = lib.kelp != null && lib.kelp.Length > 0;
            // far ridge: huge rocks and an arch, 16-26 units back
            if (rocks)
                for (int i = 0; i < 14; i++)
                    Ground(lib.rocks[rng.Next(lib.rocks.Length)], -48f + i * 7.4f + R(-2f, 2f), R(16f, 26f), R(5f, 9.5f), 0f, R(0f, 360f));
            if (lib.arch != null) Ground(lib.arch, R(-12f, 12f), 20f, 5.5f, 0f, 180f);
            // far kelp forest
            if (kelp)
                for (int i = 0; i < 26; i++)
                    Ground(lib.kelp[rng.Next(lib.kelp.Length)], -46f + i * 3.6f + R(-1f, 1f), R(9f, 15f), R(2.2f, 3.6f), 0.5f, R(0f, 360f));
            // mid layer
            if (rocks)
                for (int i = 0; i < 10; i++)
                    Ground(lib.rocks[rng.Next(lib.rocks.Length)], -36f + i * 7.8f + R(-2f, 2f), R(4.5f, 7f), R(1.6f, 3f), 0f, R(0f, 360f));
            if (lib.coral != null && lib.coral.Length > 0)
                for (int i = 0; i < 12; i++)
                    Ground(lib.coral[rng.Next(lib.coral.Length)], -32f + i * 5.6f + R(-1f, 1f), R(3.5f, 6f), R(1.4f, 2.4f), 0.15f, R(0f, 360f));
            // foreground: dark, out-of-focus fronds and boulders at the very bottom of the frame
            if (kelp)
                for (int i = 0; i < 9; i++)
                    Ground(lib.kelp[rng.Next(lib.kelp.Length)], -30f + i * 7.5f + R(-2f, 2f), R(-5.5f, -4f), R(0.9f, 1.4f), 0.4f, R(0f, 360f));
            if (rocks)
                for (int i = 0; i < 6; i++)
                    Ground(lib.rocks[rng.Next(lib.rocks.Length)], -28f + i * 11f + R(-3f, 3f), R(-5f, -3.5f), R(1.2f, 2f), 0f, R(0f, 360f));
            // the far background: enormous silhouettes drifting very slowly, never explained
            for (int i = 0; i < 2; i++)
            {
                var d = new GameObject("DeepDrifter").AddComponent<DeepDrifter>();
                d.transform.SetParent(parent, false);
                d.seed = 31 + i * 17;
                d.z = 30f + i * 6f;
                d.y = cfg.seabedY * 0.55f + i * 2.5f;
                d.scale = 9f + i * 3f;
                d.speed = 0.25f + i * 0.12f;
                d.startX = i == 0 ? -20f : 35f;
                d.direction = i == 0 ? 1f : -1f;
            }
        }

        static void MarineSnow(GameConfig cfg, float depth)
        {
            var go = new GameObject("MarineSnow");
            go.transform.position = new Vector3(0f, (cfg.surfaceY + cfg.seabedY) * 0.5f, 2f);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 14f;
            main.startSpeed = 0.06f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.09f);
            main.startColor = new Color(0.9f, 0.95f, 1f, 0.35f);
            main.maxParticles = 900;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.prewarm = true;
            var em = ps.emission;
            em.rateOverTime = 64f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(cfg.seaSize.x + 20f, depth, 12f);
            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);
            vel.y = new ParticleSystem.MinMaxCurve(-0.12f, -0.04f);
            vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.08f;
            noise.frequency = 0.3f;
            var r = go.GetComponent<ParticleSystemRenderer>();
            var m = AssetDatabase.LoadAssetAtPath<Material>($"{MatDir}/Snow.mat");
            if (m == null) { m = new Material(Shader.Find("Sprites/Default")); AssetDatabase.CreateAsset(m, $"{MatDir}/Snow.mat"); }
            r.sharedMaterial = m;
        }

        static void PostVolume()
        {
            const string path = MatDir + "/SeaPost.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (profile == null) { profile = ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(profile, path); }
            T Get<T>() where T : VolumeComponent
            {
                if (!profile.TryGet<T>(out var c)) { c = profile.Add<T>(true); AssetDatabase.AddObjectToAsset(c, profile); }
                c.active = true;
                return c;
            }
            var bloom = Get<Bloom>();
            bloom.threshold.Override(0.95f); bloom.intensity.Override(0.9f); bloom.scatter.Override(0.65f);
            var vig = Get<Vignette>();
            vig.intensity.Override(0.32f); vig.smoothness.Override(0.45f);
            var grain = Get<FilmGrain>();
            grain.type.Override(FilmGrainLookup.Medium1); grain.intensity.Override(0.16f);
            var col = Get<ColorAdjustments>();
            col.contrast.Override(10f); col.saturation.Override(-8f);
            EditorUtility.SetDirty(profile);
            var vol = new GameObject("Post").AddComponent<Volume>();
            vol.isGlobal = true;
            vol.sharedProfile = profile;
        }

        // ==================================================================== menu diorama

        static MenuDiorama MenuDiorama(ArtLibrary lib, Light sun)
        {
            var root = new GameObject("MenuDiorama").transform;
            root.position = MenuOrigin;
            var water = Prim(PrimitiveType.Plane, "Water", lib.menuWater);
            water.transform.SetParent(root, false);
            water.transform.localScale = new Vector3(7f, 1f, 7f);

            Transform boat = null, gull = null, hat = null, lantern = null;
            if (lib.boat != null)
            {
                var b = Dress(lib.boat, root, Vector3.zero, Vector3.one * 1.75f, Quaternion.identity, new Color(0.9f, 0.88f, 0.8f));
                // half-sunken like the references: bow (−X) raised toward the upper left, stern sinking under the waterline
                b.transform.localPosition = new Vector3(0f, 0.02f, 0f);
                b.transform.localRotation = Quaternion.Euler(3f, 24f, -15f);
                boat = b.transform;
                Transform bow = null;
                foreach (var t in b.GetComponentsInChildren<Transform>()) if (FishAssembler.Canon(t.name) == "socket_bow") bow = t;
                if (lib.gull != null && bow != null)
                {
                    var g = Dress(lib.gull, root, bow.position, Vector3.one * 1.6f, Quaternion.Euler(0f, 24f + 160f, 0f), new Color(0.95f, 0.94f, 0.92f));
                    gull = g.transform;
                }
                if (lib.lantern != null)
                {
                    var l = Dress(lib.lantern, b.transform, b.transform.TransformPoint(new Vector3(-0.15f, -0.05f, 0.1f)), Vector3.one * 1.6f, b.transform.rotation, Color.white);
                    if (l != null) { l.GetComponent<ToonDressing>().glow = new Color(0.95f, 0.76f, 0.31f); lantern = l.transform; }
                }
            }
            if (lib.hats != null && lib.hats.Length > 0)
            {
                var h = Dress(lib.hats[0], root, root.position + new Vector3(3.6f, -0.08f, -2.2f), Vector3.one * 1.7f, Quaternion.Euler(10f, 40f, -14f), new Color(0.95f, 0.76f, 0.31f));
                hat = h.transform;
            }

            var camGo = new GameObject("MenuCamera");
            camGo.transform.SetParent(root, false);
            // oblique close-up like the references: ~52° down, the boat filling the right two thirds
            var target = root.position + new Vector3(-1.5f, 0f, 0.45f);
            camGo.transform.position = target + new Vector3(0f, 6.3f, -4.9f);
            camGo.transform.LookAt(target);
            var mc = camGo.AddComponent<Camera>();
            mc.fieldOfView = 40f;
            mc.nearClipPlane = 0.3f;
            mc.farClipPlane = 40f;
            mc.clearFlags = CameraClearFlags.SolidColor;
            mc.backgroundColor = new Color(0.1f, 0.12f, 0.14f);
            camGo.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = false;

            var d = root.gameObject.AddComponent<MenuDiorama>();
            d.cam = mc; d.water = water.GetComponent<Renderer>(); d.boat = boat; d.gull = gull; d.hat = hat; d.lantern = lantern; d.sun = sun;
            return d;
        }

        // ==================================================================== small builders

        static GameObject FishPrefab(Material body, Material eye, float size)
        {
            var root = new GameObject("FishGreybox");
            var b = Prim(PrimitiveType.Capsule, "Body", body);
            b.transform.SetParent(root.transform, false);
            b.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            b.transform.localScale = new Vector3(size * 0.5f, size * 0.5f, size * 0.5f);
            var e = Prim(PrimitiveType.Sphere, "Eye", eye);
            e.transform.SetParent(root.transform, false);
            e.transform.localPosition = new Vector3(size * 0.28f, size * 0.07f, -size * 0.2f);
            e.transform.localScale = Vector3.one * size * 0.12f;
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, FishPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        static GameObject BallPrefab(string name, Material m)
        {
            var go = Prim(PrimitiveType.Sphere, name, m);
            var p = PrefabUtility.SaveAsPrefabAsset(go, $"{PrefabDir}/{name}.prefab");
            Object.DestroyImmediate(go);
            return p;
        }

        static GameObject BoatPrefab(Material boat, Material rod, float rodHeight)
        {
            var root = new GameObject("BoatGreybox");
            var hull = Prim(PrimitiveType.Cube, "Hull", boat);
            hull.transform.SetParent(root.transform, false);
            hull.transform.localScale = new Vector3(1.6f, 0.35f, 0.7f);
            var r = Prim(PrimitiveType.Cylinder, "Rod", rod);
            r.transform.SetParent(root.transform, false);
            r.transform.localPosition = new Vector3(0f, rodHeight * 0.5f, 0f);
            r.transform.localScale = new Vector3(0.05f, rodHeight * 0.5f, 0.05f);
            var p = PrefabUtility.SaveAsPrefabAsset(root, BoatPath);
            Object.DestroyImmediate(root);
            return p;
        }

        static GameObject HookPrefab(Material hook, Material bait)
        {
            var root = new GameObject("HookGreybox");
            var h = Prim(PrimitiveType.Sphere, "Hook", hook);
            h.transform.SetParent(root.transform, false);
            h.transform.localScale = Vector3.one * 0.3f;
            var b = Prim(PrimitiveType.Sphere, "Bait", bait);
            b.transform.SetParent(root.transform, false);
            b.transform.localPosition = new Vector3(0f, -0.22f, -0.05f);
            b.transform.localScale = Vector3.one * 0.28f;
            var p = PrefabUtility.SaveAsPrefabAsset(root, HookPath);
            Object.DestroyImmediate(root);
            return p;
        }

        static Material LineMat()
        {
            string path = $"{MatDir}/Greybox_Line.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
                m = new Material(shader);
                AssetDatabase.CreateAsset(m, path);
            }
            m.SetColor("_BaseColor", new Color(0.9f, 0.9f, 0.85f));
            EditorUtility.SetDirty(m);
            return m;
        }

        static GameObject Prim(PrimitiveType t, string name, Material m)
        {
            var go = GameObject.CreatePrimitive(t);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = m;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            return go;
        }

        static Material ShaderMat(string name, string shader)
        {
            string path = $"{MatDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            var sh = Shader.Find(shader);
            if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, path); }
            else if (sh != null && m.shader != sh) m.shader = sh;
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material ToonMat(string name, Color c, Color emission, float outline)
        {
            var m = ShaderMat(name, "BaitSwitch/Toon");
            m.SetColor("_BaseColor", c);
            m.SetColor("_EmissionColor", emission);
            m.SetFloat("_OutlineWidth", outline);
            return m;
        }

        static Material Mat(string name, Color c)
        {
            string path = $"{MatDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                m = new Material(shader);
                AssetDatabase.CreateAsset(m, path);
            }
            m.SetColor("_BaseColor", c);
            m.color = c;
            EditorUtility.SetDirty(m);
            return m;
        }
    }
}
