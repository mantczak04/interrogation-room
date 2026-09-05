using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>Explicit scene authoring for scanned surfaces and props; never runs during gameplay.</summary>
public static class StationRealismSetup
{
    private const string Folder = "Assets/Art/Environment/StationRebuild/";
    private const string Scanned = Folder + "Scanned/";
    private static readonly string[] Textures = { "painted_plaster_wall", "terrazzo_tiles", "concrete_floor_worn_001", "oak_wood_planks", "rusty_painted_metal", "grey_plaster_03" };
    private static readonly string[] Models = { "drawer_cabinet", "power_box_01", "potted_plant_04", "binder_notebook", "metal_toolbox", "cardboard_box_01", "fire_alarm", "desk_lamp_arm_01" };

    [MenuItem("Tools/Interrogation Room/Station Rebuild/12 Prepare scanned materials")]
    public static void PrepareMaterials()
    {
        RequireRoom();
        foreach (string id in Textures.Concat(Models))
        {
            ConfigureTexture(TexturePath(id, "albedo"), true, false, false);
            ConfigureTexture(TexturePath(id, "normal"), false, true, false);
            string armPath = TexturePath(id, "arm");
            ConfigureTexture(armPath, false, false, true);
            Texture2D arm = AssetDatabase.LoadAssetAtPath<Texture2D>(armPath);
            Color32[] pixels = arm.GetPixels32();
            for (int i = 0; i < pixels.Length; i++)
            {
                Color32 p = pixels[i];
                // Poly Haven ARM -> Unity metallic R, occlusion G, smoothness A.
                pixels[i] = new Color32(p.b, p.r, 0, (byte)(255 - p.g));
            }
            var packed = new Texture2D(arm.width, arm.height, TextureFormat.RGBA32, false, true);
            packed.SetPixels32(pixels);
            packed.Apply();
            string packedPath = Scanned + id + "/UnityMask.png";
            File.WriteAllBytes(packedPath, packed.EncodeToPNG());
            Object.DestroyImmediate(packed);
            AssetDatabase.ImportAsset(packedPath);
            ConfigureTexture(packedPath, false, false, false);
            ConfigureTexture(armPath, false, false, false);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(Scanned + id + "/Surface.mat");
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, Scanned + id + "/Surface.mat");
            }
            SetMaps(material, id, Color.white, 1);
            if (!Models.Contains(id)) continue;
            var importer = (ModelImporter)AssetImporter.GetAtPath(Scanned + id + "/" + id + "_static.fbx");
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.generateSecondaryUV = true;
            importer.secondaryUVPackMargin = 8;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importAnimation = false;
            importer.isReadable = true;
            importer.SaveAndReimport();
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[StationRealism] Prepared 6 scanned surfaces and 8 static textured props.");
    }

    private static string TexturePath(string id, string channel)
    {
        foreach (string ext in new[] { ".jpg", ".png", ".exr" })
        {
            string path = Scanned + id + "/" + channel + ext;
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(path) != null) return path;
        }
        throw new InvalidOperationException("Missing downloaded texture: " + id + "/" + channel);
    }

    private static void ConfigureTexture(string path, bool srgb, bool normal, bool readable)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
        importer.sRGBTexture = srgb;
        importer.isReadable = readable;
        importer.maxTextureSize = 2048;
        importer.mipmapEnabled = true;
        importer.anisoLevel = 16;
        importer.textureCompression = readable ? TextureImporterCompression.Uncompressed : TextureImporterCompression.CompressedHQ;
        importer.SaveAndReimport();
    }

    private static void SetMaps(Material material, string id, Color tint, float tiling)
    {
        material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath(id, "albedo")));
        material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath(id, "normal")));
        var mask = AssetDatabase.LoadAssetAtPath<Texture2D>(Scanned + id + "/UnityMask.png");
        material.SetTexture("_MetallicGlossMap", mask);
        material.SetTexture("_OcclusionMap", mask);
        material.EnableKeyword("_NORMALMAP");
        material.EnableKeyword("_METALLICSPECGLOSSMAP");
        material.EnableKeyword("_OCCLUSIONMAP");
        material.SetColor("_BaseColor", tint);
        material.SetFloat("_BumpScale", .65f);
        material.SetFloat("_Smoothness", 1);
        material.SetFloat("_OcclusionStrength", .7f);
        material.mainTextureScale = Vector2.one * tiling;
        material.enableInstancing = true;
        EditorUtility.SetDirty(material);
    }

    [MenuItem("Tools/Interrogation Room/Station Rebuild/13 Apply scanned environment")]
    public static void ApplyEnvironment()
    {
        Transform station = RequireRoom();
        Surface("Plaster", "painted_plaster_wall", new Color(.94f,.95f,.92f), .5f);
        Surface("Sage", "painted_plaster_wall", new Color(.42f,.49f,.45f), .5f);
        Surface("Ceiling", "grey_plaster_03", new Color(.90f,.91f,.89f), .5f);
        Surface("Floor", "concrete_floor_worn_001", new Color(.75f,.77f,.73f), .5f);
        Surface("Tile", "terrazzo_tiles", new Color(.84f,.86f,.82f), .5f);
        Surface("Oak", "oak_wood_planks", new Color(.58f,.47f,.34f), .65f);
        Surface("Stone", "concrete_floor_worn_001", new Color(.36f,.38f,.36f), 1);
        Surface("Metal", "rusty_painted_metal", new Color(.45f,.49f,.48f), .6f);
        Surface("Detail_Timber", "oak_wood_planks", new Color(.66f,.53f,.38f), 1);
        Surface("Detail_Steel", "rusty_painted_metal", new Color(.54f,.58f,.56f), 1);
        Surface("Detail_Paint", "painted_plaster_wall", new Color(.40f,.48f,.43f), 1);

        // Keep the original gameplay objects and collision envelopes. Fit only their presentation.
        foreach (Transform target in station.GetComponentsInChildren<Transform>(true)
            .Where(t => t.name.StartsWith("OfficeCabinet") || t.name == "SocialLocker" || t.name == "WorkshopDrawers").ToArray())
            Replace(target, "drawer_cabinet");
        var integration = GameObject.Find("RoundPhysicalIntegration").transform;
        foreach (Transform target in integration.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Authored_FileCabinet").ToArray())
            Replace(target, "drawer_cabinet");
        foreach (Transform target in integration.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Authored_UtilityPanel").ToArray())
            Replace(target, "power_box_01");
        var plant = GameObject.Find("B4 Quiet Plant").transform.Find("VisualRoot");
        Replace(plant, "potted_plant_04");
        ReplaceCarryable("GameplayItem_PersonalDocument", "binder_notebook", new Vector3(.24f,.036f,.29f));
        ReplaceCarryable("GameplayItem_SuspiciousToken", "metal_toolbox", new Vector3(.28f,.20f,.22f));
        ReplaceStateMarkers(integration);

        Transform previous = station.Find("ScannedDetails");
        if (previous != null) Object.DestroyImmediate(previous.gameObject);
        var details = new GameObject("ScannedDetails").transform;
        details.SetParent(station, false);
        AddProp(details, "desk_lamp_arm_01", "OfficeTaskLamp", new Vector3(-13.6f,1.02f,12.5f), 160, .56f);
        AddProp(details, "desk_lamp_arm_01", "ReceptionTaskLamp", new Vector3(1.2f,1.06f,-9.5f), 0, .50f);
        AddProp(details, "binder_notebook", "ReceptionLedger", new Vector3(2.1f,1.06f,-9.5f), 15, .032f);
        AddProp(details, "binder_notebook", "InterviewCaseFile", new Vector3(.28f,.787f,1.24f), 25, .028f);
        AddProp(details, "potted_plant_04", "ReceptionPlant", new Vector3(3.1f,1.06f,-9.5f), 25, .29f);
        AddProp(details, "metal_toolbox", "WorkshopTools", new Vector3(7.15f,1.02f,6.6f), 90, .27f);
        foreach (var p in new[] { new Vector3(-4.9f,.01f,-11.9f), new Vector3(-4.3f,.01f,-11.7f), new Vector3(-4.9f,.40f,-11.9f) })
            AddProp(details, "cardboard_box_01", "StorageCarton", p, p.x * 7, .38f, true);
        AddProp(details, "cardboard_box_01", "EvidenceCarton", new Vector3(-13.6f,.01f,6.8f), 15, .42f, true);
        foreach (var p in new[] { new Vector3(-3.28f,1.45f,-.9f),new Vector3(3.28f,1.45f,-.9f) })
            AddProp(details, "fire_alarm", "FireAlarm", p, p.x < 0 ? 90 : 270, .17f);
        Physics.SyncTransforms();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
    }

    private static void Surface(string name, string id, Color tint, float tiling)
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(Folder + name + ".mat");
        if (material == null) throw new InvalidOperationException("Missing station material " + name);
        SetMaps(material, id, tint, tiling);
    }

    private static Bounds BoundsOf(GameObject go)
    {
        var renderers = go.GetComponentsInChildren<Renderer>(true).Where(r => r is MeshRenderer && r.GetComponent<MeshFilter>().sharedMesh != null).ToArray();
        if (renderers.Length == 0) throw new InvalidOperationException("No mesh bounds: " + go.name);
        Bounds bounds = renderers[0].bounds;
        foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
        return bounds;
    }

    private static GameObject Instance(string id, Transform parent)
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(Scanned + id + "/" + id + "_static.fbx");
        if (model == null) throw new InvalidOperationException("Prepare static Blender model " + id);
        var go = (GameObject)PrefabUtility.InstantiatePrefab(model, parent);
        go.name = "ScannedVisual";
        var material = AssetDatabase.LoadAssetAtPath<Material>(Scanned + id + "/Surface.mat");
        foreach (var renderer in go.GetComponentsInChildren<MeshRenderer>())
        {
            renderer.sharedMaterials = Enumerable.Repeat(material, renderer.sharedMaterials.Length).ToArray();
            renderer.receiveGI = ReceiveGI.Lightmaps;
        }
        return go;
    }

    private static void Replace(Transform target, string id)
    {
        var old = target.Find("ScannedVisual");
        if (old != null) Object.DestroyImmediate(old.gameObject);
        Bounds desired = BoundsOf(target.gameObject);
        foreach (var renderer in target.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
        var go = Instance(id, target);
        // The scanned cabinet's drawer face points opposite to the authored cabinet.
        if (id == "drawer_cabinet") go.transform.localRotation *= Quaternion.Euler(0, 180, 0);
        Bounds actual = BoundsOf(go);
        float scale = id == "power_box_01" ? desired.size.y / actual.size.y
            : Mathf.Min(desired.size.x / actual.size.x, desired.size.y / actual.size.y, desired.size.z / actual.size.z);
        go.transform.localScale *= scale;
        actual = BoundsOf(go);
        go.transform.position += new Vector3(desired.center.x, desired.min.y + actual.extents.y, desired.center.z) - actual.center;
        SetStatic(go, true);
        if (id == "power_box_01" || id == "drawer_cabinet")
        {
            var identity = target.GetComponentInParent<Mirror.NetworkIdentity>();
            Transform action = identity != null ? identity.transform : target;
            var collider = action.GetComponent<BoxCollider>();
            if (collider == null) collider = action.gameObject.AddComponent<BoxCollider>();
            foreach (var oldCollider in target.GetComponentsInChildren<Collider>(true))
                if (oldCollider != collider) oldCollider.enabled = false;
            Bounds local = LocalBounds(action, go);
            collider.center = local.center;
            collider.size = local.size;
            if (id == "drawer_cabinet") FitCabinetCollider(collider, go);
            var point = action.Find("InteractionPoint");
            if (point != null) point.localPosition = local.center;
        }
    }

    private static void ReplaceStateMarkers(Transform integration)
    {
        foreach (var renderer in integration.GetComponentsInChildren<MeshRenderer>(true)
            .Where(r => r.name.StartsWith("PLACEHOLDER")).ToArray())
        {
            Transform target = renderer.transform;
            renderer.enabled = false;
            // Inactive legacy presentation roots are never used by runtime state hooks.
            if (target.parent.name == "VisualRoot") continue;
            var old = target.Find("ScannedVisual");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            old = target.Find("StateIndicator");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            Transform action = target.parent;
            bool indicator = action.name.Contains("Control") || action.name.Contains("Panel") || action.name.Contains("Exit") || action.name.Contains("Alarm");
            if (indicator)
            {
                var source = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "StatusIndicator.fbx");
                var go = (GameObject)PrefabUtility.InstantiatePrefab(source, target);
                go.name = "StateIndicator";
                Vector3 inherited = target.lossyScale;
                go.transform.localScale = new Vector3(1 / inherited.x, 1 / inherited.y, 1 / inherited.z);
                bool attempt = target.name.Contains("ActivePerformer");
                var tint = attempt ? new Color(1f,.34f,.035f) : new Color(.14f,.8f,.32f);
                string materialPath = Folder + (attempt ? "StateAmber.mat" : "StateGreen.mat");
                var lens = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (lens == null) { lens = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(lens, materialPath); }
                lens.SetColor("_BaseColor", tint);
                lens.EnableKeyword("_EMISSION"); lens.SetColor("_EmissionColor", tint * 2);
                lens.SetFloat("_Smoothness", .7f);
                EditorUtility.SetDirty(lens);
                foreach (var r in go.GetComponentsInChildren<MeshRenderer>())
                {
                    r.sharedMaterial = r.name.Contains("Lens") ? lens : AssetDatabase.LoadAssetAtPath<Material>(Folder + "Metal.mat");
                    r.receiveGI = ReceiveGI.LightProbes;
                    r.shadowCastingMode = ShadowCastingMode.Off;
                }
                Vector3 position = action.name.Contains("Exit")
                    ? action.TransformPoint(new Vector3(attempt ? -.17f : .17f, action.name.EndsWith("A") ? .37f : 1.65f, -.15f))
                    : action.name.Contains("Alarm") ? action.position + new Vector3(0,.95f,-.13f)
                    : action.TransformPoint(new Vector3(.15f,.18f,-.29f));
                go.transform.SetPositionAndRotation(position, action.rotation);
                SetStatic(go, false);
            }
            else
            {
                var go = Instance("binder_notebook", target);
                Vector3 inherited = target.lossyScale;
                go.transform.localScale = new Vector3(1 / inherited.x, 1 / inherited.y, 1 / inherited.z);
                Bounds bounds = BoundsOf(go);
                go.transform.localScale *= .025f / bounds.size.y;
                float top = action.name.Contains("Receipt") ? .93f : action.name.Contains("Suspicious") ? .63f
                    : action.name.Contains("Quiet Plant") ? .12f : action.name.Contains("Archive Slot") ? .65f : 1.322f;
                var position = action.position; position.y = top;
                go.transform.position += position - BoundsOf(go).center;
                go.transform.rotation = action.rotation * Quaternion.Euler(0,18,0);
                foreach (var r in go.GetComponentsInChildren<MeshRenderer>()) r.receiveGI = ReceiveGI.LightProbes;
                SetStatic(go, false);
            }
        }
    }

    private static void ReplaceCarryable(string name, string id, Vector3 size)
    {
        var target = GameObject.Find(name);
        var old = target.transform.Find("ScannedVisual");
        if (old != null) Object.DestroyImmediate(old.gameObject);
        var renderer = target.GetComponent<Renderer>();
        Bounds original = renderer.bounds;
        renderer.enabled = false;
        var go = Instance(id, target.transform);
        var scale = target.transform.lossyScale;
        go.transform.localScale = new Vector3(1 / scale.x, 1 / scale.y, 1 / scale.z);
        Bounds actual = BoundsOf(go);
        go.transform.localScale *= Mathf.Min(size.x / actual.size.x, size.y / actual.size.y, size.z / actual.size.z);
        actual = BoundsOf(go);
        float surfaceHeight = name == "GameplayItem_PersonalDocument" ? 1.322f : .575f;
        go.transform.position += new Vector3(original.center.x, surfaceHeight + actual.extents.y, original.center.z) - actual.center;
        foreach (var r in go.GetComponentsInChildren<MeshRenderer>()) r.receiveGI = ReceiveGI.LightProbes;
        SetStatic(go, false);
        Bounds local = LocalBounds(target.transform, go);
        var collider = target.GetComponent<BoxCollider>();
        collider.center = local.center;
        collider.size = Vector3.Max(local.size, Vector3.one * .05f);
        Transform point = target.transform.Find("ScannedInteractionPoint");
        if (point == null) { point = new GameObject("ScannedInteractionPoint").transform; point.SetParent(target.transform, false); }
        point.localPosition = local.center;
        var item = new SerializedObject(target.GetComponent<InterrogationRoom.Gameplay.Items.NetworkCarryableItem>());
        item.FindProperty("interactionPoint").objectReferenceValue = point;
        item.ApplyModifiedPropertiesWithoutUndo();
    }

    [MenuItem("Tools/Interrogation Room/Station Rebuild/15 Correct scanned cabinet facing")]
    public static void CorrectCabinetFacing()
    {
        Transform station = RequireRoom();
        var integration = GameObject.Find("RoundPhysicalIntegration").transform;
        var targets = station.GetComponentsInChildren<Transform>(true)
            .Where(t => t.name.StartsWith("OfficeCabinet") || t.name == "SocialLocker" || t.name == "WorkshopDrawers")
            .Concat(integration.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Authored_FileCabinet"));
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(Scanned + "drawer_cabinet/drawer_cabinet_static.fbx");
        foreach (var target in targets)
        {
            var visual = target.Find("ScannedVisual");
            if (visual == null) continue;
            visual.localRotation = source.transform.localRotation * Quaternion.Euler(0, 180, 0);
            var identity = target.GetComponentInParent<Mirror.NetworkIdentity>();
            var collider = (identity != null ? identity.transform : target).GetComponent<BoxCollider>();
            if (collider != null) FitCabinetCollider(collider, visual.gameObject);
        }
        ReplaceCarryable("GameplayItem_PersonalDocument", "binder_notebook", new Vector3(.24f,.036f,.29f));
        ReplaceStateMarkers(integration);
        Physics.SyncTransforms();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
    }

    private static void FitCabinetCollider(BoxCollider collider, GameObject visual)
    {
        Bounds bounds = LocalBounds(collider.transform, visual);
        // Stop at the upper shelf; the open frame above must not hide shelf items from interaction rays.
        float height = bounds.size.y * (1.75545f / 1.881f);
        collider.center = new Vector3(bounds.center.x, bounds.min.y + height * .5f, bounds.center.z);
        collider.size = new Vector3(bounds.size.x, height, bounds.size.z);
    }

    private static GameObject AddProp(Transform parent, string id, string name, Vector3 position, float yaw, float height, bool collision = false)
    {
        var go = Instance(id, parent);
        go.name = name;
        go.transform.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
        Bounds bounds = BoundsOf(go);
        go.transform.localScale *= height / bounds.size.y;
        bounds = BoundsOf(go);
        go.transform.position += new Vector3(position.x, position.y + bounds.extents.y, position.z) - bounds.center;
        if (collision)
        {
            var collider = go.AddComponent<BoxCollider>();
            Bounds local = LocalBounds(go.transform, go);
            collider.center = local.center;
            collider.size = local.size;
        }
        SetStatic(go, true);
        return go;
    }

    private static Bounds LocalBounds(Transform root, GameObject model)
    {
        var points = model.GetComponentsInChildren<MeshFilter>().SelectMany(f =>
        {
            Bounds b = f.sharedMesh.bounds;
            return new[] { new Vector3(-1,-1,-1),new Vector3(-1,-1,1),new Vector3(-1,1,-1),new Vector3(-1,1,1),new Vector3(1,-1,-1),new Vector3(1,-1,1),new Vector3(1,1,-1),new Vector3(1,1,1) }
                .Select(sign => root.InverseTransformPoint(f.transform.TransformPoint(b.center + Vector3.Scale(b.extents, sign))));
        }).ToArray();
        var result = new Bounds(points[0], Vector3.zero);
        foreach (var p in points) result.Encapsulate(p);
        return result;
    }

    private static void SetStatic(GameObject go, bool value)
    {
        foreach (var renderer in go.GetComponentsInChildren<MeshRenderer>())
            GameObjectUtility.SetStaticEditorFlags(renderer.gameObject, value
                ? StaticEditorFlags.ContributeGI | StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.ReflectionProbeStatic
                : 0);
    }

    [MenuItem("Tools/Interrogation Room/Station Rebuild/14 Light realistic surfaces")]
    public static void ConfigureLighting()
    {
        Transform station = RequireRoom();
        foreach (var light in station.GetComponentsInChildren<Light>())
        {
            if (light.name.StartsWith("Practical_"))
            {
                light.color = new Color(1f,.96f,.88f);
                light.intensity = light.name.Contains("interrogation") ? 16 : light.name.Contains("hall") ? 25 : 42;
                light.bounceIntensity = 1.6f;
                light.shadowBias = .025f;
                light.shadowNormalBias = .2f;
            }
            if (light.name == "Daylight")
            {
                light.intensity = 18;
                light.color = new Color(.80f,.89f,1f);
                light.bounceIntensity = 1.8f;
            }
        }
        var glass = AssetDatabase.LoadAssetAtPath<Material>(Folder + "Glass.mat");
        glass.SetColor("_BaseColor", new Color(.65f,.72f,.77f));
        glass.SetColor("_EmissionColor", new Color(.95f,1.12f,1.35f));
        EditorUtility.SetDirty(glass);
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(Folder + "StationPostFX.asset");
        if (profile.TryGet<ColorAdjustments>(out var color))
        {
            color.postExposure.Override(.15f);
            color.contrast.Override(9);
            color.saturation.Override(-8);
        }
        if (profile.TryGet<Bloom>(out var bloom)) { bloom.intensity.Override(.10f); bloom.threshold.Override(1.15f); }
        if (profile.TryGet<Vignette>(out var vignette)) { vignette.intensity.Override(.16f); vignette.smoothness.Override(.4f); }
        var grain = profile.TryGet<FilmGrain>(out var existingGrain) ? existingGrain : profile.Add<FilmGrain>();
        grain.type.Override(FilmGrainLookup.Thin1); grain.intensity.Override(.08f); grain.response.Override(.8f);
        var distortion = profile.TryGet<LensDistortion>(out var existingDistortion) ? existingDistortion : profile.Add<LensDistortion>();
        distortion.intensity.Override(-.12f); distortion.scale.Override(1.015f);
        EditorUtility.SetDirty(profile);
        var lighting = Lightmapping.lightingSettings;
        lighting.lightmapResolution = 28;
        lighting.directSampleCount = 128;
        lighting.indirectSampleCount = 512;
        lighting.maxBounces = 6;
        lighting.aoMaxDistance = .25f;
        EditorUtility.SetDirty(lighting);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
    }

    private static Transform RequireRoom()
    {
        if (Application.isPlaying || SceneManager.GetActiveScene().path != "Assets/Scenes/Room.unity")
            throw new InvalidOperationException("Open Room in Edit Mode.");
        var root = GameObject.Find("Map_Station");
        if (root == null) throw new InvalidOperationException("Compose the station first.");
        return root.transform;
    }
}
