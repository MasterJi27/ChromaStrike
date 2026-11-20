using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using ChromaStrike.Core;
using ChromaStrike.Feedback;
using ChromaStrike.Player;
using ChromaStrike.Spheres;
using ChromaStrike.UI;

namespace ChromaStrike.EditorTools
{
    /// <summary>
    /// Builds the whole playable scene (Assets/Scenes/Game.unity) from the generated assets.
    /// Menu: Chroma Strike > 2. Build Project and Scene
    /// The scene is a normal Unity scene afterwards: every object can be selected and edited.
    /// </summary>
    public static class SceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Game.unity";
        private const string ConfigPath = "Assets/Settings/GameConfig.asset";

        private static TMP_FontAsset titleFont, bodyFont, boldFont;

        [MenuItem("Chroma Strike/2. Build Project and Scene")]
        public static void BuildAll()
        {
            ProjectSetup.EnsureFolders();
            // NewScene unloads unused assets, so the empty scene must exist BEFORE any asset is created and
            // held in a C# variable. Otherwise those references would silently turn into null in the saved scene.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            AssetPipeline.ConfigureTextures();
            AssetPipeline.ConfigureModels();
            AssetPipeline.ConfigureAudio();
            AssetDatabase.Refresh();

            AssetPipeline.CreatePipeline();
            VolumeProfile post = AssetPipeline.CreatePostProcessing("PostProcessing_Day", 0.15f, 8f, 0.28f, 0.22f);

            titleFont = AssetPipeline.MakeFont("BarlowCondensed-SemiBold");
            bodyFont = AssetPipeline.MakeFont("Barlow-Medium");
            boldFont = AssetPipeline.MakeFont("Barlow-Bold");

            Materials mats = BuildMaterials();
            Sphere[] spherePrefabs = BuildSpherePrefabs(mats);
            ParticleSystem popFx = BuildPopEffect();
            Dictionary<string, GameObject> props = BuildPropPrefabs(mats);
            GameConfig config = BuildConfig(spherePrefabs);

            BuildScene(config, mats, post, props, popFx);
            AssetDatabase.SaveAssets();
            Debug.Log("CHROMA_BUILD_OK");
        }

        // ------------------------------------------------------------------ materials
        private sealed class Materials
        {
            public Material floor, wall, trim, ground, crate, crateFrame, barrier, cone, coneBase, barrel, sky, particle;
            public Material red, green, blue;
        }

        private static Materials BuildMaterials()
        {
            var m = new Materials();
            m.floor = AssetPipeline.Lit("Floor", "floor_albedo.png", null, Color.white, 0.28f);
            AssetPipeline.AddDetail(m.floor, "concrete_normal.png", new Vector2(14, 14), 0.9f);
            m.wall = AssetPipeline.Lit("Wall", "wall_albedo.png", "wall_normal.png", Color.white, 0.12f, 0f, new Vector2(7, 1));
            m.trim = AssetPipeline.Lit("WallTrim", "concrete_albedo.png", "concrete_normal.png", new Color(0.55f, 0.55f, 0.57f), 0.2f, 0f, new Vector2(12, 1));
            m.ground = AssetPipeline.Lit("OuterGround", "concrete_albedo.png", null, new Color(0.42f, 0.46f, 0.40f), 0.05f, 0f, new Vector2(60, 60));
            m.crate = AssetPipeline.Lit("Crate", "crate_albedo.png", "crate_normal.png", Color.white, 0.18f);
            m.crateFrame = AssetPipeline.Lit("CrateFrame", "crate_frame_albedo.png", "crate_normal.png", Color.white, 0.2f);
            m.barrier = AssetPipeline.Lit("Barrier", "barrier_albedo.png", "barrier_normal.png", Color.white, 0.1f);
            m.cone = AssetPipeline.Lit("TrafficCone", "cone_albedo.png", null, Color.white, 0.45f);
            m.coneBase = AssetPipeline.Lit("ConeBase", null, null, new Color(0.07f, 0.07f, 0.08f), 0.35f);
            m.barrel = AssetPipeline.Lit("Barrel", "barrel_albedo.png", "barrel_normal.png", Color.white, 0.5f, 0.45f);
            m.sky = AssetPipeline.Skybox("DaySky", new Color(0.42f, 0.58f, 0.82f), new Color(0.40f, 0.42f, 0.41f), 1.05f, 0.045f, 1.15f);
            m.red = AssetPipeline.Lit("Sphere_Red", null, null, new Color(0.91f, 0.17f, 0.20f), 0.94f);
            m.green = AssetPipeline.Lit("Sphere_Green", null, null, new Color(0.14f, 0.72f, 0.40f), 0.94f);
            m.blue = AssetPipeline.Lit("Sphere_Blue", null, null, new Color(0.16f, 0.45f, 0.97f), 0.94f);
            AssetPipeline.MakeEmissive(m.red, new Color(0.91f, 0.17f, 0.20f), 0.10f);
            AssetPipeline.MakeEmissive(m.green, new Color(0.14f, 0.72f, 0.40f), 0.10f);
            AssetPipeline.MakeEmissive(m.blue, new Color(0.16f, 0.45f, 0.97f), 0.10f);

            m.particle = AssetDatabase.LoadAssetAtPath<Material>($"{AssetPipeline.MatDir}/PopParticle.mat");
            if (m.particle == null)
            {
                m.particle = new Material(Shader.Find("Sprites/Default"));
                AssetDatabase.CreateAsset(m.particle, $"{AssetPipeline.MatDir}/PopParticle.mat");
            }
            m.particle.mainTexture = AssetPipeline.Tex("ui_glow.png");
            EditorUtility.SetDirty(m.particle);
            return m;
        }

