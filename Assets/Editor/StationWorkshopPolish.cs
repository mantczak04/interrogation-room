using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

/// <summary>Workshop furniture presentation; preserves task roots and collision envelopes.</summary>
public static class StationWorkshopPolish
{
    private const string Folder = "Assets/Art/Environment/StationRebuild/";

    [MenuItem("Tools/Interrogation Room/Station Rebuild/23 Polish workshop")]
    public static void Apply()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (Application.isPlaying || scene.path != "Assets/Scenes/Room.unity")
            throw new InvalidOperationException("Open Room in Edit Mode.");
        foreach (var name in new[] { "WorkshopDetailBench", "WorkshopSupplyRackA", "WorkshopSupplyRackB" })
        {
            AssetDatabase.ImportAsset(Folder + name + ".fbx");
            var importer = (ModelImporter)AssetImporter.GetAtPath(Folder + name + ".fbx");
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.generateSecondaryUV = true;
            importer.secondaryUVPackMargin = 8;
            importer.importAnimation = false;
            importer.SaveAndReimport();
        }
        Material("Paint", "Detail_Paint.mat", new Color(.29f,.36f,.32f), .28f);
        Material("Steel", "Social_Steel.mat", new Color(.42f,.44f,.43f), .5f);
        Material("Rubber", "Finish_Rubber.mat", new Color(.04f,.045f,.04f), .1f);
        Material("Timber", "Detail_Timber.mat", new Color(.58f,.48f,.35f), .18f);
        Material("Paper", "Detail_Paper.mat", new Color(.73f,.69f,.57f), .1f);
        Material("Cardboard", "Detail_Paper.mat", new Color(.42f,.32f,.21f), .06f);
        Material("Red", "Detail_Paint.mat", new Color(.37f,.12f,.08f), .24f);
        var station = GameObject.Find("Map_Station").transform;
        var old = station.Find("WorkshopPolish");
        if (old != null) Object.DestroyImmediate(old.gameObject);
        var root = new GameObject("WorkshopPolish").transform;
        root.SetParent(station, false);
        Replace("WorkshopBench", "WorkshopDetailBench", root);
        Replace("WorkshopStockA", "WorkshopSupplyRackA", root);
        Replace("WorkshopStockB", "WorkshopSupplyRackB", root);

        // Preserve the existing task toolbox, but separate it from the vice and socket tray.
        PlaceToolbox("WorkshopTools", new Vector3(12.72f,.958f,7.54f), 180);
        GameObject.Find("WorkshopSocketTray").transform.position = new Vector3(13.30f,.958f,7.49f);
        PlaceToolbox("WorkshopSpareTools", new Vector3(13.88f,.958f,7.50f), 90);
        Physics.SyncTransforms();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[StationWorkshop] Detailed bench and varied maintenance stock saved.");
    }

    [MenuItem("Tools/Interrogation Room/Station Rebuild/24 Align workshop toolboxes")]
    public static void AlignToolboxes()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (Application.isPlaying || scene.path != "Assets/Scenes/Room.unity")
            throw new InvalidOperationException("Open Room in Edit Mode.");
        PlaceToolbox("WorkshopTools", new Vector3(12.72f,.958f,7.54f), 180);
        PlaceToolbox("WorkshopSpareTools", new Vector3(13.88f,.958f,7.50f), 90);
        Physics.SyncTransforms();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void PlaceToolbox(string name, Vector3 bottom, float yaw)
    {
        var go = GameObject.Find(name);
        if (go == null) throw new InvalidOperationException("Missing toolbox " + name);
        go.transform.rotation = Quaternion.Euler(90, yaw, 0);
        var renderers = go.GetComponentsInChildren<Renderer>().Where(r => r.enabled).ToArray();
        var bounds = renderers[0].bounds;
        foreach (var r in renderers.Skip(1)) bounds.Encapsulate(r.bounds);
        go.transform.position += bottom - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
    }

    private static void Material(string key, string templateName, Color color, float smoothness)
    {
        string path = Folder + "Workshop_" + key + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            var template = AssetDatabase.LoadAssetAtPath<Material>(Folder + templateName);
            if (template == null) throw new InvalidOperationException("Missing material " + templateName);
            material = new Material(template);
            AssetDatabase.CreateAsset(material, path);
        }
        material.SetColor("_BaseColor", color);
        material.SetFloat("_Smoothness", smoothness);
        if (key == "Cardboard")
        {
            material.SetTexture("_BaseMap", null);
            material.SetTexture("_BumpMap", null);
            material.DisableKeyword("_NORMALMAP");
        }
        material.enableInstancing = true;
        EditorUtility.SetDirty(material);
    }

    private static void Replace(string name, string model, Transform root)
    {
        var previous = GameObject.Find(name);
        if (previous == null) throw new InvalidOperationException("Missing workshop furniture " + name);
        foreach (var r in previous.GetComponentsInChildren<Renderer>()) r.enabled = false;
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + model + ".fbx");
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root);
        go.name = model;
        go.transform.SetPositionAndRotation(previous.transform.position, previous.transform.rotation);
        foreach (var r in go.GetComponentsInChildren<MeshRenderer>())
        {
            string key = r.name.Substring(r.name.LastIndexOf('_') + 1).Split('.')[0];
            r.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(Folder + "Workshop_" + key + ".mat");
            if (r.sharedMaterial == null) throw new InvalidOperationException("Missing workshop material " + key);
            r.receiveGI = ReceiveGI.Lightmaps;
            GameObjectUtility.SetStaticEditorFlags(r.gameObject, StaticEditorFlags.ContributeGI |
                StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.ReflectionProbeStatic);
        }
    }
}
