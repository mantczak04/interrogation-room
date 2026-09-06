using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Requested office grouping and corridor clearance corrections.</summary>
public static class StationLayoutClearance
{
    [MenuItem("Tools/Interrogation Room/Station Rebuild/26 Correct furniture clearance")]
    public static void Apply()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (Application.isPlaying || scene.path != "Assets/Scenes/Room.unity")
            throw new InvalidOperationException("Open Room in Edit Mode.");
        var desk = GameObject.Find("OfficeWorkingDesk");
        var delta = new Vector3(-12.55f,.005f,13.27f) - desk.transform.position;
        foreach (string name in new[] { "OfficeWorkingDesk", "Office_Chair", "OfficeTaskLamp", "OfficeDisplay",
            "OfficePhone", "OfficeMonitor", "OfficeKeyboard", "OfficeRecords", "OfficeNotebook",
            "OfficeDetailDesk_OfficeWorkingDesk", "OfficeDetailPhone_OfficePhone" })
            GameObject.Find(name).transform.position += delta;
        Center("Office_Sofa", -6.83f,12.70f);
        Center("Office_CoffeeTable", -8.30f,12.70f);
        GameObject.Find("OfficeBoard").transform.position = new Vector3(-6.34f,2.2f,12.60f);
        Center("Korytarz_LawkaW",-1.70f,3.67f);
        Center("Korytarz_LawkaE",1.70f,3.67f);
        Center("Korytarz_Wieszak",3.90f,5.55f);
        Center("Korytarz_Roslina_Natural",-5.64f,5.65f);
        GameObject.Find("Korytarz_Roslina").transform.position = new Vector3(-5.64f,.02f,5.65f);

