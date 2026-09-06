using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

/// <summary>Repeatable, presentation-only finishing pass for the authored station.</summary>
public static class StationFinishPolish
{
    private const string Folder = "Assets/Art/Environment/StationRebuild/";
    private static Transform root;
    private static readonly string[] Models = { "FinishTelevision", "FinishCredenza", "FinishKeyboard", "FinishMonitor", "FinishToolTray", "FinishRecordBundle" };

    [MenuItem("Tools/Interrogation Room/Station Rebuild/20 Apply finish polish")]
    public static void Apply()
    {
        if (Application.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != "Assets/Scenes/Room.unity")
            throw new InvalidOperationException("Open Room in Edit Mode.");
        foreach (var name in Models)
        {
            AssetDatabase.ImportAsset(Folder + name + ".fbx");
            var importer = (ModelImporter)AssetImporter.GetAtPath(Folder + name + ".fbx");
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.generateSecondaryUV = true;
            importer.secondaryUVPackMargin = 8;
            importer.importAnimation = false;
            importer.SaveAndReimport();
        }
        Material("Plastic", new Color(.14f,.16f,.15f), .28f);
        Material("Rubber", new Color(.035f,.04f,.038f), .08f);
        Material("Glass", new Color(.045f,.075f,.073f), .78f);
        Material("Ivory", new Color(.60f,.59f,.51f), .27f);
        Material("Steel", new Color(.29f,.32f,.31f), .42f);
        AssetDatabase.LoadAssetAtPath<Material>(Folder + "Finish_Steel.mat").SetFloat("_Metallic", .8f);
        var station = GameObject.Find("Map_Station").transform;
        var old = station.Find("FinishPolish");
        if (old != null) Object.DestroyImmediate(old.gameObject);
        root = new GameObject("FinishPolish").transform;
        root.SetParent(station, false);

        // Preserve all existing interaction roots and colliders. Replace presentation only.
        foreach (string name in new[] { "Sala_RoslinaW", "Sala_RoslinaE", "Briefing_Plant", "Reception_Plant", "Korytarz_Roslina" })
        {
            var target = station.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);
            if (target == null) continue;
            var renderer = target.GetComponent<Renderer>();
            if (renderer == null) continue;
            var b = renderer.bounds;
            foreach (var legacy in target.GetComponentsInChildren<Renderer>(true)) legacy.enabled = false;
            Scanned("potted_plant_04", name + "_Natural", new Vector3(b.center.x,.005f,b.center.z), .72f, name.Length * 17);
        }
        HideVisual("Sala_TV");
        HideVisual("Sala_SzafkaTV");
        Add("FinishCredenza", "CommonCredenza", new Vector3(-5.66f,.005f,8.55f), 270);
        Add("FinishTelevision", "CommonTelevision", new Vector3(-5.66f,.62f,8.55f), 270);
        HideVisual("OfficeDisplay");
        Add("FinishMonitor", "OfficeMonitor", new Vector3(-12.10f,.77f,12.98f));
        Add("FinishKeyboard", "OfficeKeyboard", new Vector3(-12.08f,.77f,12.56f));
        Add("FinishToolTray", "WorkshopSocketTray", new Vector3(13.26f,.961f,7.47f));
        Add("FinishRecordBundle", "OfficeRecords", new Vector3(-12.56f,.77f,12.53f), 8);
        Add("FinishRecordBundle", "CommonReading", new Vector3(-3.8f,.581f,8.02f), 5);
        Scanned("binder_notebook", "OfficeNotebook", new Vector3(-12.55f,.77f,12.96f), .025f, -8);
        Scanned("metal_toolbox", "WorkshopSpareTools", new Vector3(13.83f,.961f,7.44f), .19f, 12);

