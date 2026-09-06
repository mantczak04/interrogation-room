using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

/// <summary>Office furniture and desktop composition, preserving authored gameplay objects.</summary>
public static class StationOfficePolish
{
    private const string Folder = "Assets/Art/Environment/StationRebuild/";

    [MenuItem("Tools/Interrogation Room/Station Rebuild/25 Polish office")]
    public static void Apply()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (Application.isPlaying || scene.path != "Assets/Scenes/Room.unity")
            throw new InvalidOperationException("Open Room in Edit Mode.");
        foreach (var name in new[] { "OfficeDetailDesk", "OfficeDetailPhone", "OfficeFilingCabinet" })
        {
            AssetDatabase.ImportAsset(Folder + name + ".fbx");
            var importer = (ModelImporter)AssetImporter.GetAtPath(Folder + name + ".fbx");
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.generateSecondaryUV = true;
            importer.secondaryUVPackMargin = 8;
            importer.importAnimation = false;
            importer.SaveAndReimport();
        }
        Material("Paint", "Detail_Paint.mat", new Color(.34f,.40f,.37f), .3f);
        Material("Steel", "Social_Steel.mat", new Color(.44f,.46f,.45f), .5f);
        Material("Rubber", "Finish_Rubber.mat", new Color(.04f,.045f,.04f), .1f);
        Material("Timber", "Detail_Timber.mat", new Color(.58f,.48f,.35f), .22f);
        Material("Paper", "Detail_Paper.mat", new Color(.78f,.75f,.67f), .1f);
        Material("Plastic", "Finish_Plastic.mat", new Color(.12f,.14f,.13f), .28f);
        Material("Ivory", "Finish_Ivory.mat", new Color(.67f,.66f,.58f), .3f);
        var station = GameObject.Find("Map_Station").transform;
        var old = station.Find("OfficePolish");
        if (old != null) Object.DestroyImmediate(old.gameObject);
        var root = new GameObject("OfficePolish").transform;
        root.SetParent(station, false);
        Replace("OfficeWorkingDesk", "OfficeDetailDesk", root);
        Replace("OfficePhone", "OfficeDetailPhone", root);
        Replace("OfficeStorageA", "OfficeFilingCabinet", root);
        Replace("OfficeStorageB", "OfficeFilingCabinet", root);
        PlaceBottom("OfficeRecords", new Vector3(-12.68f,.77f,12.50f));
        var records = Bounds(GameObject.Find("OfficeRecords"));
        PlaceBottom("OfficeNotebook", new Vector3(records.center.x,records.max.y + .001f,records.center.z));
        Physics.SyncTransforms();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[StationOffice] Detailed desk, telephone, filing cabinets and paperwork saved.");
    }

    private static Bounds Bounds(GameObject go)
    {
        var rr = go.GetComponentsInChildren<Renderer>().Where(r => r.enabled).ToArray();
        var b = rr[0].bounds;
        foreach (var r in rr.Skip(1)) b.Encapsulate(r.bounds);
        return b;
    }

    private static void PlaceBottom(string name, Vector3 bottom)
    {
        var go = GameObject.Find(name);
        var b = Bounds(go);
        go.transform.position += bottom - new Vector3(b.center.x,b.min.y,b.center.z);
    }

    private static void Material(string key, string templateName, Color color, float smoothness)
    {
        string path = Folder + "Office_" + key + ".mat";
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
        material.enableInstancing = true;
        EditorUtility.SetDirty(material);
    }

    private static void Replace(string name, string model, Transform root)
    {
        var previous = GameObject.Find(name);
        if (previous == null) throw new InvalidOperationException("Missing office furniture " + name);
        foreach (var r in previous.GetComponentsInChildren<Renderer>()) r.enabled = false;
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + model + ".fbx");
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root);
        go.name = model + "_" + name;
        go.transform.SetPositionAndRotation(previous.transform.position, previous.transform.rotation);
        foreach (var r in go.GetComponentsInChildren<MeshRenderer>())
        {
            string key = r.name.Substring(r.name.LastIndexOf('_') + 1).Split('.')[0];
            r.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(Folder + "Office_" + key + ".mat");
            if (r.sharedMaterial == null) throw new InvalidOperationException("Missing office material " + key);
            r.receiveGI = ReceiveGI.Lightmaps;
            GameObjectUtility.SetStaticEditorFlags(r.gameObject, StaticEditorFlags.ContributeGI |
                StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.ReflectionProbeStatic);
        }
    }
}