        // Floors and threshold tops previously shared y=0 over their seam.
        // A four-millimetre raised sill separates the rendered surfaces.
        const string sillPath="Assets/Art/Environment/StationRebuild/ThresholdSteel.mat";
        var sillMaterial=AssetDatabase.LoadAssetAtPath<Material>(sillPath);
        if(sillMaterial==null) {
            sillMaterial=new Material(GameObject.Find("Threshold_DrzwiWarsztat").GetComponent<Renderer>().sharedMaterial);
            AssetDatabase.CreateAsset(sillMaterial,sillPath);
        }
        sillMaterial.SetTexture("_BaseMap",null);
        sillMaterial.SetTexture("_BumpMap",null);
        sillMaterial.DisableKeyword("_NORMALMAP");
        sillMaterial.SetColor("_BaseColor",new Color(.24f,.26f,.27f));
        sillMaterial.SetFloat("_Metallic",.65f);
        sillMaterial.SetFloat("_Smoothness",.25f);
        EditorUtility.SetDirty(sillMaterial);
        foreach (var t in GameObject.Find("Map_Station").GetComponentsInChildren<Transform>())
            if (t.name.StartsWith("Threshold_")) {
                var p=t.position; p.y=.004f; t.position=p;
                t.GetComponent<Renderer>().sharedMaterial=sillMaterial;
            }
        FlushThresholdCollision();
        OpenToolbox();
        ReplaceSpareToolbox();
        Physics.SyncTransforms();
        foreach (var seat in GameObject.Find("Office_Sofa").GetComponentsInChildren<InterrogationRoom.Gameplay.Interaction.NetworkChairSeat>())
            seat.transform.Find("StationStandPoint").position = new Vector3(-7.85f,.02f,11.45f);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }

    private static void Center(string name,float x,float z)
    {
        var go=GameObject.Find(name);
        var rr=go.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).ToArray();
        var b=rr[0].bounds;
        foreach(var r in rr.Skip(1)) b.Encapsulate(r.bounds);
        go.transform.position+=new Vector3(x-b.center.x,0,z-b.center.z);
    }

    [MenuItem("Tools/Interrogation Room/Station Rebuild/29 Flatten threshold collision")]
    public static void FlushThresholdCollision()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(Application.isPlaying || scene.path!="Assets/Scenes/Room.unity")
            throw new InvalidOperationException("Open Room in Edit Mode.");
        foreach(var t in GameObject.Find("Map_Station").GetComponentsInChildren<Transform>()) {
            if(!t.name.StartsWith("Threshold_")) continue;
            var source=t.GetComponent<MeshCollider>();
            var child=t.Find("FlushFloorCollider");
            if(child==null) { child=new GameObject("FlushFloorCollider").transform;child.SetParent(t,false); }
            child.localPosition=new Vector3(0,-t.position.y,0);
            var collider=child.GetComponent<MeshCollider>();
            if(collider==null) collider=child.gameObject.AddComponent<MeshCollider>();
            collider.sharedMesh=source.sharedMesh;
            source.enabled=false;
        }
        Physics.SyncTransforms();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    [MenuItem("Tools/Interrogation Room/Station Rebuild/27 Replace sideways spare toolbox")]
    public static void ReplaceSpareToolbox()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(Application.isPlaying || scene.path!="Assets/Scenes/Room.unity")
            throw new InvalidOperationException("Open Room in Edit Mode.");
        var source=GameObject.Find("WorkshopOpenToolbox");
        var old=GameObject.Find("WorkshopSpareTools");
        if(source==null || old==null) throw new InvalidOperationException("Missing workshop toolboxes.");
        foreach(var r in old.GetComponentsInChildren<Renderer>()) r.enabled=false;
        var previous=GameObject.Find("WorkshopOpenSpareToolbox");
        if(previous!=null) UnityEngine.Object.DestroyImmediate(previous);
        var replacement=UnityEngine.Object.Instantiate(source,old.transform.parent);
        replacement.name="WorkshopOpenSpareToolbox";
        replacement.transform.SetPositionAndRotation(new Vector3(13.88f,.958f,7.50f),Quaternion.identity);
        replacement.transform.localScale=Vector3.one*.8f;
        foreach(var r in replacement.GetComponentsInChildren<Renderer>()) r.lightmapIndex=-1;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    [MenuItem("Tools/Interrogation Room/Station Rebuild/28 Refresh detailed toolboxes")]
    public static void RefreshToolboxes()
    {
        if(Application.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!="Assets/Scenes/Room.unity")
            throw new InvalidOperationException("Open Room in Edit Mode.");
        OpenToolbox();
        ReplaceSpareToolbox();
        AssetDatabase.SaveAssets();
    }

    private static void OpenToolbox()
    {
        const string folder="Assets/Art/Environment/StationRebuild/";
        string path=folder+"WorkshopOpenToolbox.fbx";
        AssetDatabase.ImportAsset(path);
        var importer=(ModelImporter)AssetImporter.GetAtPath(path);
        importer.materialImportMode=ModelImporterMaterialImportMode.None;
        importer.generateSecondaryUV=true;
        importer.secondaryUVPackMargin=8;
        importer.SaveAndReimport();
        var old=GameObject.Find("WorkshopTools");
        old.GetComponent<Renderer>().enabled=false;
        var existing=GameObject.Find("WorkshopOpenToolbox");
        if(existing!=null) UnityEngine.Object.DestroyImmediate(existing);
        var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),old.transform.parent);
        go.name="WorkshopOpenToolbox";
        go.transform.SetPositionAndRotation(new Vector3(12.72f,.958f,7.50f),Quaternion.identity);
        foreach(var r in go.GetComponentsInChildren<MeshRenderer>()) {
            string key=r.name.Substring(r.name.LastIndexOf('_')+1).Split('.')[0];
            string materialPath=folder+"Toolbox_"+key+".mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(mat==null) {
                string template=(key=="Red"||key=="Rust")?"Plastic":key;
                mat=new Material(AssetDatabase.LoadAssetAtPath<Material>(folder+"Office_"+template+".mat"));
                AssetDatabase.CreateAsset(mat,materialPath);
            }
            if(key=="Paint") { mat.SetColor("_BaseColor",new Color(.23f,.30f,.25f));mat.SetFloat("_Smoothness",.32f); }
            if(key=="Steel") { mat.SetColor("_BaseColor",new Color(.48f,.51f,.53f));mat.SetFloat("_Metallic",.85f);mat.SetFloat("_Smoothness",.46f); }
            if(key=="Red") { mat.SetColor("_BaseColor",new Color(.34f,.035f,.018f));mat.SetFloat("_Smoothness",.25f); }
            if(key=="Rust") { mat.SetColor("_BaseColor",new Color(.19f,.07f,.025f));mat.SetFloat("_Smoothness",.06f); }
            EditorUtility.SetDirty(mat);
            r.sharedMaterial=mat;
            GameObjectUtility.SetStaticEditorFlags(r.gameObject,StaticEditorFlags.ContributeGI|StaticEditorFlags.BatchingStatic|StaticEditorFlags.OccludeeStatic|StaticEditorFlags.ReflectionProbeStatic);
        }
    }
}
