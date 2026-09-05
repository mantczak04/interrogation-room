using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

/// <summary>Explicit authoring of furniture groups and their supporting surfaces in Room.</summary>
public static class StationLayoutRefinement
{
    private const string Folder = "Assets/Art/Environment/StationRebuild/";
    private static Transform root;
    private static readonly string[] Models = { "KitchenRun", "StationFridge", "StationDiningTable", "StationArchiveRack", "StationStaffLocker", "StationWorkbench", "StationAlarmPanel", "StationOfficeDesk", "StationMonitor", "StationDeskPhone", "StationThreshold", "StationDoorLining" };

    [MenuItem("Tools/Interrogation Room/Station Rebuild/16 Import refined furnishings")]
    public static void ImportModels()
    {
        foreach (string model in Models)
        {
            string path = Folder + model + ".fbx";
            AssetDatabase.ImportAsset(path);
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.generateSecondaryUV = true;
            importer.secondaryUVPackMargin = 8;
            importer.importAnimation = false;
            importer.SaveAndReimport();
        }
    }

    [MenuItem("Tools/Interrogation Room/Station Rebuild/17 Refine room layouts")]
    public static void Apply()
    {
        if (Application.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != "Assets/Scenes/Room.unity")
            throw new InvalidOperationException("Open Room in Edit Mode.");
        var station = GameObject.Find("Map_Station").transform;
        var previous = station.Find("RefinedFurnishings");
        if (previous != null) Object.DestroyImmediate(previous.gameObject);
        root = new GameObject("RefinedFurnishings").transform;
        root.SetParent(station, false);
        foreach(var door in station.GetComponentsInChildren<InterrogationRoom.Gameplay.Interaction.NetworkDoor>())
        {
            var p=door.transform.position;p.y=0;
            Add("StationThreshold","Threshold_"+door.name,p,door.transform.eulerAngles.y);
            Add("StationDoorLining","Lining_"+door.name,p,door.transform.eulerAngles.y);
        }

        // Replace the old freestanding workshop/storage racks, retaining the evidence layout.
        foreach (var t in station.Find("BlenderArchitecture").GetComponentsInChildren<Transform>(true))
        {
            var r = t.GetComponent<Renderer>();
            if (r != null && t.name.StartsWith("Furniture_") &&
                (r.bounds.center.x > 6 || r.bounds.center.x > -6.1f && r.bounds.center.z < -5)) t.gameObject.SetActive(false);
        }

        Hide("Socjalny_Szafka1", "Socjalny_Szafka2", "Socjalny_Lodowka", "Socjalny_Stol", "SocialLocker");
        Add("KitchenRun", "SocialKitchen", new Vector3(14.6f,.005f,-1.3f), 90);
        Add("StationFridge", "SocialFridge", new Vector3(14.60f,.005f,-3.35f), 90);
        Add("StationDiningTable", "SocialDiningTable", new Vector3(11.3f,.005f,-2.5f));
        Bottom("Socjalny_Ekspres", new Vector3(14.45f,.94f,-.35f),90);
        Bottom("Socjalny_Radio", new Vector3(14.45f,.94f,-1.1f),90);
        MoveEgg("Spot_MugChoir", new Vector3(14.45f,.94f,-1.60f),90);
        Place("Socjalny_Stolek1",new Vector3(11.3f,.48f,-1.78f),0);
        Place("Socjalny_Stolek2",new Vector3(11.3f,.48f,-3.22f),180);
        Bottom("Socjalny_LampaStojaca",new Vector3(6.65f,.005f,.62f),0);
        ReplaceAction("B4 Personal Locker","StationStaffLocker",new Vector3(7.45f,.005f,-4.66f),180);
        Add("StationStaffLocker","SocialStorage",new Vector3(8.63f,.005f,-4.66f),180);
        MoveAction("B4 Quiet Plant", new Vector3(14.52f,.005f,.59f),0);

        Hide("Workshop_Desk", "WorkshopBoard", "WorkshopDrawers");
        Add("StationWorkbench","WorkshopBench",new Vector3(13.2f,.005f,7.59f));
        Bottom("WorkshopTools",new Vector3(12.5f,.96f,7.50f),0);
        ReplaceAction("B5 Maintenance Cabinet","StationStaffLocker",new Vector3(14.63f,.005f,2.55f),90);
        Add("StationArchiveRack","WorkshopStockA",new Vector3(6.68f,.005f,6.85f),270);
        Add("StationArchiveRack","WorkshopStockB",new Vector3(6.68f,.005f,2.30f),270);

        foreach (float z in new[] {-7.3f,-9.35f,-11.4f})
        {
            Add("StationArchiveRack","StorageWest_"+z,new Vector3(-5.61f,.005f,z),270);
            Add("StationArchiveRack","StorageEast_"+z,new Vector3(-.52f,.005f,z),90);
        }
        var cartons = station.GetComponentsInChildren<Transform>().Where(t=>t.name=="StorageCarton").ToArray();
        for (int i=0;i<cartons.Length;i++) cartons[i].position = new Vector3(-2.6f+(i%2)*.6f,i==2?.40f:.005f,-12.1f);

        Add("StationArchiveRack","ArchiveNorthA",new Vector3(-13.35f,.005f,.69f));
        Add("StationArchiveRack","ArchiveNorthB",new Vector3(-7.73f,.005f,.69f));
        Add("StationArchiveRack","ArchiveWest",new Vector3(-14.65f,.005f,-.35f),270);
        ReplaceAction("B4 Archive Alarm","StationAlarmPanel",new Vector3(-11.35f,1.45f,-4.89f),180);
        MountIndicators("B4 Archive Alarm",new Vector3(.10f,.02f,-.14f));
        MountIndicators("B5 Service Panel",new Vector3(0,.05f,-.16f));
        MountIndicators("B5 Vent Control",new Vector3(0,.05f,-.16f));
        MountIndicators("B5 Gate Control",new Vector3(0,.05f,-.16f));
        Place("Archiwum_Krzeslo",new Vector3(-10.05f,.57f,-3.12f),210.7f);
        Add("StationDeskPhone","ArchivePhone",new Vector3(-9.05f,1.145f,-2.90f),270,false);
        Place("Evidence_Bench",new Vector3(-8.05f,.52f,1.64f),180);

        Hide("Office_Desk","OfficeCabinetA","OfficeCabinetB");
        Add("StationOfficeDesk","OfficeWorkingDesk",new Vector3(-12.1f,.005f,12.75f));
        Place("Office_Chair",new Vector3(-12.1f,.57f,11.76f),120.7f);
        Bottom("OfficeTaskLamp",new Vector3(-12.8f,.77f,12.76f),155);
        Add("StationMonitor","OfficeDisplay",new Vector3(-12.1f,.77f,12.98f),0,false);
        Add("StationDeskPhone","OfficePhone",new Vector3(-11.5f,.77f,12.76f),0,false);
        Add("StationStaffLocker","OfficeStorageA",new Vector3(-14.35f,.005f,8.58f),180);
        Add("StationStaffLocker","OfficeStorageB",new Vector3(-13.17f,.005f,8.58f),180);
        Place("OfficeBoard",new Vector3(-6.34f,2.2f,13.1f),90);
        Place("Office_Sofa",new Vector3(-6.83f,.50f,11.95f),90);
        Place("Office_CoffeeTable",new Vector3(-8.7f,.30f,11.95f),90);

        Hide("Sala_StolW","Sala_StolE");
        Add("StationDiningTable","CommonTableWest",new Vector3(-2.8f,.005f,11.4f));
        Add("StationDiningTable","CommonTableEast",new Vector3(2.8f,.005f,11.4f));
        Place("Sala_KrzesloW1",new Vector3(-3.84f,.49f,11.4f),270);
        Place("Sala_KrzesloW2",new Vector3(-1.76f,.49f,11.4f),90);
        Place("Sala_KrzesloW3",new Vector3(-2.8f,.49f,12.12f),0);
        Place("Sala_KrzesloE1",new Vector3(1.76f,.49f,11.4f),270);
        Place("Sala_KrzesloE2",new Vector3(3.84f,.49f,11.4f),90);
        Place("Sala_Sofa",new Vector3(-3.6f,.50f,6.90f),180);
        Place("Sala_SofaE",new Vector3(3.6f,.48f,6.80f),180);
        Place("Sala_StolikKawowy",new Vector3(-3.6f,.30f,8.02f),0);
        Place("Sala_StolikKawowyE",new Vector3(3.6f,.30f,7.95f),0);
        Place("Sala_SzafkaTV",new Vector3(-5.66f,.33f,8.55f),270);
        Bottom("Sala_TV",new Vector3(-5.66f,.65f,8.55f),90);
        Place("Sala_Lawka",new Vector3(.6f,.52f,13.62f),0);
        GameObject.Find("SpawnPoint_NW").transform.position=new Vector3(-.6f,.04f,11.7f);
        FlattenRug("Sala_Dywan",new Vector3(-3.6f,.005f,7.75f));
        FlattenRug("Sala_DywanE",new Vector3(3.6f,.005f,7.75f));
        FlattenRug("Korytarz_Wycieraczka",new Vector3(0,.005f,-3.3f));
        ConfigureCommonLighting();
        Physics.SyncTransforms();
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("[StationRefinement] Room groups composed. Bake seats, finalize access, then run 18 Final surface alignment.");
    }

