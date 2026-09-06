using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class StationFinalPolish
{
    private const string Folder="Assets/Art/Environment/StationRebuild/";
    [MenuItem("Tools/Interrogation Room/Station Rebuild/33 Final room cleanup")]
    public static void Apply()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(Application.isPlaying||scene.path!="Assets/Scenes/Room.unity")throw new InvalidOperationException("Open Room in Edit Mode.");
        var station=GameObject.Find("Map_Station").transform;
        var old=station.Find("FinalRoomPolish");
        if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
        var root=new GameObject("FinalRoomPolish").transform;root.SetParent(station,false);
        string alarmPath=Folder+"DetailedArchiveAlarm.fbx";
        AssetDatabase.ImportAsset(alarmPath);
        var importer=(ModelImporter)AssetImporter.GetAtPath(alarmPath);
        importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.generateSecondaryUV=true;importer.SaveAndReimport();

        foreach(var r in station.Find("BlenderArchitecture").GetComponentsInChildren<Renderer>().Where(r=>r.name.StartsWith("Furniture_")&&r.bounds.center.x< -6&&r.bounds.center.z>1.3f&&r.bounds.center.z<8))
        {
            r.enabled=false;
            foreach(var c in r.GetComponents<Collider>())c.enabled=false;
        }
        int index=0;
        foreach(var p in new[]{new Vector3(-13.3f,.01f,7.70f),new Vector3(-7.8f,.01f,7.70f),new Vector3(-13.3f,.01f,1.65f)})
        {
            var rack=Model("DetailedSupplyRack"+(index++%2),root,p,p.z<2?180:0,"Shelving_");
            var box=rack.AddComponent<BoxCollider>();box.center=new Vector3(0,1.05f,0);box.size=new Vector3(1.64f,2.10f,.49f);
        }
        Center("EvidenceCarton",new Vector3(-14.1f,.22f,2.55f));

        var desk=GameObject.Find("Archiwum_Biurko").transform;
        var pivot=desk.Find("Visual_Desk_Wood").GetComponent<Renderer>().bounds.center;pivot.y=0;
        var turn=Quaternion.Euler(0,Mathf.DeltaAngle(desk.eulerAngles.y,0),0);
        var target=new Vector3(-8,0,-4.43f);
        foreach(string n in new[]{"Archiwum_Biurko","Archiwum_Monitor","Archiwum_Klawiatura","Archiwum_Kosz","Spot_Typewriter","B5 Receipt Clue","ArchiveDetailedPhone"})
        {
            var t=GameObject.Find(n).transform;t.SetPositionAndRotation(target+turn*(t.position-pivot),turn*t.rotation);Record(t);
        }
        var chair=GameObject.Find("Archiwum_Krzeslo").transform;chair.rotation=Quaternion.identity;
        Center("Archiwum_Krzeslo",new Vector3(-8,.57f,-3.28f));Stand("Archiwum_Krzeslo",new Vector3(-6.9f,.02f,-3.2f));
        var side=GameObject.Find("B4 Suspicious Item").transform;
        var sb=side.GetComponentsInChildren<Renderer>().First(r=>r.name=="Visual_CoffeeTable_DarkWood").bounds;
        side.position+=new Vector3(-9.75f-sb.center.x,0,-4.48f-sb.center.z);Record(side);
        var token=GameObject.Find("GameplayItem_SuspiciousToken").transform;token.position=new Vector3(-9.75f,token.position.y,-4.48f);Record(token);

        var alarm=GameObject.Find("B4 Archive Alarm");
        foreach(var r in alarm.GetComponentsInChildren<Renderer>().Where(r=>!r.name.StartsWith("StatusIndicator")))r.enabled=false;
        var detailedAlarm=Model("DetailedArchiveAlarm",root,alarm.transform.position,180,"Fixture_");
        var red=AssetDatabase.LoadAssetAtPath<Material>(Folder+"AlarmButtonRed.mat");
        if(red==null){red=new Material(AssetDatabase.LoadAssetAtPath<Material>(Folder+"Fixture_Enamel.mat"));red.SetColor("_BaseColor",new Color(.48f,.035f,.02f));AssetDatabase.CreateAsset(red,Folder+"AlarmButtonRed.mat");}
        detailedAlarm.GetComponentsInChildren<Renderer>().First(r=>r.name.EndsWith("_Blue")).sharedMaterial=red;
        foreach(var r in station.GetComponentsInChildren<Renderer>().Where(r=>r.name=="FireAlarm"&&Mathf.Abs(r.bounds.center.x)<4))
        {
            r.transform.Rotate(0,180,0,Space.Self);
            // Save a stable facing rather than accumulating turns on repeated runs.
            r.transform.rotation=Quaternion.Euler(0,r.bounds.center.x<0?270:90,0);
            Record(r.transform);
        }
        foreach(string n in new[]{"Socjalny_Ekspres","Socjalny_Radio","Authored_MugSet"})
        {
            var t=GameObject.Find(n).transform;var b=Bounds(t.gameObject);t.position+=Vector3.right*(14.87f-b.max.x);Record(t);
        }
        var mugRoot=GameObject.Find("Spot_MugChoir").transform;
        var mugs=GameObject.Find("Authored_MugSet").transform;
        var mugPosition=mugs.position;
        mugRoot.position=new Vector3(mugPosition.x,1.02f,mugPosition.z);
        mugs.position=mugPosition;Record(mugRoot);Record(mugs);
        Panel("B5 Service Panel");
        Center("Briefing_TableA",new Vector3(9.45f,.39f,11.55f));
        Center("Briefing_TableB",new Vector3(12.40f,.39f,11.55f));
        foreach(string suffix in new[]{"A","C"})Center("Briefing_Chair"+suffix,new Vector3(9.45f,.49f,suffix=="A"?10.6f:12.5f));
        foreach(string suffix in new[]{"B","D"})Center("Briefing_Chair"+suffix,new Vector3(12.4f,.49f,suffix=="B"?10.6f:12.5f));
        Stand("Briefing_ChairA",new Vector3(8.2f,.02f,10.6f));Stand("Briefing_ChairC",new Vector3(8.2f,.02f,12.5f));
        Stand("Briefing_ChairB",new Vector3(13.65f,.02f,10.6f));Stand("Briefing_ChairD",new Vector3(11.15f,.02f,12.5f));
        var board=GameObject.Find("BriefingBoard").transform;board.position=new Vector3(13.5f,2,8.30f);Record(board);
        var sofa=Bounds(GameObject.Find("Office_Sofa"));var coffee=Bounds(GameObject.Find("Office_CoffeeTable"));
        Center("Office_CoffeeTable",new Vector3(sofa.min.x-coffee.extents.x-.12f,coffee.center.y,sofa.center.z));
        Physics.SyncTransforms();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
    }
    private static void Panel(string name)
    {
        var root=GameObject.Find(name);var scan=root.GetComponentsInChildren<Renderer>().First(r=>r.name=="ScannedVisual").transform;
        scan.localRotation=Quaternion.Euler(0,180,0);
        var mesh=scan.GetComponent<MeshFilter>().sharedMesh.bounds;var matrix=Matrix4x4.TRS(scan.position,scan.rotation,scan.lossyScale);
        var b=new Bounds(matrix.MultiplyPoint3x4(mesh.center),Vector3.zero);
        for(int i=0;i<8;i++)b.Encapsulate(matrix.MultiplyPoint3x4(mesh.center+Vector3.Scale(mesh.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1))));
        var dx=14.94f-b.max.x;scan.position+=Vector3.right*dx;Record(scan);
        var box=root.GetComponent<BoxCollider>();box.center=root.transform.InverseTransformPoint(b.center+Vector3.right*dx);box.size=new Vector3(b.size.z,b.size.y,b.size.x);
    }
    private static GameObject Model(string model,Transform root,Vector3 p,float yaw,string prefix)
    {
        var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+model+".fbx"),root);
        go.transform.SetPositionAndRotation(p,Quaternion.Euler(0,yaw,0));Record(go.transform);
        foreach(var r in go.GetComponentsInChildren<MeshRenderer>())
        {
            var key=r.name.Substring(r.name.LastIndexOf('_')+1).Split('.')[0];
            r.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Folder+prefix+key+".mat");
            GameObjectUtility.SetStaticEditorFlags(r.gameObject,StaticEditorFlags.ContributeGI|StaticEditorFlags.BatchingStatic|StaticEditorFlags.ReflectionProbeStatic);
            PrefabUtility.RecordPrefabInstancePropertyModifications(r);
        }
        return go;
    }
    private static Bounds Bounds(GameObject go){var rs=go.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).ToArray();var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);return b;}
    private static void Center(string name,Vector3 p){var go=GameObject.Find(name);go.transform.position+=p-Bounds(go).center;Record(go.transform);}
    private static void Record(Transform t){EditorUtility.SetDirty(t);PrefabUtility.RecordPrefabInstancePropertyModifications(t);}
    private static void Stand(string name,Vector3 p){var so=new SerializedObject(GameObject.Find(name).GetComponent<InterrogationRoom.Gameplay.Interaction.NetworkChairSeat>());var t=(Transform)so.FindProperty("standPoint").objectReferenceValue;t.position=p;Record(t);}
}