        // ------------------------------------------------------------------ prefabs
        private static Sphere[] BuildSpherePrefabs(Materials mats)
        {
            Mesh sphereMesh = GetPrimitiveMesh(PrimitiveType.Sphere);
            var result = new Sphere[3];
            Material[] materials = { mats.red, mats.green, mats.blue };

            for (int i = 0; i < 3; i++)
            {
                var color = (SphereColor)i;
                var root = new GameObject($"Sphere_{color}") { layer = AssetPipeline.LayerSpheres };
                var collider = root.AddComponent<SphereCollider>();
                collider.radius = 0.5f;

                var visual = new GameObject("Visual");
                visual.transform.SetParent(root.transform, false);
                visual.layer = AssetPipeline.LayerSpheres;
                visual.AddComponent<MeshFilter>().sharedMesh = sphereMesh;
                visual.AddComponent<MeshRenderer>().sharedMaterial = materials[i];

                var sphere = root.AddComponent<Sphere>();
                Wire.Enum(sphere, "color", i);

                string path = $"Assets/Prefabs/Sphere_{color}.prefab";
                result[i] = PrefabUtility.SaveAsPrefabAsset(root, path).GetComponent<Sphere>();
                Object.DestroyImmediate(root);
            }
            return result;
        }

        private static Mesh GetPrimitiveMesh(PrimitiveType type)
        {
            GameObject temp = GameObject.CreatePrimitive(type);
            Mesh mesh = temp.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(temp);
            return mesh;
        }

        private static ParticleSystem BuildPopEffect()
        {
            var go = new GameObject("PopEffect");
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = 0.4f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 6.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
            main.gravityModifier = 0.9f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 80;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 36) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.3f;