    [MenuItem("Tools/Interrogation Room/Station Rebuild/19 Balance common room lighting")]
    public static void ConfigureCommonLighting()
    {
        if (Application.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != "Assets/Scenes/Room.unity")
            throw new InvalidOperationException("Open Room in Edit Mode.");
        var station = GameObject.Find("Map_Station").transform;
        var lighting = station.Find("Lighting");
        var architecture = station.Find("BlenderArchitecture");
        for (int side=0; side<2; side++)
        {
            float x=side==0 ? -3f : 3f;
            var sourceLight=lighting.Find("Practical_common"+side).GetComponent<Light>();
            for (int row=0; row<2; row++)
            {
                float z=row==0 ? 11.55f : 7.9f;
                var light=sourceLight;
                if (row==1)
                {
                    string name="Practical_commonLounge"+side;
                    var existing=lighting.Find(name);
                    light=existing!=null ? existing.GetComponent<Light>() : Object.Instantiate(sourceLight,lighting);
                    light.name=name;
                }
                light.transform.SetPositionAndRotation(new Vector3(x,3.18f,z),Quaternion.Euler(90,0,0));
                light.intensity=32;
                light.spotAngle=150;
                light.innerSpotAngle=80;
                light.bounceIntensity=1.8f;
                light.shapeRadius=.3f;
                light.lightmapBakeType=LightmapBakeType.Baked;
                foreach (string material in new[]{"Metal","Diffuser"})
                {
                    var source=architecture.Find((side==0 ? "Shell_0_-2_" : "Shell_-1_-2_")+material);
                    var fixture=source;
                    if (row==1)
                    {
                        string name="CommonLoungeFixture_"+side+"_"+material;
                        fixture=architecture.Find(name);
                        if (fixture==null) fixture=Object.Instantiate(source,architecture);
                        fixture.name=name;
                    }
                    var center=fixture.GetComponent<Renderer>().bounds.center;
                    fixture.position+=new Vector3(x,center.y,z)-center;
                }
            }
        }
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }

    [MenuItem("Tools/Interrogation Room/Station Rebuild/18 Final surface alignment")]
    public static void FinalSurfaceAlignment()
    {
        Bottom("WeaponPickup",new Vector3(3.6f,.59f,7.95f),0);
        MountIndicators("B4 Archive Alarm",new Vector3(.10f,.02f,-.14f));
        MountIndicators("B5 Service Panel",new Vector3(0,.05f,-.16f));
        MountIndicators("B5 Vent Control",new Vector3(0,.05f,-.16f));
        MountIndicators("B5 Gate Control",new Vector3(0,.05f,-.16f));
        foreach (string name in new[] { "B4 Personal Locker", "B5 Maintenance Cabinet" })
        {
            var action = GameObject.Find(name).transform;
            foreach (var renderer in action.GetComponentsInChildren<Renderer>(true)
                .Where(r => r.name == "ScannedVisual" && r.transform.parent.name.StartsWith("PLACEHOLDER_")))
            {
                var bounds = renderer.bounds;
                var support = action.position + Vector3.up * 1.942f;
                renderer.transform.position += support - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            }
        }
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }

    private static GameObject Add(string model,string name,Vector3 position,float yaw=0,bool solid=true,Transform parent=null)
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+model+".fbx");
        if(prefab==null)throw new InvalidOperationException("Import "+model);
        var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent!=null?parent:root);
        go.transform.SetPositionAndRotation(position,Quaternion.Euler(0,yaw,0));
        foreach(var r in go.GetComponentsInChildren<MeshRenderer>())
        {
            string key=model=="StationDoorLining"?"Timber":model=="StationThreshold"?"Steel"
                :r.name.Substring(r.name.LastIndexOf('_')+1).Split('.')[0];
            r.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Folder+"Detail_"+key+".mat");
            if(r.sharedMaterial==null)throw new InvalidOperationException("Missing material for "+model+" / "+key);
            r.receiveGI=ReceiveGI.Lightmaps;
            GameObjectUtility.SetStaticEditorFlags(r.gameObject,StaticEditorFlags.ContributeGI|StaticEditorFlags.BatchingStatic|StaticEditorFlags.OccludeeStatic|StaticEditorFlags.ReflectionProbeStatic);
            if(solid){var c=r.gameObject.AddComponent<MeshCollider>();c.sharedMesh=r.GetComponent<MeshFilter>().sharedMesh;}
        }
        go.name=name;
        return go;
    }

    private static Bounds Bounds(GameObject go)
    {
        var rr=go.GetComponentsInChildren<Renderer>()
            .Where(r=>r.enabled && !(r is ParticleSystemRenderer) && r.bounds.size.sqrMagnitude>.000001f).ToArray();
        if(rr.Length==0)throw new InvalidOperationException("No visible renderer: "+go.name);
        var b=rr[0].bounds;foreach(var r in rr.Skip(1))b.Encapsulate(r.bounds);return b;
    }
    private static void Place(string name,Vector3 center,float yaw)
    {var go=GameObject.Find(name);go.transform.rotation=Quaternion.Euler(0,yaw,0);go.transform.position+=center-Bounds(go).center;}
    private static void Bottom(string name,Vector3 bottom,float yaw)
    {var go=GameObject.Find(name);go.transform.rotation=Quaternion.Euler(0,yaw,0);var b=Bounds(go);go.transform.position+=bottom-new Vector3(b.center.x,b.min.y,b.center.z);}
    private static void Hide(params string[] names)
    {foreach(string name in names){var go=GameObject.Find(name);if(go!=null)go.SetActive(false);}}
    private static void MoveAction(string name,Vector3 position,float yaw)
    {GameObject.Find(name).transform.SetPositionAndRotation(position,Quaternion.Euler(0,yaw,0));}
    private static void MoveEgg(string name,Vector3 position,float yaw)
    {GameObject.Find(name).transform.SetPositionAndRotation(position,Quaternion.Euler(0,yaw,0));}

    private static void ReplaceAction(string name,string model,Vector3 position,float yaw)
    {
        var action=GameObject.Find(name).transform;
        action.SetPositionAndRotation(position,Quaternion.Euler(0,yaw,0));
        var visual=action.Find("VisualRoot");
        foreach(var child in visual.Cast<Transform>().ToArray())
            if(child.name=="RefinedVisual")Object.DestroyImmediate(child.gameObject);else child.gameObject.SetActive(false);
        visual.SetLocalPositionAndRotation(Vector3.zero,Quaternion.identity);visual.localScale=Vector3.one;
        var go=Add(model,"RefinedVisual",position,yaw,false,visual);
        var rr=go.GetComponentsInChildren<MeshFilter>();
        var points=rr.SelectMany(f=>new[]{new Vector3(-1,-1,-1),new Vector3(-1,-1,1),new Vector3(-1,1,-1),new Vector3(-1,1,1),new Vector3(1,-1,-1),new Vector3(1,-1,1),new Vector3(1,1,-1),new Vector3(1,1,1)}.Select(s=>action.InverseTransformPoint(f.transform.TransformPoint(f.sharedMesh.bounds.center+Vector3.Scale(f.sharedMesh.bounds.extents,s))))).ToArray();
        var b=new Bounds(points[0],Vector3.zero);foreach(var p in points)b.Encapsulate(p);
        var collider=action.GetComponent<BoxCollider>();collider.center=b.center;collider.size=b.size;collider.enabled=true;
        action.Find("InteractionPoint").localPosition=b.center;
    }

    private static void MountIndicators(string name,Vector3 localPosition)
    {
        var action=GameObject.Find(name).transform;
        int i=0;
        foreach(var t in action.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="StateIndicator"))
        {
            t.SetPositionAndRotation(action.TransformPoint(localPosition+Vector3.right*(i++*.09f)),action.rotation*Quaternion.Euler(0,180,0));
            var inherited=t.parent.lossyScale;
            t.localScale=new Vector3(.45f/inherited.x,.45f/inherited.y,.45f/inherited.z);
        }
    }

    private static void FlattenRug(string name,Vector3 center)
    {
        var go=GameObject.Find(name);var scale=go.transform.localScale;
        scale.y*=.006f/Bounds(go).size.y;go.transform.localScale=scale;
        Place(name,center,0);
        foreach(var r in go.GetComponentsInChildren<Renderer>())r.shadowCastingMode=ShadowCastingMode.Off;
        foreach(var c in go.GetComponentsInChildren<Collider>())c.enabled=false;
    }
}
