using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using InterrogationRoom.Gameplay.Interaction;

/// <summary>Presentation-only fixture pass. No lighting bake or door-state changes.</summary>
public static class StationRepeatedFixturesPolish
{
    private const string Folder="Assets/Art/Environment/StationRebuild/";

    [MenuItem("Tools/Interrogation Room/Station Rebuild/31 Refine repeated fixtures")]
    public static void Apply()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(Application.isPlaying||scene.path!="Assets/Scenes/Room.unity")
            throw new InvalidOperationException("Open Room in Edit Mode.");
        foreach(var model in new[]{"RefinedDoorLeaf","RefinedRadiator","RefinedFixtureTrim","RefinedBriefingTable"}) {
            string path=Folder+model+".fbx";
            AssetDatabase.ImportAsset(path);
            var importer=(ModelImporter)AssetImporter.GetAtPath(path);
            importer.materialImportMode=ModelImporterMaterialImportMode.None;
            importer.generateSecondaryUV=true;
            importer.secondaryUVPackMargin=8;
            importer.SaveAndReimport();
        }
        var station=GameObject.Find("Map_Station").transform;
        var old=station.Find("RepeatedFixturesPolish");
        if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
        var root=new GameObject("RepeatedFixturesPolish").transform;
        root.SetParent(station,false);
        foreach(var door in station.GetComponentsInChildren<NetworkDoor>()) {
            var leaf=(Transform)new SerializedObject(door).FindProperty("doorLeaf").objectReferenceValue;
            var existing=leaf.Find("DetailedDoorVisual");
            if(existing!=null)UnityEngine.Object.DestroyImmediate(existing.gameObject);
            foreach(var r in leaf.GetComponentsInChildren<Renderer>())r.enabled=false;
            var visual=Add("RefinedDoorLeaf",leaf,Vector3.zero,Quaternion.identity,false);
            visual.name="DetailedDoorVisual";
        }
        var radiators=station.GetComponentsInChildren<Transform>().Where(t=>System.Text.RegularExpressions.Regex.IsMatch(t.name,@"^Radiator_\d+$")).ToArray();
        foreach(var radiator in radiators) {
            foreach(var r in radiator.GetComponentsInChildren<Renderer>())r.enabled=false;
            var go=Add("RefinedRadiator",root,radiator.position,radiator.rotation,true);
            go.name="Detailed_"+radiator.name;
        }
        foreach(var light in station.GetComponentsInChildren<Light>().Where(l=>l.name.StartsWith("Practical_"))) {
            var p=light.transform.position;p.y=3.29f;
            var go=Add("RefinedFixtureTrim",root,p,Quaternion.identity,true);
            go.name="Trim_"+light.name;
        }
        // Original fixture bodies are part of combined architecture meshes.
        foreach(var r in station.GetComponentsInChildren<Renderer>().Where(r=>r.name.EndsWith("_Metal")&&r.bounds.center.y>3f))
            r.sharedMaterial=Material("Enamel");
        foreach(var r in station.GetComponentsInChildren<Renderer>().Where(r=>r.name.StartsWith("Lining_")))
            r.sharedMaterial=Material("Timber");
        for(int i=0;i<2;i++) {
            var table=GameObject.Find(i==0?"Briefing_TableA":"Briefing_TableB");
            foreach(var r in table.GetComponentsInChildren<Renderer>())r.enabled=false;
            Add("RefinedBriefingTable",root,new Vector3(10.2f+i*1.6f,.005f,11.2f),Quaternion.identity,true);
        }
        foreach(var name in new[]{"Office_Plastic","Finish_Plastic","Toolbox_Plastic","Shelving_Cardboard","Workshop_Cardboard"}) {
            var mat=AssetDatabase.LoadAssetAtPath<Material>(Folder+name+".mat");
            if(mat==null)continue;
            mat.SetFloat("_Metallic",0);
            mat.SetFloat("_Smoothness",name.Contains("Cardboard")?.04f:.22f);
            if(mat.HasProperty("_BumpScale"))mat.SetFloat("_BumpScale",.25f);
            EditorUtility.SetDirty(mat);
        }
        Physics.SyncTransforms();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }

    private static GameObject Add(string model,Transform parent,Vector3 position,Quaternion rotation,bool isStatic)
    {
        var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+model+".fbx"),parent);
        go.transform.localPosition=position;
        go.transform.localRotation=rotation;
        foreach(var r in go.GetComponentsInChildren<MeshRenderer>()) {
            string key=r.name.Substring(r.name.LastIndexOf('_')+1).Split('.')[0];
            r.sharedMaterial=Material(key);
            if(isStatic)GameObjectUtility.SetStaticEditorFlags(r.gameObject,StaticEditorFlags.ContributeGI|StaticEditorFlags.BatchingStatic|StaticEditorFlags.OccludeeStatic|StaticEditorFlags.ReflectionProbeStatic);
        }
        return go;
    }

    private static Material Material(string key)
    {
        string path=Folder+"Fixture_"+key+".mat";
        var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(mat!=null)return mat;
        string template=(key=="Brass"?"Steel":key=="Enamel"?"Ivory":key=="Blue"?"Paint":key);
        mat=new Material(AssetDatabase.LoadAssetAtPath<Material>(Folder+"Office_"+template+".mat"));
        if(key=="Steel"||key=="Brass"||key=="Enamel"||key=="Rubber") {
            mat.SetTexture("_BaseMap",null);mat.SetTexture("_BumpMap",null);
            mat.SetTexture("_MetallicGlossMap",null);mat.DisableKeyword("_NORMALMAP");mat.DisableKeyword("_METALLICSPECGLOSSMAP");
        }
        if(key=="Steel") {mat.SetColor("_BaseColor",new Color(.38f,.41f,.43f));mat.SetFloat("_Metallic",.85f);mat.SetFloat("_Smoothness",.42f);}
        if(key=="Brass") {mat.SetColor("_BaseColor",new Color(.43f,.31f,.13f));mat.SetFloat("_Metallic",.8f);mat.SetFloat("_Smoothness",.36f);}
        if(key=="Enamel") {mat.SetColor("_BaseColor",new Color(.65f,.64f,.57f));mat.SetFloat("_Metallic",.08f);mat.SetFloat("_Smoothness",.3f);}
        if(key=="Timber") {mat.SetColor("_BaseColor",new Color(.44f,.35f,.26f));mat.SetFloat("_Smoothness",.2f);mat.SetFloat("_BumpScale",.3f);}
        if(key=="Blue")mat.SetColor("_BaseColor",new Color(.10f,.17f,.21f));
        mat.enableInstancing=true;
        AssetDatabase.CreateAsset(mat,path);
        return mat;
    }
}
