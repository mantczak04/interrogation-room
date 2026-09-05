using System;
using System.Collections.Generic;
using System.Linq;
using InterrogationRoom.Gameplay.Interaction;
using Mirror;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>
/// Editor-only composition of the Blender-authored station. Existing gameplay
/// objects keep their identities and bindings; no round rules live here.
/// Run once on the original Room scene. The guard prevents accidental double moves.
/// </summary>
public static class StationRebuildSetup
{
    private const string Folder = "Assets/Art/Environment/StationRebuild";
    private const string ModelPath = Folder + "/StationRebuild.fbx";
    private const string ScenePath = "Assets/Scenes/Room.unity";
    private const string RootName = "Map_Station";

    [Serializable] private sealed class Layout
    {
        public Room[] rooms;
        public Door[] doors;
        public Fixture[] lights;
        public Window[] windows;
    }
    [Serializable] private sealed class Room
    {
        public string key, id, label;
        public float x, z, width, depth;
    }
    [Serializable] private sealed class Door
    {
        public string name, a, b, label;
        public float x, z, angle;
    }
    [Serializable] private sealed class Fixture
    {
        public string name;
        public float x, z;
        public bool central;
    }
    [Serializable] private sealed class Window
    {
        public float x, z;
        public string axis;
    }

    [MenuItem("Tools/Interrogation Room/Station Rebuild/1 Import architecture")]
    public static void ImportArchitecture()
    {
        RequireEditMode();
        var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
        if (importer == null) throw new InvalidOperationException("Export the Blender architecture first.");
        importer.globalScale = 1;
        importer.useFileScale = true;
        importer.generateSecondaryUV = true;
        importer.secondaryUVPackMargin = 8;
        importer.isReadable = true;
        importer.importCameras = false;
        importer.importLights = false;
        importer.SaveAndReimport();
        Debug.Log("[StationRebuild] Architecture imported with lightmap UVs.");
    }

    [MenuItem("Tools/Interrogation Room/Station Rebuild/2 Compose Room")]
    public static void Compose()
    {
        RequireEditMode();
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath || GameObject.Find(RootName) != null)
            throw new InvalidOperationException("Composition requires the original Room scene without Map_Station.");
        GameObject old = GameObject.Find("Map_Graybox");
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        Layout layout = ReadLayout();
        if (old == null || source == null || layout.rooms.Length != 14 || layout.doors.Length != 14)
            throw new InvalidOperationException("Station inputs are incomplete.");

        var root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Compose station");
        var materials = CreateMaterials();
        CreateShell(root.transform, source, materials);

        Transform furniture = old.transform.Find("Meble");
        furniture.SetParent(root.transform, true);
        MoveFurniture(furniture);
        Transform doors = old.transform.Find("Drzwi");
        doors.SetParent(root.transform, true);
        CreateRooms(root.transform, layout);
        ConfigureDoors(doors, layout, materials);
        MoveGameplay();
        FurnishNewRooms(furniture);
        foreach (NetworkStartPosition spawn in Object.FindObjectsByType<NetworkStartPosition>(FindObjectsSortMode.None))
            spawn.transform.position += new Vector3(0, .04f, 4.5f);
        Place("SpawnPoint_W", new Vector3(-1f,.04f,9.7f));
        Place("SpawnPoint_E", new Vector3(1f,.04f,9.7f));
        // Two additional starts support the approved eight-player maximum.
        foreach (float x in new[] { -3f, 3f })
        {
            var spawn = new GameObject("SpawnPoint_Additional_" + x);
            spawn.transform.position = new Vector3(x, .04f, 13f);
            spawn.transform.rotation = Quaternion.Euler(0, 180, 0);
            spawn.AddComponent<NetworkStartPosition>();
        }