        // Dressing remains outside walking lanes and cannot obscure task interaction points.
        int rackIndex = 0;
        foreach (var rack in station.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("StorageWest_") || t.name.StartsWith("StorageEast_") || t.name.StartsWith("ArchiveNorth")))
        {
            var bundle = Add("FinishRecordBundle", "RackRecords_" + rackIndex, rack.TransformPoint(new Vector3(.32f,2.105f,-.035f)), rack.eulerAngles.y + 4);
            bundle.transform.localScale = new Vector3(1,.8f,1);
            if (rackIndex++ % 2 == 0)
                Scanned("cardboard_box_01", "LabelledCarton_" + rackIndex, rack.TransformPoint(new Vector3(-.4f,2.102f,0)), .24f, rack.eulerAngles.y);
        }
        foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (light.name.StartsWith("Practical_commonLounge")) light.intensity = 38;
            if (light.name.StartsWith("Practical_hall"))
            {
                light.intensity = 21;
                light.innerSpotAngle = 70;
                light.spotAngle = 150;
            }
        }
        var voice = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(m => m != null && m.GetType().Name == "VivoxVoiceRuntime");
        if (voice != null)
        {
            var serialized = new SerializedObject(voice);
            serialized.FindProperty("micMutedColor").colorValue = new Color(.68f,.32f,.25f,.85f);
            serialized.FindProperty("micNormalColor").colorValue = new Color(.84f,.81f,.70f,.8f);
            serialized.FindProperty("micSpeakingColor").colorValue = new Color(.48f,.68f,.52f,.95f);
            var icon = serialized.FindProperty("micIcon").objectReferenceValue as UnityEngine.UI.Image;
            if (icon != null) icon.rectTransform.sizeDelta = new Vector2(220,220);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        // Use the scanned carpet when available; the initial fallback is textured and matte.
        var matObject = GameObject.Find("Korytarz_Wycieraczka");
        if (matObject != null)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(Folder + "FinishMat.mat");
            if (material == null)
            {
                material = new Material(AssetDatabase.LoadAssetAtPath<Material>(Folder + "Scanned/concrete_floor_worn_001/Surface.mat"));
                AssetDatabase.CreateAsset(material, Folder + "FinishMat.mat");
            }
            material.SetColor("_BaseColor", new Color(.23f,.20f,.16f));
            material.SetFloat("_Smoothness", .08f);
            material.SetFloat("_BumpScale", .35f);
            material.mainTextureScale = new Vector2(4,4);
            matObject.GetComponent<Renderer>().sharedMaterial = material;
            EditorUtility.SetDirty(material);
        }
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "Scanned/dirty_carpet/albedo.jpg") != null) ApplyCarpet();
        Physics.SyncTransforms();
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("[StationFinish] Authored six detail models and replaced legacy plant/TV presentation.");
    }

    private static void Material(string key, Color tint, float smoothness)
    {
        string path = Folder + "Finish_" + key + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m,path); }
        m.SetColor("_BaseColor",tint); m.SetFloat("_Smoothness",smoothness);
        m.enableInstancing = true;
        EditorUtility.SetDirty(m);
    }

    private static void HideVisual(string name)
    {
        var t = GameObject.Find("Map_Station").GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name == name);
        if (t == null) return;
        foreach (var r in t.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
    }

    private static GameObject Add(string model, string name, Vector3 position, float yaw = 0)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Folder + model + ".fbx"),root);
        go.name = name;
        go.transform.SetPositionAndRotation(position, Quaternion.Euler(0,yaw,0));
        foreach (var r in go.GetComponentsInChildren<MeshRenderer>())
        {
            string key = r.name.Substring(r.name.LastIndexOf('_')+1).Split('.')[0];
            r.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(Folder + "Finish_" + key + ".mat") ??
                AssetDatabase.LoadAssetAtPath<Material>(Folder + "Detail_" + key + ".mat");
            if (r.sharedMaterial == null) throw new InvalidOperationException("Missing finish material " + key);
            Static(r);
        }
        return go;
    }

    private static void Scanned(string id, string name, Vector3 bottom, float height, float yaw)
    {
        string path = Folder + "Scanned/" + id + "/";
        var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path + id + "_static.fbx"), root);
        go.name = name;
        go.transform.rotation = Quaternion.Euler(0,yaw,0);
        var renderers = go.GetComponentsInChildren<MeshRenderer>();
        var b = renderers[0].bounds;
        foreach (var r in renderers) b.Encapsulate(r.bounds);
        go.transform.localScale *= height / b.size.y;
        b = renderers[0].bounds;
        foreach (var r in renderers) b.Encapsulate(r.bounds);
        go.transform.position += bottom - new Vector3(b.center.x,b.min.y,b.center.z);
        foreach (var r in renderers)
        {
            r.sharedMaterials = Enumerable.Repeat(AssetDatabase.LoadAssetAtPath<Material>(path + "Surface.mat"),r.sharedMaterials.Length).ToArray();
            Static(r);
        }
    }

