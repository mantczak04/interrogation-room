using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

/// <summary>Staff-room presentation; retains authored interactions and furniture collision.</summary>
public static class StationSocialPolish
{
    private const string Folder = "Assets/Art/Environment/StationRebuild/";

    [MenuItem("Tools/Interrogation Room/Station Rebuild/22 Polish staff room")]
    public static void Apply()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (Application.isPlaying || scene.path != "Assets/Scenes/Room.unity")
            throw new InvalidOperationException("Open Room in Edit Mode.");
        foreach (var name in new[] { "SocialKitchenDetail", "SocialFridgeDetail" })
        {
            AssetDatabase.ImportAsset(Folder + name + ".fbx");
            var importer = (ModelImporter)AssetImporter.GetAtPath(Folder + name + ".fbx");
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.generateSecondaryUV = true;
            importer.secondaryUVPackMargin = 8;
            importer.importAnimation = false;
            importer.SaveAndReimport();
        }
        MakeMaterial("Enamel", "Finish_Ivory.mat", new Color(.53f,.57f,.51f), .32f, 0);
        MakeMaterial("Steel", "Finish_Steel.mat", new Color(.48f,.50f,.48f), .6f, .85f);
        MakeMaterial("Rubber", "Finish_Rubber.mat", new Color(.035f,.038f,.035f), .08f, 0);
        MakeMaterial("Worktop", "Scanned/concrete_floor_worn_001/Surface.mat", new Color(.43f,.45f,.41f), .28f, 0);
        MakeMaterial("Paper", "Detail_Paper.mat", new Color(.78f,.74f,.61f), .08f, 0);
        var station = GameObject.Find("Map_Station").transform;
        var old = station.Find("SocialPolish");
        if (old != null) Object.DestroyImmediate(old.gameObject);
        var root = new GameObject("SocialPolish").transform;
        root.SetParent(station, false);
        Replace("SocialKitchen", "SocialKitchenDetail", root);
        Replace("SocialFridge", "SocialFridgeDetail", root);

        // Put the notices above the dining area instead of behind full-height lockers.
        GameObject.Find("SocialBoard").transform.position = new Vector3(11.35f,1.97f,-4.91f);
        var floor = station.GetComponentsInChildren<Renderer>().Single(r => r.name == "Shell_-2_0_Tile");
        var flooring = MakeMaterial("Floor", "Scanned/terrazzo_tiles/Surface.mat", new Color(.72f,.72f,.68f), .22f, 0);
        flooring.mainTextureScale = new Vector2(.25f,.25f);
        floor.sharedMaterial = flooring;
        // Keep the table's timber top, but restore painted steel legs and rails.
        foreach (var r in GameObject.Find("SocialDiningTable").GetComponentsInChildren<Renderer>())
            if (r.name.EndsWith("_Steel")) r.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(Folder + "Finish_Plastic.mat");
        Physics.SyncTransforms();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[StationSocial] Recessed kitchen, detailed fridge, exposed noticeboard and terrazzo floor saved.");
    }

    private static Material MakeMaterial(string key, string source, Color color, float smoothness, float metallic)
    {
        string path = Folder + "Social_" + key + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            var template = AssetDatabase.LoadAssetAtPath<Material>(Folder + source);
            if (template == null) throw new InvalidOperationException("Missing material " + source);
            material = new Material(template);
            AssetDatabase.CreateAsset(material, path);
        }
        material.SetColor("_BaseColor", color);
        material.SetFloat("_Smoothness", smoothness);
        material.SetFloat("_Metallic", metallic);
        material.enableInstancing = true;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void Replace(string targetName, string model, Transform root)
    {
        var target = GameObject.Find(targetName);
        if (target == null) throw new InvalidOperationException("Missing furniture " + targetName);
        foreach (var r in target.GetComponentsInChildren<Renderer>()) r.enabled = false;
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + model + ".fbx");
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root);
        go.name = model;
        go.transform.SetPositionAndRotation(target.transform.position, target.transform.rotation);
        foreach (var r in go.GetComponentsInChildren<MeshRenderer>())
        {
            string key = r.name.Substring(r.name.LastIndexOf('_') + 1).Split('.')[0];
            r.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(Folder + "Social_" + key + ".mat");
            if (r.sharedMaterial == null) throw new InvalidOperationException("Missing social material " + key);
            r.receiveGI = ReceiveGI.Lightmaps;
            GameObjectUtility.SetStaticEditorFlags(r.gameObject, StaticEditorFlags.ContributeGI |
                StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.ReflectionProbeStatic);
        }
    }
}