        old.name = "Map_PreRebuild_Disabled";
        old.SetActive(false);
        GameObject.Find("Directional Light")?.SetActive(false);
        GameObject.Find("Swiatlo_Postaci_Przesluchania")?.SetActive(false);
        ConfigureLighting(root.transform, layout);
        ReplaceDoorVisuals();
        var camera = GameObject.Find("MapOverviewCamera").GetComponent<Camera>();
        camera.transform.SetPositionAndRotation(new Vector3(0, 1.7f, 13.2f), Quaternion.Euler(4, 180, 0));
        camera.nearClipPlane = .08f;
        camera.farClipPlane = 100;
        Lightmapping.Clear();
        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[StationRebuild] Composed ten rooms, fourteen doors and eight spawn points. Bake and validate next.");
    }

    [MenuItem("Tools/Interrogation Room/Station Rebuild/3 Refresh architecture")]
    public static void RefreshArchitecture()
    {
        RequireEditMode();
        GameObject root = GameObject.Find(RootName);
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (root == null || source == null || SceneManager.GetActiveScene().path != ScenePath)
            throw new InvalidOperationException("Open the composed Room scene first.");
        Transform previous = root.transform.Find("BlenderArchitecture");
        if (previous != null) Object.DestroyImmediate(previous.gameObject);
        CreateShell(root.transform, source, CreateMaterials());
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
    }

    private static void CreateShell(Transform parent, GameObject source, Dictionary<string, Material> materials)
    {
        var shell = (GameObject)PrefabUtility.InstantiatePrefab(source, parent);
        shell.name = "BlenderArchitecture";
        foreach (MeshRenderer renderer in shell.GetComponentsInChildren<MeshRenderer>())
        {
            string key = renderer.name.Substring(renderer.name.LastIndexOf('_') + 1).Split('.')[0];
            if (!materials.TryGetValue(key, out Material material))
                throw new InvalidOperationException("Unmapped architecture material: " + key);
            renderer.sharedMaterials = Enumerable.Repeat(material, renderer.sharedMaterials.Length).ToArray();
            renderer.receiveGI = ReceiveGI.Lightmaps;
            renderer.scaleInLightmap = 1;
            renderer.shadowCastingMode = ShadowCastingMode.TwoSided;
            GameObjectUtility.SetStaticEditorFlags(renderer.gameObject,
                StaticEditorFlags.ContributeGI | StaticEditorFlags.BatchingStatic |
                StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic |
                StaticEditorFlags.ReflectionProbeStatic);
            var collider = renderer.gameObject.AddComponent<MeshCollider>();
            collider.sharedMesh = renderer.GetComponent<MeshFilter>().sharedMesh;
        }
    }

    [MenuItem("Tools/Interrogation Room/Station Rebuild/5 Update door visuals")]
    public static void ReplaceDoorVisuals()
    {
        RequireEditMode();
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/DoorLeaf.fbx");
        if (model == null) throw new InvalidOperationException("Export the Blender door asset first.");
        var materials = CreateMaterials();
        foreach (NetworkDoor door in GameObject.Find(RootName).GetComponentsInChildren<NetworkDoor>())
        {
            Transform previous = door.transform.Find("AuthoredDoorLeaf");
            if(previous != null) Object.DestroyImmediate(previous.gameObject);
            foreach (var component in door.GetComponents<Component>())
                if(component is MeshRenderer || component is MeshFilter || component is BoxCollider)
                    Object.DestroyImmediate(component);
            door.transform.localScale = Vector3.one;
            var leaf = (GameObject)PrefabUtility.InstantiatePrefab(model,door.transform);
            leaf.name = "AuthoredDoorLeaf";
            foreach(Renderer renderer in leaf.GetComponentsInChildren<Renderer>())
                renderer.sharedMaterials = Enumerable.Repeat(materials[renderer.name.Substring(renderer.name.LastIndexOf('_')+1).Split('.')[0]],renderer.sharedMaterials.Length).ToArray();
            var collider=leaf.AddComponent<BoxCollider>();
            collider.size=new Vector3(1.48f,2.14f,.08f);
            var handle=new GameObject("InteractionPoint").transform;
            handle.SetParent(leaf.transform,false);
            // Keep the target inside the leaf so visibility rays hit it from either face.
            handle.localPosition=new Vector3(.5f,0,0);
            var so=new SerializedObject(door);
            so.FindProperty("doorLeaf").objectReferenceValue=leaf.transform;
            so.FindProperty("visualRoot").objectReferenceValue=leaf.transform;
            so.FindProperty("blockingCollider").objectReferenceValue=collider;
            so.FindProperty("interactionPoint").objectReferenceValue=handle;
            so.FindProperty("hingeLocalOffset").vector3Value=new Vector3(-.74f,0,0);
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    [MenuItem("Tools/Interrogation Room/Station Rebuild/11 Fix two-sided door interaction")]
    public static void FixDoorInteractionPoints()
    {
        RequireEditMode();
        var root = GameObject.Find(RootName);
        if (SceneManager.GetActiveScene().path != ScenePath || root == null)
            throw new InvalidOperationException("Open the rebuilt Room scene first.");
        foreach (var door in root.GetComponentsInChildren<NetworkDoor>())
        {
            var point = door.transform.Find("AuthoredDoorLeaf/InteractionPoint");
            if (point == null) throw new InvalidOperationException("Missing door target: " + door.name);
            Undo.RecordObject(point, "Fix two-sided door interaction");
            point.localPosition = new Vector3(.5f, 0, 0);
        }
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
    }

    private static Dictionary<string, Material> CreateMaterials()
    {
        var result = new Dictionary<string, Material>();
        AddMaterial(result, "Plaster", "Assets/Materials/P2_PlasterCream.mat", new Color(.83f,.82f,.77f), .12f);
        AddMaterial(result, "Sage", "Assets/Materials/P2_BottleGreen.mat", new Color(.43f,.49f,.45f), .24f);
        AddMaterial(result, "Ceiling", "Assets/Materials/P2_PlasterCream.mat", new Color(.9f,.89f,.85f), .08f);
        AddMaterial(result, "Stone", null, new Color(.31f,.33f,.32f), .22f);
        AddMaterial(result, "Oak", "Assets/Materials/P2_DarkWood.mat", new Color(.63f,.49f,.35f), .25f);
        AddMaterial(result, "Metal", null, new Color(.16f,.18f,.18f), .4f);
        AddMaterial(result, "Brass", null, new Color(.42f,.33f,.18f), .48f);
        AddMaterial(result, "Floor", "Assets/Materials/Posterunek/Instances/Mat_Floor_PodlogaSala.mat", new Color(.65f,.64f,.59f), .2f);
        AddMaterial(result, "Tile", "Assets/Materials/Posterunek/Instances/Mat_Floor_PodlogaKorytarz.mat", new Color(.67f,.69f,.65f), .25f);
        AddMaterial(result, "Glass", null, new Color(.54f,.64f,.7f), .55f);
        AddMaterial(result, "Diffuser", null, new Color(.93f,.93f,.88f), .25f);
        foreach (string key in new[] { "Glass", "Diffuser" })
        {
            Material m = result[key];
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", key == "Glass" ? new Color(.7f,.86f,1f)*1.2f : new Color(1f,.93f,.8f)*2f);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            EditorUtility.SetDirty(m);
        }
        return result;
    }

    private static void AddMaterial(Dictionary<string, Material> target, string name, string source, Color tint, float smoothness)
    {
        string path = Folder + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Material template = source == null ? null : AssetDatabase.LoadAssetAtPath<Material>(source);
            material = template != null ? new Material(template) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.name = name;
            AssetDatabase.CreateAsset(material, path);
        }
        material.SetColor("_BaseColor", tint);
        material.SetFloat("_Smoothness", smoothness);
        material.SetFloat("_BumpScale", .3f);
        material.mainTextureScale = Vector2.one;
        material.enableInstancing = true;
        target.Add(name, material);
        EditorUtility.SetDirty(material);
    }

    private static void CreateRooms(Transform parent, Layout layout)
    {
        Transform rooms = new GameObject("RoomVolumes").transform;
        rooms.SetParent(parent);
        foreach (Room definition in layout.rooms)
        {
            var room = new GameObject("Room_" + definition.key);
            room.transform.SetParent(rooms);
            room.transform.position = new Vector3(definition.x, 1.7f, definition.z);
            var collider = room.AddComponent<BoxCollider>();
            collider.size = new Vector3(definition.width, 3.4f, definition.depth);
            collider.isTrigger = true;
            var volume = room.AddComponent<RoomVolume>();
            var so = new SerializedObject(volume);
            so.FindProperty("roomId").stringValue = definition.id;
            so.FindProperty("volumeCollider").objectReferenceValue = collider;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void ConfigureDoors(Transform parent, Layout layout, Dictionary<string, Material> materials)
    {
        string fontPath = Folder + "/StationLabels.asset";
        if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath) == null)
            AssetDatabase.CopyAsset("Assets/Fonts/RoomLabels SDF.asset", fontPath);
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
        GameObject template = parent.Find("DrzwiSala").gameObject;
        foreach (Door definition in layout.doors)
        {
            Transform existing = parent.Find(definition.name);
            GameObject door = existing != null ? existing.gameObject : Object.Instantiate(template, parent);
            door.name = definition.name;
            door.transform.SetPositionAndRotation(new Vector3(definition.x, 1.1f, definition.z), Quaternion.Euler(0, definition.angle, 0));
            door.transform.localScale = new Vector3(1.48f, 2.14f, .065f);
            var component = door.GetComponent<NetworkDoor>();
            var so = new SerializedObject(component);
            so.FindProperty("roomAId").stringValue = definition.a;
            so.FindProperty("roomBId").stringValue = definition.b;
            so.ApplyModifiedPropertiesWithoutUndo();
            door.GetComponent<Renderer>().sharedMaterial = materials["Oak"];
            // Labels are attached to the building, not the animated door leaf.
            var sign = new GameObject("Sign_" + definition.name);
            sign.transform.SetParent(parent.parent);
            sign.transform.SetPositionAndRotation(new Vector3(definition.x, 2.53f, definition.z), Quaternion.Euler(0, definition.angle, 0));
            foreach (float side in new[] { -1f, 1f })
            {
                var textObject = new GameObject("Label");
                textObject.transform.SetParent(sign.transform, false);
                textObject.transform.localPosition = new Vector3(0, 0, side * .16f);
                textObject.transform.localRotation = Quaternion.Euler(0, side > 0 ? 180 : 0, 0);
                var text = textObject.AddComponent<TextMeshPro>();
                text.font = font;
                text.text = definition.label;
                text.fontSize = 1.15f;
                text.alignment = TextAlignmentOptions.Center;
                text.color = new Color(.09f,.13f,.13f);
                text.rectTransform.sizeDelta = new Vector2(3.5f,.28f);
                text.enableAutoSizing = false;
            }
        }
    }

    private static void MoveFurniture(Transform furniture)
    {
        foreach (Transform child in furniture)
        {
            Vector3 shift = Vector3.zero;
            if (child.name.StartsWith("Sala_")) shift = new Vector3(0,0,4.5f);
            if (child.name.StartsWith("Przesluchania_")) shift = new Vector3(-8.85f,0,-1.85f);
            if (child.name.StartsWith("Archiwum_")) shift = new Vector3(-10.825f,0,3.1f);
            if (child.name.StartsWith("Socjalny_")) shift = new Vector3(14.625f,0,3.1f);
            child.position += shift;
        }
        Place("Sala_Kosz", new Vector3(5.4f,.02f,6.7f));
        Place("Korytarz_LawkaW", new Vector3(-2.5f,.02f,3.65f));
        Place("Korytarz_LawkaE", new Vector3(.6f,.02f,3.65f));
        Place("Korytarz_Wieszak", new Vector3(5.45f,.02f,5.45f));
        Place("Korytarz_Wycieraczka", new Vector3(-.4f,.03f,-3.5f));
        Place("Korytarz_Roslina", new Vector3(-5.5f,.02f,5.5f));
        foreach (Transform child in furniture)
            if (child.name.StartsWith("Sala_Stol") && !child.name.StartsWith("Sala_Stolik") || child.name.StartsWith("Sala_Krzeslo"))
                child.position += new Vector3(0,0,-1.4f);
    }

    private static void MoveGameplay()
    {
        Transform actions = GameObject.Find("RoundPhysicalIntegration").transform;
        foreach (Transform child in actions)
        {
            if (child.name.StartsWith("B4") || child.name == "B5 Receipt Clue" || child.name.StartsWith("GameplayItem_"))
                child.position += child.name == "B4 Personal Locker" || child.name == "B4 Quiet Plant"
                    ? new Vector3(14.625f,0,3.1f) : new Vector3(-10.825f,0,3.1f);
        }
        Place("B5 Maintenance Cabinet",new Vector3(13.8f,.02f,2.3f));
        Place("B5 Service Panel",new Vector3(13.8f,.02f,7.3f));
        Place("B5 Vent Control",new Vector3(-5.45f,.02f,12.05f));
        Place("B5 Gate Control",new Vector3(5.45f,.02f,12.05f));
        Place("B5 Final Exit A",new Vector3(-5.4f,.02f,-12.4f));
        Place("B5 Final Exit B",new Vector3(5.4f,.02f,-12.4f));
        Place("WeaponPickup",new Vector3(1.62f,.54f,8.63f));
        Place("Spot_MugChoir",new Vector3(9.1f,.86f,-2f));
        Place("Spot_Typewriter",new Vector3(-12.205f,1.15f,-.35f));
        Place("Spot_PigeonInspector",new Vector3(-8.225f,1.4f,-2.7f));
        Place("Spot_IntercomForecast",new Vector3(5.65f,.8f,2.5f));
        Place("B5 Receipt Clue",new Vector3(-12.5f,.02f,-.35f));
        foreach (GameObject sceneRoot in SceneManager.GetActiveScene().GetRootGameObjects())
            if (sceneRoot.name == "GameplayItem_SuspiciousToken" && sceneRoot.GetComponent<NetworkIdentity>() == null)
                sceneRoot.SetActive(false);
        var ambience = GameObject.Find("PoliceStation_Ambience");
        if (ambience != null)
            foreach (AudioSource sound in ambience.GetComponentsInChildren<AudioSource>())
                sound.transform.position = new Vector3(Mathf.Clamp(sound.transform.position.x,-4,4), sound.transform.position.y, 4.6f);
    }

    private static void FurnishNewRooms(Transform parent)
    {
        // Reuse authored props, preserving their real mesh origins by moving bounds.
        CopyProp("Archiwum_Biurko", "Office_Desk", new Vector3(-13,.6f,11), parent);
        CopyProp("Archiwum_Krzeslo", "Office_Chair", new Vector3(-11.8f,.57f,11), parent);
        CopyProp("Archiwum_Monitor", "Office_Monitor", new Vector3(-13.3f,1.08f,11), parent);
        CopyProp("Sala_StolW", "Briefing_TableA", new Vector3(10,.39f,11), parent);
        CopyProp("Sala_StolE", "Briefing_TableB", new Vector3(11.5f,.39f,11), parent);
        CopyProp("Sala_KrzesloW1", "Briefing_ChairA", new Vector3(8.9f,.49f,11), parent);
        CopyProp("Sala_KrzesloE2", "Briefing_ChairB", new Vector3(12.6f,.49f,11), parent);
        CopyProp("Sala_Lawka", "Reception_Bench", new Vector3(3,.52f,-11.8f), parent);
        CopyProp("Sala_RoslinaW", "Reception_Plant", new Vector3(5.2f,.67f,-6.2f), parent);
        CopyProp("Archiwum_Biurko", "Workshop_Desk", new Vector3(8,.58f,6.5f), parent);
        CopyProp("Sala_StolikKawowy", "Office_CoffeeTable", new Vector3(-8,.3f,12.3f), parent);
        CopyProp("Sala_Sofa", "Office_Sofa", new Vector3(-8,.5f,12.2f), parent);
        CopyProp("Sala_RoslinaE", "Briefing_Plant", new Vector3(14,.67f,13), parent);
        CopyProp("Sala_Lawka", "Evidence_Bench", new Vector3(-8,.52f,2.2f), parent);
    }

    private static void CopyProp(string sourceName, string name, Vector3 center, Transform parent)
    {
        GameObject source = GameObject.Find(sourceName);
        if (source == null) throw new InvalidOperationException("Missing reusable prop: " + sourceName);
        GameObject copy = Object.Instantiate(source, parent);
        copy.name = name;
        Renderer[] renderers = copy.GetComponentsInChildren<Renderer>();
        Bounds bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
        copy.transform.position += center - bounds.center;
    }

    private static void ConfigureLighting(Transform parent, Layout layout)
    {
        Transform lighting = new GameObject("Lighting").transform;
        lighting.SetParent(parent);
        foreach (Fixture fixture in layout.lights)
        {
            Light light = NewLight(lighting,"Practical_"+fixture.name,new Vector3(fixture.x,3.18f,fixture.z));
            light.type = LightType.Spot;
            light.transform.rotation = Quaternion.Euler(90,0,0);
            light.spotAngle = 140;
            light.innerSpotAngle = 90;
            light.range = 9;
            light.intensity = fixture.central ? 25 : fixture.name.StartsWith("hall") ? 18 : 50;
            light.color = new Color(1f,.97f,.94f);
            light.lightmapBakeType = fixture.central ? LightmapBakeType.Mixed : LightmapBakeType.Baked;
            light.bounceIntensity = 1.4f;
        }
        foreach (Window window in layout.windows)
        {
            Vector3 inside = window.axis == "z" ? new Vector3(-Mathf.Sign(window.x),0,0) : new Vector3(0,0,-Mathf.Sign(window.z));
            Light light = NewLight(lighting,"Daylight",new Vector3(window.x,2.1f,window.z)+inside*.2f);
            light.type = LightType.Rectangle;
            light.areaSize = new Vector2(1.8f,1.4f);
            light.transform.rotation = Quaternion.LookRotation(inside);
            light.intensity = 8;
            light.range = 12;
            light.color = new Color(.83f,.91f,1);
            light.lightmapBakeType = LightmapBakeType.Baked;
        }
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(.24f,.28f,.32f);
        RenderSettings.ambientEquatorColor = new Color(.16f,.18f,.2f);
        RenderSettings.ambientGroundColor = new Color(.1f,.11f,.12f);
        RenderSettings.ambientIntensity = 1;
        RenderSettings.fog = false;
        var volume = GameObject.Find("PostFX_Global").GetComponent<Volume>();
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(profile,Folder+"/StationPostFX.asset");
        profile.Add<Tonemapping>(true).mode.value = TonemappingMode.ACES;
        var color = profile.Add<ColorAdjustments>(true);
        color.postExposure.value = .5f;
        color.contrast.value = 4;
        color.saturation.value = -3;
        var bloom = profile.Add<Bloom>(true);
        bloom.intensity.value = .12f;
        bloom.threshold.value = 1.2f;
        profile.Add<Vignette>(true).intensity.value = .12f;
        foreach (VolumeComponent component in profile.components) AssetDatabase.AddObjectToAsset(component,profile);
        volume.sharedProfile = profile;
        var settings = new LightingSettings
        {
            bakedGI = true, realtimeGI = false,
            lightmapper = LightingSettings.Lightmapper.ProgressiveCPU,
            lightmapResolution = 12, lightmapMaxSize = 2048,
            lightmapPadding = 4, indirectSampleCount = 128,
            directSampleCount = 32, environmentSampleCount = 64,
            maxBounces = 4, minBounces = 2, ao = true, aoMaxDistance = .5f,
            aoExponentIndirect = .6f,
            mixedBakeMode = MixedLightingMode.Shadowmask
        };
        AssetDatabase.CreateAsset(settings,Folder+"/StationLighting.lighting");
        Lightmapping.lightingSettings = settings;
        foreach (ProbeVolume probe in Object.FindObjectsByType<ProbeVolume>(FindObjectsSortMode.None))
        {
            probe.transform.position = new Vector3(0,1.7f,.5f);
            probe.size = new Vector3(32,4,29);
            EditorUtility.SetDirty(probe);
        }
        foreach (Room room in layout.rooms.Where(r=>r.id!="korytarz"))
        {
            var go = new GameObject("Reflection_"+room.key);
            go.transform.SetParent(lighting);
            go.transform.position = new Vector3(room.x,1.6f,room.z);
            var probe = go.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Baked;
            probe.size = new Vector3(room.width,3.4f,room.depth);
            probe.boxProjection = true;
            probe.resolution = 128;
            probe.blendDistance = .3f;
        }
    }

    private static Light NewLight(Transform parent, string name, Vector3 position)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = position;
        var light = go.AddComponent<Light>();
        light.shadows = LightShadows.Soft;
        light.shadowBias = .025f;
        light.shadowNormalBias = .15f;
        light.renderingLayerMask = -1;
        return light;
    }

    private static void Place(string name, Vector3 position)
    {
        GameObject go = GameObject.Find(name);
        if (go == null) throw new InvalidOperationException("Missing scene object: " + name);
        go.transform.position = position;
    }

    private static Layout ReadLayout() => JsonUtility.FromJson<Layout>(AssetDatabase.LoadAssetAtPath<TextAsset>(Folder+"/layout.json").text);
    private static void RequireEditMode()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Station authoring requires Edit Mode.");
    }
}