[MenuItem("Tools/Interrogation Room/Station Rebuild/21 Apply scanned carpet")]
    public static void ApplyCarpet()
    {
        if (Application.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != "Assets/Scenes/Room.unity")
            throw new InvalidOperationException("Open Room in Edit Mode.");
        const string path = Folder + "Scanned/dirty_carpet/";
        foreach (string file in new[] { "albedo.jpg", "normal.png", "arm.png" })
        {
            AssetDatabase.ImportAsset(path + file);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path + file);
            importer.textureType = file == "normal.png" ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = file == "albedo.jpg";
            importer.maxTextureSize = 2048;
            importer.mipmapEnabled = true;
            importer.anisoLevel = 16;
            importer.isReadable = file == "arm.png";
            importer.textureCompression = file == "arm.png" ? TextureImporterCompression.Uncompressed : TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }
        var arm = AssetDatabase.LoadAssetAtPath<Texture2D>(path + "arm.png");
        var pixels = arm.GetPixels32();
        for (int i = 0; i < pixels.Length; i++)
        {
            var p = pixels[i];
            pixels[i] = new Color32(p.b, p.r, 0, (byte)(255-p.g));
        }
        var mask = new Texture2D(arm.width,arm.height,TextureFormat.RGBA32,false,true);
        mask.SetPixels32(pixels); mask.Apply();
        System.IO.File.WriteAllBytes(path+"UnityMask.png",mask.EncodeToPNG());
        Object.DestroyImmediate(mask);
        AssetDatabase.ImportAsset(path+"UnityMask.png");
        var maskImporter = (TextureImporter)AssetImporter.GetAtPath(path+"UnityMask.png");
        maskImporter.sRGBTexture = false; maskImporter.maxTextureSize = 2048;
        maskImporter.textureCompression = TextureImporterCompression.CompressedHQ;
        maskImporter.SaveAndReimport();
        var armImporter = (TextureImporter)AssetImporter.GetAtPath(path+"arm.png");
        armImporter.isReadable=false; armImporter.textureCompression=TextureImporterCompression.CompressedHQ;
        armImporter.SaveAndReimport();
        foreach (string name in new[] { "Sala_Dywan", "Sala_DywanE", "Korytarz_Wycieraczka" })
        {
            bool small = name == "Korytarz_Wycieraczka";
            string asset = Folder + (small ? "FinishMat.mat" : "FinishCarpet.mat");
            var m = AssetDatabase.LoadAssetAtPath<Material>(asset);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m,asset); }
            m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(path+"albedo.jpg"));
            m.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(path+"normal.png"));
            m.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(path+"UnityMask.png"));
            m.SetTexture("_OcclusionMap",AssetDatabase.LoadAssetAtPath<Texture2D>(path+"UnityMask.png"));
            m.EnableKeyword("_NORMALMAP"); m.EnableKeyword("_METALLICSPECGLOSSMAP"); m.EnableKeyword("_OCCLUSIONMAP");
            m.SetColor("_BaseColor", small ? new Color(.45f,.43f,.39f) : new Color(.85f,.85f,.80f));
            m.SetFloat("_Smoothness",.7f); m.SetFloat("_BumpScale",.45f); m.SetFloat("_OcclusionStrength",.6f);
            m.mainTextureScale=small ? new Vector2(1.4f,.8f) : new Vector2(4,3);
            m.enableInstancing=true;
            foreach(var r in GameObject.Find(name).GetComponentsInChildren<MeshRenderer>())r.sharedMaterial=m;
            EditorUtility.SetDirty(m);
        }
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
    }

    private static void Static(MeshRenderer renderer)
    {
        renderer.receiveGI = ReceiveGI.Lightmaps;
        GameObjectUtility.SetStaticEditorFlags(renderer.gameObject, StaticEditorFlags.ContributeGI | StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.ReflectionProbeStatic);
    }
}
