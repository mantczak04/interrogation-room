using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Archive desk support and varied storage presentation, with existing gameplay envelopes.</summary>
public static class StationArchiveStoragePolish
{
    private const string Folder="Assets/Art/Environment/StationRebuild/";

    [MenuItem("Tools/Interrogation Room/Station Rebuild/30 Polish archive and storage")]
    public static void Apply()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(Application.isPlaying||scene.path!="Assets/Scenes/Room.unity")
            throw new InvalidOperationException("Open Room in Edit Mode.");
        foreach(var name in new[]{"DetailedArchiveRack","DetailedSupplyRack0","DetailedSupplyRack1"}) {
            string path=Folder+name+".fbx";
            AssetDatabase.ImportAsset(path);
            var importer=(ModelImporter)AssetImporter.GetAtPath(path);
            importer.materialImportMode=ModelImporterMaterialImportMode.None;
            importer.generateSecondaryUV=true;
            importer.secondaryUVPackMargin=8;
            importer.SaveAndReimport();
        }
        var station=GameObject.Find("Map_Station").transform;
        var old=station.Find("ArchiveStoragePolish");
        if(old!=null) UnityEngine.Object.DestroyImmediate(old.gameObject);
        var root=new GameObject("ArchiveStoragePolish").transform;
        root.SetParent(station,false);
        foreach(string name in new[]{"ArchiveNorthA","ArchiveNorthB","ArchiveWest"})
            Replace(GameObject.Find(name),"DetailedArchiveRack",root);
        var racks=station.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("StorageWest_")||t.name.StartsWith("StorageEast_")).OrderBy(t=>t.name).ToArray();
        for(int i=0;i<racks.Length;i++)Replace(racks[i].gameObject,"DetailedSupplyRack"+(i%2),root);

        // Use the visible desktop height rather than the old placeholder collider top.
        float top=GameObject.Find("Archiwum_Biurko").transform.Find("Visual_Desk_Wood").GetComponent<Renderer>().bounds.max.y;
        Bottom(GameObject.Find("Spot_Typewriter"),top+.002f);
        Bottom(GameObject.Find("B5 Receipt Clue"),top+.002f);
        Bottom(GameObject.Find("Archiwum_Biurko").transform.Find("Visual_Computer_OldBeige").gameObject,top+.002f);
        foreach(var r in GameObject.Find("ArchivePhone").GetComponentsInChildren<Renderer>())r.enabled=false;
        var phone=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"OfficeDetailPhone.fbx"),root);
        phone.name="ArchiveDetailedPhone";
        phone.transform.SetPositionAndRotation(new Vector3(-8.76f,top+.002f,-3.62f),Quaternion.Euler(0,270,0));
        phone.transform.localScale=Vector3.one*.75f;
        foreach(var r in phone.GetComponentsInChildren<MeshRenderer>()) {
            string key=r.name.Substring(r.name.LastIndexOf('_')+1).Split('.')[0];
            r.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Folder+"Office_"+key+".mat");
        }
        Physics.SyncTransforms();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }

    private static void Bottom(GameObject go,float y)
    {
        var rr=go.GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();
        float bottom=rr.Min(r=>r.bounds.min.y);
        go.transform.position+=Vector3.up*(y-bottom);
    }

    private static void Replace(GameObject previous,string model,Transform parent)
    {
        foreach(var r in previous.GetComponentsInChildren<Renderer>())r.enabled=false;
        var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+model+".fbx"),parent);
        go.name="Detail_"+previous.name;
        go.transform.SetPositionAndRotation(previous.transform.position,previous.transform.rotation);
        foreach(var r in go.GetComponentsInChildren<MeshRenderer>()) {
            string key=r.name.Substring(r.name.LastIndexOf('_')+1).Split('.')[0];
            string path=Folder+"Shelving_"+key+".mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(mat==null) {
                string template=key=="Cardboard"?"Workshop_Cardboard":key=="Blue"?"Office_Paint":"Office_"+key;
                mat=new Material(AssetDatabase.LoadAssetAtPath<Material>(Folder+template+".mat"));
                AssetDatabase.CreateAsset(mat,path);
            }
            if(key=="Blue")mat.SetColor("_BaseColor",new Color(.12f,.19f,.23f));
            if(key=="Paint")mat.SetColor("_BaseColor",new Color(.24f,.30f,.27f));
            if(key=="Paper")mat.SetColor("_BaseColor",new Color(.63f,.60f,.52f));
            EditorUtility.SetDirty(mat);
            r.sharedMaterial=mat;
            GameObjectUtility.SetStaticEditorFlags(r.gameObject,StaticEditorFlags.ContributeGI|StaticEditorFlags.BatchingStatic|StaticEditorFlags.OccludeeStatic|StaticEditorFlags.ReflectionProbeStatic);
        }
    }
}