            var fade = ps.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;

            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0.1f));

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>($"{AssetPipeline.MatDir}/PopParticle.mat");
            renderer.renderMode = ParticleSystemRenderMode.Billboard;

            ParticleSystem prefab = PrefabUtility.SaveAsPrefabAsset(go, "Assets/Prefabs/PopEffect.prefab").GetComponent<ParticleSystem>();
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static Dictionary<string, GameObject> BuildPropPrefabs(Materials mats)
        {
            var result = new Dictionary<string, GameObject>();
            result["Crate"] = Prop("Crate", new Dictionary<string, Material> { { "Body", mats.crate }, { "Battens", mats.crateFrame } });
            result["JerseyBarrier"] = Prop("JerseyBarrier", new Dictionary<string, Material> { { "Body", mats.barrier } });
            result["TrafficCone"] = Prop("TrafficCone", new Dictionary<string, Material> { { "Body", mats.cone }, { "Base", mats.coneBase } });
            result["Barrel"] = Prop("Barrel", new Dictionary<string, Material> { { "Body", mats.barrel } });
            return result;
        }

        /// <summary>Instantiates an imported OBJ, assigns materials by part name, adds a collider and saves it as a prefab.</summary>
        private static GameObject Prop(string model, Dictionary<string, Material> byName)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"{AssetPipeline.ModelDir}/{model}.obj");
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            instance.name = model;
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            instance.layer = AssetPipeline.LayerEnvironment;

            Bounds bounds = default;
            bool first = true;
            foreach (MeshRenderer r in instance.GetComponentsInChildren<MeshRenderer>())
            {
                r.gameObject.layer = AssetPipeline.LayerEnvironment;
                if (byName.TryGetValue(r.name, out Material single)) r.sharedMaterial = single;
                else
                {
                    var list = new List<Material>(byName.Values);
                    var assigned = new Material[r.sharedMaterials.Length];
                    for (int i = 0; i < assigned.Length; i++) assigned[i] = list[Mathf.Min(i, list.Count - 1)];
                    r.sharedMaterials = assigned;
                }
                if (first) { bounds = r.bounds; first = false; } else bounds.Encapsulate(r.bounds);
            }

            if (model == "TrafficCone" || model == "Barrel")
            {
                var capsule = instance.AddComponent<CapsuleCollider>();
                capsule.center = bounds.center;
                capsule.height = bounds.size.y;
                capsule.radius = Mathf.Min(bounds.extents.x, bounds.extents.z) * (model == "Barrel" ? 1f : 0.6f);
            }
            else
            {
                var box = instance.AddComponent<BoxCollider>();
                box.center = bounds.center;
                box.size = bounds.size;
            }

            GameObjectUtility.SetStaticEditorFlags(instance, StaticEditorFlags.BatchingStatic | StaticEditorFlags.ContributeGI | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, $"Assets/Prefabs/{model}.prefab");
            Object.DestroyImmediate(instance);
            return prefab;
        }

        // ------------------------------------------------------------------ config
        private static GameConfig BuildConfig(Sphere[] prefabs)
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<GameConfig>();
                AssetDatabase.CreateAsset(config, ConfigPath);
            }

            config.spawnBlockers = (1 << AssetPipeline.LayerEnvironment) | (1 << AssetPipeline.LayerPlayer);
            config.shootMask = (1 << AssetPipeline.LayerEnvironment) | (1 << AssetPipeline.LayerSpheres);
            config.palette = new[]
            {
                new SphereColorDefinition { id = SphereColor.Red, displayName = "Red", color = new Color(0.91f, 0.17f, 0.20f), prefab = prefabs[0] },
                new SphereColorDefinition { id = SphereColor.Green, displayName = "Green", color = new Color(0.14f, 0.72f, 0.40f), prefab = prefabs[1] },
                new SphereColorDefinition { id = SphereColor.Blue, displayName = "Blue", color = new Color(0.16f, 0.45f, 0.97f), prefab = prefabs[2] },
            };
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            return config;
        }

        // ------------------------------------------------------------------ scene
        private static void BuildScene(GameConfig config, Materials mats, VolumeProfile post, Dictionary<string, GameObject> props, ParticleSystem popFx)
        {

            Transform environment = Folder("Environment");
            Transform propRoot = Folder("Props");
            Transform lighting = Folder("Lighting");

            BuildLighting(lighting, mats, post);
            BuildArena(environment, mats);
            BuildProps(propRoot, props);

            Transform playerRoot = BuildPlayer(config, out PlayerInputReader input, out CursorController cursor, out FirstPersonController fps,
                out ClickHandler clicker, out Camera cam);

            Transform systems = Folder("Game Systems");
            var container = new GameObject("Spheres").transform;
            container.SetParent(systems, false);

            AudioService audio = BuildAudio(systems);
            var spawner = Child<SpawnerManager>(systems, "Spawner Manager");
            Wire.Ref(spawner, "config", config);
            Wire.Ref(spawner, "player", playerRoot);
            Wire.Ref(spawner, "sphereContainer", container);

            var game = Child<GameManager>(systems, "Game Manager");
            Wire.Ref(game, "config", config);
            Wire.Ref(game, "spawner", spawner);
            Wire.Ref(game, "clickHandler", clicker);
            Wire.Ref(game, "player", fps);
            Wire.Ref(game, "cursor", cursor);

            var fx = Child<PopEffectPlayer>(systems, "Pop Effects");
            Wire.Ref(fx, "prefab", popFx);

            var feedback = Child<FeedbackController>(systems, "Feedback Controller");
            Wire.Ref(feedback, "config", config);
            Wire.Ref(feedback, "game", game);
            Wire.Ref(feedback, "spawner", spawner);
            Wire.Ref(feedback, "clickHandler", clicker);
            Wire.Ref(feedback, "audioService", audio);
            Wire.Ref(feedback, "popEffects", fx);

            BuildUi(config, game, spawner, clicker, audio);

            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        private static Transform Folder(string name) => new GameObject(name).transform;

        private static T Child<T>(Transform parent, string name) where T : Component
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.AddComponent<T>();
        }

        private static void BuildLighting(Transform parent, Materials mats, VolumeProfile post)
        {
            var sunGo = new GameObject("Sun");
            sunGo.transform.SetParent(parent, false);
            sunGo.transform.rotation = Quaternion.Euler(46f, -38f, 0f);
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.94f, 0.84f);
            sun.intensity = 2.4f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.9f;

            RenderSettings.sun = sun;
            RenderSettings.skybox = mats.sky;
            RenderSettings.ambientMode = AmbientMode.Skybox;
            RenderSettings.ambientIntensity = 1.1f;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.0075f;
            RenderSettings.fogColor = new Color(0.72f, 0.79f, 0.88f);

            var probeGo = new GameObject("Reflection Probe");
            probeGo.transform.SetParent(parent, false);
            probeGo.transform.position = new Vector3(0, 3f, 0);
            var probe = probeGo.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Realtime;
            probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
            probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.NoTimeSlicing;
            probe.resolution = 128;
            probe.hdr = true;
            probe.size = new Vector3(30f, 12f, 30f);

            var volumeGo = new GameObject("Post Processing Volume");
            volumeGo.transform.SetParent(parent, false);
            var volume = volumeGo.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = post;
        }

        private static GameObject Block(string name, Transform parent, Vector3 pos, Vector3 scale, Material material, bool collide = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            go.layer = AssetPipeline.LayerEnvironment;
            if (!collide) Object.DestroyImmediate(go.GetComponent<BoxCollider>());
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.ContributeGI | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
            return go;
        }

        private static void BuildArena(Transform parent, Materials mats)
        {
            Block("Floor", parent, new Vector3(0, -0.25f, 0), new Vector3(28f, 0.5f, 28f), mats.floor);
            Block("Outer Ground", parent, new Vector3(0, -0.6f, 0), new Vector3(400f, 0.2f, 400f), mats.ground, collide: false);

            const float half = 13.75f;   // wall centre
            const float length = 28.5f;
            Block("Wall North", parent, new Vector3(0, 2f, half), new Vector3(length, 4f, 0.5f), mats.wall);
            Block("Wall South", parent, new Vector3(0, 2f, -half), new Vector3(length, 4f, 0.5f), mats.wall);
            Block("Wall East", parent, new Vector3(half, 2f, 0), new Vector3(0.5f, 4f, length), mats.wall);
            Block("Wall West", parent, new Vector3(-half, 2f, 0), new Vector3(0.5f, 4f, length), mats.wall);

            // Cap and skirting give the walls a finished look instead of bare slabs.
            const float capHalf = 13.78f;
            foreach (float z in new[] { capHalf, -capHalf })
            {
                Block("Wall Cap", parent, new Vector3(0, 4.08f, z), new Vector3(29.1f, 0.16f, 0.72f), mats.trim, false);
                Block("Skirting", parent, new Vector3(0, 0.22f, z > 0 ? 13.46f : -13.46f), new Vector3(27f, 0.44f, 0.1f), mats.trim, false);
            }
            foreach (float x in new[] { capHalf, -capHalf })
            {
                Block("Wall Cap", parent, new Vector3(x, 4.08f, 0), new Vector3(0.72f, 0.16f, 29.1f), mats.trim, false);
                Block("Skirting", parent, new Vector3(x > 0 ? 13.46f : -13.46f, 0.22f, 0), new Vector3(0.1f, 0.44f, 27f), mats.trim, false);
            }
        }

        private static void BuildProps(Transform parent, Dictionary<string, GameObject> props)
        {
            void Place(string key, Vector3 pos, float yaw, float scale = 1f)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(props[key], parent);
                go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
                go.transform.localScale = Vector3.one * scale;
            }

            // Cover pieces inside the arena (the spawner avoids them automatically).
            Place("JerseyBarrier", new Vector3(-6.5f, 0, -6f), 0);
            Place("JerseyBarrier", new Vector3(6.5f, 0, -6f), 90);
            Place("JerseyBarrier", new Vector3(-6f, 0, 6.5f), 90);
            Place("JerseyBarrier", new Vector3(6f, 0, 6.5f), 0);
            Place("JerseyBarrier", new Vector3(0f, 0, 9f), 0);

            // Corner crate stacks.
            foreach (var corner in new[] { new Vector2(-1, -1), new Vector2(1, -1), new Vector2(-1, 1), new Vector2(1, 1) })
            {
                float x = corner.x * 12.2f, z = corner.y * 12.2f;
                Place("Crate", new Vector3(x, 0, z), 12f * corner.x);
                Place("Crate", new Vector3(x - corner.x * 1.05f, 0, z + 0.1f), -8f * corner.y);
                Place("Crate", new Vector3(x, 1f, z), 31f);
            }

            // Barrels along the east and west walls.
            for (int i = 0; i < 3; i++)
            {
                Place("Barrel", new Vector3(12.6f, 0, -2f + i * 0.72f), i * 37f);
                Place("Barrel", new Vector3(-12.6f, 0, 2.5f - i * 0.72f), i * 53f);
            }

            // Cones frame the player's start circle.
            foreach (var p in new[] { new Vector2(2.9f, 0), new Vector2(-2.9f, 0), new Vector2(0, 2.9f), new Vector2(0, -2.9f) })
                Place("TrafficCone", new Vector3(p.x, 0, p.y), 0);
        }

        private static Transform BuildPlayer(GameConfig config, out PlayerInputReader input, out CursorController cursor,
            out FirstPersonController fps, out ClickHandler clicker, out Camera camera)
        {
            var root = new GameObject("Player") { layer = AssetPipeline.LayerPlayer };
            root.transform.position = new Vector3(0, 0.02f, 0);

            var controller = root.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.35f;
            controller.center = new Vector3(0, 0.9f, 0);
            controller.stepOffset = 0.3f;
            controller.skinWidth = 0.03f;

            var pivot = new GameObject("Camera Pivot");
            pivot.transform.SetParent(root.transform, false);
            pivot.transform.localPosition = new Vector3(0, 1.65f, 0);

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            camGo.transform.SetParent(pivot.transform, false);
            camera = camGo.AddComponent<Camera>();
            camera.fieldOfView = 60f; // vertical FOV, about 91 degrees horizontal at 16:9: no stretched spheres at the screen edges
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 250f;
            camera.clearFlags = CameraClearFlags.Skybox;
            camGo.AddComponent<AudioListener>();
            var cameraData = camGo.AddComponent<UniversalAdditionalCameraData>();
            cameraData.renderPostProcessing = true;
            cameraData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            cameraData.antialiasingQuality = AntialiasingQuality.High;

            input = root.AddComponent<PlayerInputReader>();
            cursor = root.AddComponent<CursorController>();
            Wire.Ref(cursor, "input", input);

            fps = root.AddComponent<FirstPersonController>();
            Wire.Ref(fps, "config", config);
            Wire.Ref(fps, "input", input);
            Wire.Ref(fps, "cursor", cursor);
            Wire.Ref(fps, "cameraPivot", pivot.transform);

            clicker = root.AddComponent<ClickHandler>();
            Wire.Ref(clicker, "config", config);
            Wire.Ref(clicker, "playerCamera", camera);
            Wire.Ref(clicker, "input", input);
            Wire.Ref(clicker, "cursor", cursor);
            return root.transform;
        }

        private static AudioService BuildAudio(Transform parent)
        {
            var audio = Child<AudioService>(parent, "Audio Service");
            var so = new SerializedObject(audio);
            var list = so.FindProperty("effects");
            var map = new (SfxId id, string file, float volume, float lo, float hi)[]
            {
                (SfxId.UiClick, "ui_click", 0.7f, 1f, 1f), (SfxId.UiHover, "ui_hover", 0.35f, 0.95f, 1.05f),
                (SfxId.Pop, "sphere_pop", 0.9f, 0.88f, 1.15f), (SfxId.Spawn, "sphere_spawn", 0.35f, 0.95f, 1.1f),
                (SfxId.Batch, "batch_restore", 0.7f, 1f, 1f), (SfxId.GameStart, "game_start", 0.8f, 1f, 1f),
                (SfxId.GameEnd, "game_end", 0.85f, 1f, 1f), (SfxId.Tick, "tick", 0.5f, 1f, 1f), (SfxId.EmptyShot, "fire_click", 0.25f, 0.9f, 1.1f),
            };
            list.arraySize = map.Length;
            for (int i = 0; i < map.Length; i++)
            {
                SerializedProperty e = list.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("id").enumValueIndex = (int)map[i].id;
                e.FindPropertyRelative("clip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Audio/{map[i].file}.wav");
                e.FindPropertyRelative("volume").floatValue = map[i].volume;
                e.FindPropertyRelative("pitchRange").vector2Value = new Vector2(map[i].lo, map[i].hi);
            }
            so.FindProperty("ambience").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/ambience.wav");
            so.ApplyModifiedPropertiesWithoutUndo();
            return audio;
        }

        // ------------------------------------------------------------------ UI
        private static void BuildUi(GameConfig config, GameManager game, SpawnerManager spawner, ClickHandler clicker, AudioService audio)
        {
            var canvasGo = new GameObject("Canvas", typeof(RectTransform));
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var eventSystem = new GameObject("EventSystem", typeof(EventSystem));
            var module = eventSystem.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();

            var ui = canvasGo.AddComponent<UIManager>();
            Wire.Ref(ui, "config", config);
            Wire.Ref(ui, "game", game);
            Wire.Ref(ui, "spawner", spawner);
            Wire.Ref(ui, "clickHandler", clicker);

            // ---- HUD ------------------------------------------------------------
            PanelFader hud = Screen("HUD", canvasGo.transform);
            BuildCrosshair(hud.transform, ui);
            BuildTimer(hud.transform, ui);
            BuildCounters(hud.transform, ui);
            BuildBatchBar(hud.transform, ui);
            BuildToast(hud.transform, ui);
            Hint(hud.transform);

            // ---- Start / End --------------------------------------------------------
            PanelFader start = Screen("Start Screen", canvasGo.transform);
            BuildStartScreen(start.transform, ui, audio);
            PanelFader end = Screen("End Screen", canvasGo.transform);
            BuildEndScreen(end.transform, ui, audio);

            Wire.Ref(ui, "startScreen", start);
            Wire.Ref(ui, "hudScreen", hud);
            Wire.Ref(ui, "endScreen", end);
        }

        private static PanelFader Screen(string name, Transform parent)
        {
            RectTransform rt = UiFactory.Rect(name, parent);
            UiFactory.Stretch(rt);
            UiFactory.Group(rt.gameObject);
            return rt.gameObject.AddComponent<PanelFader>();
        }

        private static void BuildCrosshair(Transform parent, UIManager ui)
        {
            RectTransform root = UiFactory.Place(UiFactory.Rect("Crosshair", parent), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(64, 64));
            var view = root.gameObject.AddComponent<CrosshairView>();

            var parts = new List<Image>();
            Image outline = UiFactory.Img("Dot Outline", root, "ui_circle.png", new Color(0, 0, 0, 0.55f));
            UiFactory.Place(outline.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(11, 11));
            Image dot = UiFactory.Img("Dot", root, "ui_circle.png", new Color(1, 1, 1, 0.95f));
            UiFactory.Place(dot.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(6, 6));
            parts.Add(dot);

            foreach (var (name, offset, size) in new[]
            {
                ("Left", new Vector2(-17, 0), new Vector2(11, 2.5f)), ("Right", new Vector2(17, 0), new Vector2(11, 2.5f)),
                ("Top", new Vector2(0, 17), new Vector2(2.5f, 11)), ("Bottom", new Vector2(0, -17), new Vector2(2.5f, 11)),
            })
            {
                Image tick = UiFactory.Img("Tick " + name, root, null, new Color(1, 1, 1, 0.85f));
                UiFactory.Place(tick.rectTransform, new Vector2(0.5f, 0.5f), offset, size);
                parts.Add(tick);
            }

            RectTransform marker = UiFactory.Place(UiFactory.Rect("Hit Marker", root), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(64, 64));
            var group = marker.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0;
            foreach (float angle in new[] { 45f, 135f, 225f, 315f })
            {
                Image bar = UiFactory.Img("Bar", marker, null, new Color(1f, 0.35f, 0.3f, 1f));
                RectTransform r = UiFactory.Place(bar.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(12, 3.2f));
                r.localRotation = Quaternion.Euler(0, 0, angle);
                r.anchoredPosition = Quaternion.Euler(0, 0, angle) * new Vector3(19, 0, 0);
            }

            Wire.Ref(view, "hitMarker", group);
            Wire.Refs(view, "dotParts", parts.ToArray());
            Wire.Ref(ui, "crosshair", view);
        }

        private static void BuildTimer(Transform parent, UIManager ui)
        {
            Image bg = UiFactory.Img("Timer", parent, "ui_pill.png", new Color(0.06f, 0.065f, 0.08f, 0.8f));
            UiFactory.Place(bg.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -34), new Vector2(250, 98));
            var view = bg.gameObject.AddComponent<TimerView>();

            TextMeshProUGUI small = UiFactory.Label("Caption", bg.transform, "TIME REMAINING", bodyFont, 17, UiFactory.Muted, TextAlignmentOptions.Center, 5f);
            UiFactory.Place(small.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -9), new Vector2(240, 24));
            TextMeshProUGUI text = UiFactory.Label("Time", bg.transform, "02:00", titleFont, 62, UiFactory.Text, TextAlignmentOptions.Center, 3f);
            UiFactory.Place(text.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -8), new Vector2(240, 72));
            Image underline = UiFactory.Img("Underline", bg.transform, null, UiFactory.Text);
            UiFactory.Place(underline.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 8), new Vector2(110, 3.5f));

            Wire.Ref(view, "text", text);
            Wire.Ref(view, "underline", underline);
            Wire.Ref(ui, "timerView", view);
        }

        private static void BuildCounters(Transform parent, UIManager ui)
        {
            Image card = UiFactory.Img("Counters", parent, "ui_round.png", UiFactory.Panel);
            UiFactory.Place(card.rectTransform, new Vector2(1f, 1f), new Vector2(-34, -34), new Vector2(380, 270));

            TextMeshProUGUI head = UiFactory.Label("Header", card.transform, "LIVE SPHERE COUNT", bodyFont, 18, UiFactory.Muted, TextAlignmentOptions.Left, 5f);
            UiFactory.Place(head.rectTransform, new Vector2(0f, 1f), new Vector2(30, -20), new Vector2(320, 26));

            CounterRow[] rows = new CounterRow[3];
            for (int i = 0; i < 3; i++)
            {
                RectTransform rt = UiFactory.Rect(((SphereColor)i) + " Row", card.transform);
                UiFactory.Place(rt, new Vector2(0.5f, 1f), new Vector2(0, -(72 + i * 60)), new Vector2(340, 52));
                var row = rt.gameObject.AddComponent<CounterRow>();

                Image dot = UiFactory.Img("Dot", rt, "ui_circle.png", Color.white);
                UiFactory.Place(dot.rectTransform, new Vector2(0f, 0.5f), new Vector2(8, 0), new Vector2(22, 22));
                TextMeshProUGUI label = UiFactory.Label("Label", rt, "Red Spheres:", bodyFont, 31, UiFactory.Text, TextAlignmentOptions.Left);
                UiFactory.Place(label.rectTransform, new Vector2(0f, 0.5f), new Vector2(44, 0), new Vector2(210, 44));
                TextMeshProUGUI value = UiFactory.Label("Value", rt, "00", boldFont, 42, UiFactory.Text, TextAlignmentOptions.Right);
                UiFactory.Place(value.rectTransform, new Vector2(1f, 0.5f), new Vector2(-4, 0), new Vector2(90, 52));

                Wire.Ref(row, "dot", dot);
                Wire.Ref(row, "label", label);
                Wire.Ref(row, "value", value);
                rows[i] = row;
            }
            Wire.Ref(ui, "redRow", rows[0]);
            Wire.Ref(ui, "greenRow", rows[1]);
            Wire.Ref(ui, "blueRow", rows[2]);
        }

        private static void BuildBatchBar(Transform parent, UIManager ui)
        {
            Image bg = UiFactory.Img("Batch Progress", parent, "ui_pill.png", new Color(0.06f, 0.065f, 0.08f, 0.8f));
            UiFactory.Place(bg.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 46), new Vector2(480, 46));
            var view = bg.gameObject.AddComponent<BatchProgressView>();

            Image fill = UiFactory.Img("Fill", bg.transform, "ui_pill.png", UiFactory.Amber);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.fillAmount = 0;
            UiFactory.Stretch(fill.rectTransform);
            fill.rectTransform.offsetMin = new Vector2(5, 5);
            fill.rectTransform.offsetMax = new Vector2(-5, -5);
            fill.color = new Color(UiFactory.Amber.r, UiFactory.Amber.g, UiFactory.Amber.b, 0.85f);

            TextMeshProUGUI label = UiFactory.Label("Label", bg.transform, "NEXT BATCH  0/10", bodyFont, 21, UiFactory.Text, TextAlignmentOptions.Center, 4f);
            UiFactory.Stretch(label.rectTransform);

            Wire.Ref(view, "fill", fill);
            Wire.Ref(view, "label", label);
            Wire.Ref(ui, "batchProgress", view);
        }

        private static void BuildToast(Transform parent, UIManager ui)
        {
            RectTransform rt = UiFactory.Place(UiFactory.Rect("Toast", parent), new Vector2(0.5f, 0f), new Vector2(0, 126), new Vector2(1000, 56));
            rt.gameObject.AddComponent<CanvasGroup>();
            var view = rt.gameObject.AddComponent<ToastView>();
            TextMeshProUGUI text = UiFactory.Label("Text", rt, "", boldFont, 32, UiFactory.Amber, TextAlignmentOptions.Center, 3f);
            UiFactory.Stretch(text.rectTransform);
            Wire.Ref(view, "text", text);
            Wire.Ref(ui, "toast", view);
        }

        private static void Hint(Transform parent)
        {
            TextMeshProUGUI hint = UiFactory.Label("Controls Hint", parent, "WASD MOVE    MOUSE AIM    CLICK SHOOT    SHIFT SPRINT    ESC FREE CURSOR", bodyFont, 18,
                new Color(1, 1, 1, 0.55f), TextAlignmentOptions.Left, 3f);
            UiFactory.Place(hint.rectTransform, new Vector2(0f, 0f), new Vector2(34, 22), new Vector2(900, 26));
        }

        private static void BuildStartScreen(Transform parent, UIManager ui, AudioService audio)
        {
            Image dim = UiFactory.Img("Dim", parent, null, new Color(0.02f, 0.03f, 0.05f, 0.58f));
            UiFactory.Stretch(dim.rectTransform);

            Image card = UiFactory.Img("Card", parent, "ui_round.png", UiFactory.Panel);
            UiFactory.Place(card.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 780));
            Transform c = card.transform;

            Text(c, "FIRST PERSON TARGET TRAINING", bodyFont, 26, UiFactory.Amber, 0, 300, 800, 36, 9f);
            Text(c, "CHROMA STRIKE", titleFont, 128, UiFactory.Text, 0, 200, 840, 150, 5f).textWrappingMode = TextWrappingModes.NoWrap;

            for (int i = 0; i < 3; i++)
            {
                Image dot = UiFactory.Img("Dot " + i, c, "ui_circle.png", i == 0 ? new Color(0.91f, 0.17f, 0.20f) : i == 1 ? new Color(0.14f, 0.72f, 0.40f) : new Color(0.16f, 0.45f, 0.97f));
                UiFactory.Place(dot.rectTransform, new Vector2(0.5f, 0.5f), new Vector2((i - 1) * 46, 112), new Vector2(26, 26));
            }

            Text(c, "Shoot the coloured spheres before the clock runs out.\nEvery 10 you destroy come back in the same colour mix.",
                bodyFont, 31, UiFactory.Muted, 0, 30, 840, 100, 0f);
            Text(c, "120 SECONDS    |    30 SPHERES    |    +1 EVERY 5 SECONDS", bodyFont, 25, UiFactory.Amber, 0, -66, 820, 34, 3f);

            Button start = UiFactory.MakeButton("Start Button", c, "START GAME", boldFont, UiFactory.Amber, new Color(0.1f, 0.09f, 0.07f),
                new Vector2(0.5f, 0.5f), new Vector2(0, -160), new Vector2(460, 108), 44, audio);
            Button quit = UiFactory.MakeButton("Quit Button", c, "QUIT", bodyFont, new Color(1, 1, 1, 0.09f), UiFactory.Text,
                new Vector2(0.5f, 0.5f), new Vector2(0, -270), new Vector2(200, 62), 26, audio);

            Text(c, "W A S D  MOVE      MOUSE  AIM      LEFT CLICK  SHOOT      SHIFT  SPRINT", bodyFont, 21, UiFactory.Muted, 0, -338, 840, 28, 2f);

            Wire.Ref(ui, "startButton", start);
            Wire.Ref(ui, "quitFromMenuButton", quit);
        }

        private static void BuildEndScreen(Transform parent, UIManager ui, AudioService audio)
        {
            Image dim = UiFactory.Img("Dim", parent, null, new Color(0.02f, 0.03f, 0.05f, 0.66f));
            UiFactory.Stretch(dim.rectTransform);

            Image card = UiFactory.Img("Card", parent, "ui_round.png", UiFactory.Panel);
            UiFactory.Place(card.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1180, 800));
            Transform c = card.transform;

            Text(c, "ROUND COMPLETE", bodyFont, 26, UiFactory.Amber, 0, 330, 800, 36, 9f);
            Text(c, "TIME'S UP", titleFont, 140, UiFactory.Text, 0, 240, 900, 160, 6f).textWrappingMode = TextWrappingModes.NoWrap;
            Text(c, "FINAL SPHERE COUNT", bodyFont, 28, UiFactory.Muted, 0, 140, 800, 36, 8f);

            string[] names = { "RED SPHERES", "GREEN SPHERES", "BLUE SPHERES" };
            Color[] colors = { new Color(0.91f, 0.17f, 0.20f), new Color(0.14f, 0.72f, 0.40f), new Color(0.16f, 0.45f, 0.97f) };
            TextMeshProUGUI[] numbers = new TextMeshProUGUI[3];
            for (int i = 0; i < 3; i++)
            {
                Image tile = UiFactory.Img(names[i], c, "ui_round.png", UiFactory.Tile);
                UiFactory.Place(tile.rectTransform, new Vector2(0.5f, 0.5f), new Vector2((i - 1) * 350, -15), new Vector2(320, 270));
                Image bar = UiFactory.Img("Colour Bar", tile.transform, null, colors[i]);
                UiFactory.Place(bar.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -22), new Vector2(120, 8));
                numbers[i] = UiFactory.Label("Number", tile.transform, "0", boldFont, 150, UiFactory.Text, TextAlignmentOptions.Center);
                UiFactory.Place(numbers[i].rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 14), new Vector2(300, 170));
                TextMeshProUGUI label = UiFactory.Label("Label", tile.transform, names[i], bodyFont, 26, UiFactory.Muted, TextAlignmentOptions.Center, 6f);
                UiFactory.Place(label.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 34), new Vector2(300, 34));
            }

            TextMeshProUGUI summary = Text(c, "", bodyFont, 30, UiFactory.Muted, 0, -190, 1000, 40, 2f);
            Button restart = UiFactory.MakeButton("Restart Button", c, "RESTART", boldFont, UiFactory.Amber, new Color(0.1f, 0.09f, 0.07f),
                new Vector2(0.5f, 0.5f), new Vector2(-150, -290), new Vector2(380, 104), 42, audio);
            Button quit = UiFactory.MakeButton("Quit Button", c, "QUIT", bodyFont, new Color(1, 1, 1, 0.09f), UiFactory.Text,
                new Vector2(0.5f, 0.5f), new Vector2(190, -290), new Vector2(240, 104), 32, audio);

            Wire.Ref(ui, "endRed", numbers[0]);
            Wire.Ref(ui, "endGreen", numbers[1]);
            Wire.Ref(ui, "endBlue", numbers[2]);
            Wire.Ref(ui, "endSummary", summary);
            Wire.Ref(ui, "restartButton", restart);
            Wire.Ref(ui, "quitFromEndButton", quit);
        }

        private static TextMeshProUGUI Text(Transform parent, string text, TMP_FontAsset font, float size, Color color, float x, float y, float w, float h, float spacing)
        {
            TextMeshProUGUI t = UiFactory.Label("Text", parent, text, font, size, color, TextAlignmentOptions.Center, spacing);
            UiFactory.Place(t.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(x, y), new Vector2(w, h));
            return t;
        }
    }
}
