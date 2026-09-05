using System;
using System.Collections.Generic;
using System.Linq;
using InterrogationRoom.Gameplay.Interaction;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>Repeatable Editor-only furnishings pass; preserves gameplay identities and authored action IDs.</summary>
public static class StationInteriorPolish
{
    private const string Folder = "Assets/Art/Environment/StationRebuild/";
    private static Transform details;
    private static Dictionary<string, Material> materials;
    [Serializable] private sealed class Layout { public Window[] windows; }
    [Serializable] private sealed class Window { public float x, z; public string axis; }

    [MenuItem("Tools/Interrogation Room/Station Rebuild/6 Polish interiors")]
    public static void Apply()
    {
        if (Application.isPlaying || SceneManager.GetActiveScene().path != "Assets/Scenes/Room.unity")
            throw new InvalidOperationException("Open the rebuilt Room scene in Edit Mode.");
        var station = GameObject.Find("Map_Station");
        if (station == null) throw new InvalidOperationException("Compose the station first.");
        materials = MakeMaterials();
        Transform previous = station.transform.Find("InteriorDetails");
        if (previous != null) Object.DestroyImmediate(previous.gameObject);
        details = new GameObject("InteriorDetails").transform;
        details.SetParent(station.transform, false);

        // Keep all gameplay components, replacing only the old table's presentation.
        GameObject table = GameObject.Find("Przesluchania_Stol");
        foreach (Renderer r in table.GetComponentsInChildren<Renderer>()) r.enabled = false;
        foreach (Collider c in table.GetComponentsInChildren<Collider>()) c.enabled = false;
        Add("InterviewTable", "InterviewTable", new Vector3(0,.01f,1.25f));
        station.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Przesluchania_Laptop").gameObject.SetActive(false);
        ReplaceInterviewChair("Przesluchania_KrzesloN");
        ReplaceInterviewChair("Przesluchania_KrzesloS");
        PlaceBounds("Przesluchania_KrzesloN", new Vector3(0,.49f,2.3f), 0);
        PlaceBounds("Przesluchania_KrzesloS", new Vector3(0,.49f,.2f), 180);
        Add("WallClock", "InterviewClock", new Vector3(0,2.35f,2.95f));
        Add("NoticeBoard", "InterviewProcedures", new Vector3(2.94f,1.95f,.6f),90);

        // Desk groups face into their rooms; room transitions remain clear.
        PlaceBounds("Office_Desk", new Vector3(-12.8f,.58f,12.3f),180);
        PlaceBounds("Office_Chair", new Vector3(-12.8f,.57f,10.95f),0);
        PlaceBounds("Office_Monitor", new Vector3(-12.8f,1.08f,12.55f),180);
        PlaceBounds("Office_Sofa", new Vector3(-7.2f,.5f,12.1f),90);
        PlaceBounds("Office_CoffeeTable", new Vector3(-9.1f,.3f,12.1f),90);
        SpaceLoungeFurniture();
        AlignWaitingBenches();
        Add("NoticeBoard","OfficeBoard",new Vector3(-13f,1.9f,8.3f),180);
        Add("FileCabinet","OfficeCabinetA",new Vector3(-14.5f,.01f,13.3f),270);
        Add("FileCabinet","OfficeCabinetB",new Vector3(-14.5f,.01f,9f),270);
        Add("WallClock","OfficeClock",new Vector3(-9.2f,2.4f,13.95f));

        PlaceBounds("Briefing_TableA",new Vector3(10.2f,.39f,11.2f),90);
        PlaceBounds("Briefing_TableB",new Vector3(11.8f,.39f,11.2f),90);
        PlaceBounds("Briefing_ChairA",new Vector3(9.3f,.49f,10.15f),180);
        PlaceBounds("Briefing_ChairB",new Vector3(11.9f,.49f,10.05f),180);
        CopySeat("Sala_KrzesloW3","Briefing_ChairC",new Vector3(9.9f,.49f,12.35f),0);
        CopySeat("Sala_KrzesloW3","Briefing_ChairD",new Vector3(11.9f,.49f,12.35f),0);
        GameObject.Find("SpawnPoint_W").transform.position=new Vector3(-.5f,.04f,9.6f);
        GameObject.Find("SpawnPoint_E").transform.position=new Vector3(.5f,.04f,9.6f);
        Add("NoticeBoard","BriefingBoard",new Vector3(13,2,8.3f),180);
        Add("WallClock","BriefingClock",new Vector3(7.7f,2.35f,13.93f));

        // A real reception desk and waiting area, with both sides accessible.
        Add("ReceptionCounter","ReceptionCounter",new Vector3(2.1f,.01f,-9.5f),180);
        Add("ReceiptTray","ReceptionForms",new Vector3(2.8f,1.075f,-9.5f),180,false);
        PlaceBounds("Reception_Bench",new Vector3(5.35f,.52f,-8.2f),90);
        Add("NoticeBoard","ReceptionNotices",new Vector3(.2f,1.95f,-8),270);
        Add("WallClock","ReceptionClock",new Vector3(.2f,2.4f,-10.9f),270);
        Label(details,"ReceptionTitle","RECEPCJA",new Vector3(3,2.7f,-12.91f),180,1.9f);

        // Existing cabinets/appliances form a wall run instead of a barrier across the kitchen.
        PlaceBounds("Socjalny_Szafka1",new Vector3(14.45f,.47f,-1.4f),90);
        PlaceBounds("Socjalny_Szafka2",new Vector3(14.45f,.47f,-2.3f),90);
        PlaceBounds("Socjalny_Ekspres",new Vector3(14.4f,1.10f,-1.4f),90);
        PlaceBounds("Socjalny_Radio",new Vector3(14.4f,1.17f,-2.3f),90);
        PlaceBounds("Socjalny_Lodowka",new Vector3(14.45f,.62f,-3.45f),90);
        PlaceBounds("Socjalny_Stol",new Vector3(11.4f,.33f,-2.5f),0);
        ReplaceInterviewChair("Socjalny_Stolek1");
        ReplaceInterviewChair("Socjalny_Stolek2");
        PlaceBounds("Socjalny_Stolek1",new Vector3(11.4f,.49f,-1.35f),0);
        PlaceBounds("Socjalny_Stolek2",new Vector3(11.4f,.49f,-3.65f),180);
        Add("NoticeBoard","SocialBoard",new Vector3(8.4f,1.9f,-4.91f),180);
        Add("FileCabinet","SocialLocker",new Vector3(7,.01f,-4.3f),180);

        PlaceBounds("Workshop_Desk",new Vector3(7.15f,.58f,6.6f),90);
        Add("NoticeBoard","WorkshopBoard",new Vector3(6.32f,1.95f,6.6f),270);
        Add("FileCabinet","WorkshopDrawers",new Vector3(14.4f,.01f,5),90);
        Label(details,"EvidenceTitle","DEPOZYT DOWODOW",new Vector3(-12.8f,2.55f,7.91f),0,1.5f);
        Add("NoticeBoard","EvidenceRegister",new Vector3(-6.33f,1.95f,6.5f),90);

        // Remap the original tasks to visible, purpose-built props. No task definitions change.
        Action("B4 Records Cabinet","FileCabinet",new Vector3(-14.5f,.01f,-3.55f),270,new Vector3(0,.95f,-.32f));
        MoveAction("B4 Evidence Shelf",new Vector3(-14.5f,.01f,7.1f),270);
        MoveAction("B4 Archive Slot",new Vector3(-12.6f,.01f,-4.45f),0);
        MoveAction("B4 Suspicious Item",new Vector3(-8,.01f,-3.5f),0);
        Action("B4 Personal Locker","FileCabinet",new Vector3(7,.01f,-2.5f),270,new Vector3(0,.95f,-.32f));
        MoveAction("B4 Quiet Plant",new Vector3(8,.01f,.2f),0);
        MoveAction("B4 Archive Alarm",new Vector3(-11,.01f,-4.4f),0);
        Action("B5 Receipt Clue","ReceiptTray",new Vector3(-9.15f,.82f,-3.65f),0,new Vector3(0,.09f,-.08f));
        Action("B5 Maintenance Cabinet","FileCabinet",new Vector3(14.5f,.01f,2.2f),90,new Vector3(0,.95f,-.32f));
        Action("B5 Service Panel","UtilityPanel",new Vector3(14.86f,1.45f,7.2f),90,new Vector3(0,-.1f,-.15f));
        Action("B5 Vent Control","UtilityPanel",new Vector3(-5.86f,1.45f,12.05f),270,new Vector3(0,-.1f,-.15f));
        Action("B5 Gate Control","UtilityPanel",new Vector3(5.86f,1.45f,12.05f),90,new Vector3(0,-.1f,-.15f));
        Action("B5 Final Exit A","ServiceVent",new Vector3(-4.7f,1.15f,-12.9f),180,new Vector3(0,0,-.15f));
        Action("B5 Final Exit B","ServiceExit",new Vector3(4.7f,.01f,-12.9f),180,new Vector3(0,1.06f,-.2f));
        Label(details,"ExitA","WENTYLACJA / WYLOT",new Vector3(-4.7f,1.9f,-12.83f),180,1.0f);
        Label(details,"ExitB","WYJSCIE SLUZBOWE",new Vector3(4.7f,2.55f,-12.83f),180,1.0f);
        GameObject.Find("GameplayItem_PersonalDocument").transform.position=new Vector3(-14.5f,1.55f,-3.55f);
        GameObject.Find("RoundPhysicalIntegration/GameplayItem_SuspiciousToken").transform.position=new Vector3(-8,.64f,-3.5f);
        var document=GameObject.Find("GameplayItem_PersonalDocument").GetComponent<Renderer>();
        document.sharedMaterial=materials["Paper"];
        GameObject.Find("RoundPhysicalIntegration/GameplayItem_SuspiciousToken").GetComponent<Renderer>().sharedMaterial=materials["Red"];

        Egg("Spot_Typewriter","Typewriter",new Vector3(-9.1f,.82f,-2.7f),90);
        Egg("Spot_MugChoir","MugSet",new Vector3(14.4f,.935f,-1.9f),90);
        Egg("Spot_PigeonInspector","Pigeon",new Vector3(-14.88f,1.395f,-2.4f),270);
        Egg("Spot_IntercomForecast","Intercom",new Vector3(3.29f,1.5f,1.9f),270);

        // Window blinds soften the bright panes and make their scale and construction readable.
        var layout=JsonUtility.FromJson<Layout>(AssetDatabase.LoadAssetAtPath<TextAsset>(Folder+"layout.json").text);
        int index=0;
        foreach(var w in layout.windows)
        {
            Vector3 inward=w.axis=="z"?new Vector3(-Mathf.Sign(w.x),0,0):new Vector3(0,0,-Mathf.Sign(w.z));
            float yaw=Quaternion.LookRotation(-inward).eulerAngles.y;
            Vector3 p=new Vector3(w.x,2.1f,w.z)+inward*.19f;
            Add("WindowBlind","Blind_"+index,p,yaw,false);
            // A radiator below each window stays outside the central walking routes.
            Add("Radiator","Radiator_"+index,new Vector3(w.x,.01f,w.z)+inward*.28f,yaw);
            index++;
        }
        StyleSigns(station.transform);
        UpdateWayfinding();
        RefineLighting(station.transform);
        Physics.SyncTransforms();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log("[StationPolish] Interior props, task visuals, placement, signs and lighting updated. Bake lighting and validate traversal next.");
    }

    private static Dictionary<string,Material> MakeMaterials()
    {
        var palette=new Dictionary<string,Color> {
            {"Paint",new Color(.28f,.34f,.32f)},{"Steel",new Color(.23f,.26f,.26f)},
            {"Timber",new Color(.36f,.25f,.15f)},{"Paper",new Color(.72f,.69f,.6f)},
            {"Cork",new Color(.34f,.24f,.15f)},{"Ink",new Color(.055f,.065f,.065f)},
            {"Red",new Color(.43f,.095f,.06f)},{"Ceramic",new Color(.63f,.66f,.61f)},
            {"Feather",new Color(.26f,.30f,.33f)}};
        var result=new Dictionary<string,Material>();
        foreach(var pair in palette)
        {
            string path=Folder+"Detail_"+pair.Key+".mat";
            Material m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
            m.SetColor("_BaseColor",pair.Value);m.SetFloat("_Smoothness",pair.Key=="Steel"?.36f:.18f);
            m.SetFloat("_Metallic",pair.Key=="Steel"?.55f:0);
            m.enableInstancing=true;EditorUtility.SetDirty(m);result[pair.Key]=m;
        }
        return result;
    }

    private static GameObject Model(string model,Transform parent)
    {
        var asset=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+model+".fbx");
        if(asset==null)throw new InvalidOperationException("Missing Blender model "+model);
        var go=(GameObject)PrefabUtility.InstantiatePrefab(asset,parent);
        go.name="Authored_"+model;
        foreach(var renderer in go.GetComponentsInChildren<Renderer>())
        {
            string key=renderer.name.Substring(renderer.name.LastIndexOf('_')+1).Split('.')[0];
            renderer.sharedMaterials=Enumerable.Repeat(materials[key],renderer.sharedMaterials.Length).ToArray();
        }
        return go;
    }

    private static GameObject Add(string model,string name,Vector3 position,float yaw=0,bool solid=true)
    {
        var go=Model(model,details);go.name=name;go.transform.SetPositionAndRotation(position,Quaternion.Euler(0,yaw,0));
        foreach(var renderer in go.GetComponentsInChildren<MeshRenderer>())
        {
            GameObjectUtility.SetStaticEditorFlags(renderer.gameObject,StaticEditorFlags.ContributeGI|StaticEditorFlags.BatchingStatic|StaticEditorFlags.OccluderStatic|StaticEditorFlags.OccludeeStatic|StaticEditorFlags.ReflectionProbeStatic);
            renderer.receiveGI=ReceiveGI.Lightmaps;
            if(solid){var c=renderer.gameObject.AddComponent<MeshCollider>();c.sharedMesh=renderer.GetComponent<MeshFilter>().sharedMesh;}
        }
        return go;
    }

    private static void Action(string name,string model,Vector3 position,float yaw,Vector3 interaction)
    {
        Transform root=GameObject.Find(name).transform;
        root.SetPositionAndRotation(position,Quaternion.Euler(0,yaw,0));
        Transform visual=root.Find("VisualRoot");
        foreach(Transform child in visual.Cast<Transform>().ToArray())
            if(child.name.StartsWith("Authored_"))Object.DestroyImmediate(child.gameObject);else child.gameObject.SetActive(false);
        var go=Model(model,visual);
        FitCollider(root.gameObject,go);
        root.Find("InteractionPoint").localPosition=interaction;
    }

    private static void MoveAction(string name,Vector3 position,float yaw)
    {
        var root=GameObject.Find(name);root.transform.SetPositionAndRotation(position,Quaternion.Euler(0,yaw,0));
        var visual=root.transform.Find("VisualRoot");
        foreach(var r in visual.GetComponentsInChildren<Renderer>())if(r.name.StartsWith("PLACEHOLDER"))r.enabled=false;
        FitCollider(root,visual.gameObject);
        Bounds b=BoundsOf(visual.gameObject);
        root.transform.Find("InteractionPoint").position=b.center;
    }

    private static Bounds BoundsOf(GameObject go)
    {
        var rr=go.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).ToArray();
        if(rr.Length==0)throw new InvalidOperationException("No visible model on "+go.name);
        Bounds b=rr[0].bounds;foreach(var r in rr)b.Encapsulate(r.bounds);return b;
    }

    private static void FitCollider(GameObject root,GameObject visual)
    {
        // Compute bounds in the target's local space, even for a rotated wall control.
        var points=visual.GetComponentsInChildren<MeshFilter>().Where(f=>f.GetComponent<Renderer>().enabled && f.gameObject.activeInHierarchy)
            .SelectMany(f=>new[]{f.sharedMesh.bounds.min,f.sharedMesh.bounds.max}.Select(v=>root.transform.InverseTransformPoint(f.transform.TransformPoint(v)))).ToArray();
        Bounds b=new Bounds(points[0],Vector3.zero);foreach(var p in points)b.Encapsulate(p);
        var c=root.GetComponent<BoxCollider>();
        if(c==null)c=root.AddComponent<BoxCollider>();
        c.center=b.center;c.size=Vector3.Max(b.size,Vector3.one*.04f);c.enabled=true;
    }

    private static void Egg(string name,string model,Vector3 position,float yaw)
    {
        var root=GameObject.Find(name);root.transform.SetPositionAndRotation(position,Quaternion.Euler(0,yaw,0));
        Transform prop=root.transform.Find("PropRoot");prop.gameObject.SetActive(true);
        foreach(string state in new[]{"DormantVisual","TriggeredVisual"})
        {
            Transform visual=prop.Find(state);visual.gameObject.SetActive(true);
            foreach(Transform child in visual.Cast<Transform>().ToArray())
                if(child.name.StartsWith("Authored_"))Object.DestroyImmediate(child.gameObject);else child.gameObject.SetActive(false);
            if(visual.GetComponent<Renderer>()!=null)visual.GetComponent<Renderer>().enabled=false;
            foreach(var c in visual.GetComponents<Collider>())c.enabled=false;
            visual.SetLocalPositionAndRotation(Vector3.zero,Quaternion.identity);visual.localScale=Vector3.one;
            var go=Model(model,visual);FitCollider(go,go);
            if(state=="TriggeredVisual")go.transform.localRotation=Quaternion.Euler(0,12,0);
        }
        root.GetComponent<BoxCollider>().enabled=false;
        prop.Find("TriggeredVisual").gameObject.SetActive(false);
    }

    private static void PlaceBounds(string name,Vector3 center,float yaw)
    {
        var go=GameObject.Find(name);go.transform.rotation=Quaternion.Euler(0,yaw,0);
        if(!go.GetComponentsInChildren<Renderer>().Any(r=>r.enabled))return;
        go.transform.position+=center-BoundsOf(go).center;
    }

    private static void CopySeat(string source,string name,Vector3 center,float yaw)
    {
        var go=Object.Instantiate(GameObject.Find(source),details);go.name=name;PlaceBounds(name,center,yaw);
        // Serialized edits call Mirror's OnValidate, which owns scene ID allocation.
        var id=go.GetComponent<Mirror.NetworkIdentity>();
        var serialized=new SerializedObject(id);
        serialized.FindProperty("sceneId").ulongValue=0;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ReplaceInterviewChair(string name)
    {
        var target=GameObject.Find(name);
        var old=target.transform.Find("Authored_Chair");
        if(old!=null)Object.DestroyImmediate(old.gameObject);
        foreach(var r in target.GetComponentsInChildren<Renderer>())
        {
            var filter=r.GetComponent<MeshFilter>();
            if(filter!=null)Object.DestroyImmediate(filter);
            Object.DestroyImmediate(r);
        }
        var source=GameObject.Find("Sala_KrzesloW3").GetComponentInChildren<MeshFilter>();
        var visual=new GameObject("Authored_Chair");visual.transform.SetParent(target.transform,false);
        visual.transform.localPosition=source.transform.localPosition;
        visual.transform.localRotation=source.transform.localRotation;
        visual.transform.localScale=source.transform.localScale;
        visual.AddComponent<MeshFilter>().sharedMesh=source.sharedMesh;
        visual.AddComponent<MeshRenderer>().sharedMaterials=source.GetComponent<Renderer>().sharedMaterials;
        FitCollider(target,visual);
    }

    [MenuItem("Tools/Interrogation Room/Station Rebuild/7 Finalize player access")]
    public static void FinalizeAccess()
    {
        if(Application.isPlaying || SceneManager.GetActiveScene().path!="Assets/Scenes/Room.unity")
            throw new InvalidOperationException("Finalize Room in Edit Mode, after baking chairs.");
        GameObject.Find("WeaponPickup").transform.position=new Vector3(1.5f,.65f,7.55f);
        Physics.SyncTransforms();
        int configured=0;
        foreach(var seat in Object.FindObjectsByType<NetworkChairSeat>(FindObjectsSortMode.None))
        {
            Transform furniture=seat.transform;
            while(furniture.parent!=null && furniture.parent.name!="Meble" && furniture.parent.name!="InteriorDetails")furniture=furniture.parent;
            Vector3 position=seat.SeatPosition;
            Vector3 chosen=Vector3.zero;bool found=false;
            foreach(float radius in new[]{1.0f,1.25f,1.5f,1.75f})
            {
                foreach(float angle in new[]{0f,45f,-45f,90f,-90f,135f,-135f,180f})
                {
                    Vector3 p=position+seat.SeatRotation*Quaternion.Euler(0,angle,0)*Vector3.forward*radius;
                    if(Physics.CheckCapsule(p+Vector3.up*.5f,p+Vector3.up*1.4f,.44f,~0,QueryTriggerInteraction.Ignore))continue;
                    if(!Physics.Raycast(p+Vector3.up*.3f,Vector3.down,out RaycastHit floor,.5f,~0,QueryTriggerInteraction.Ignore)||Math.Abs(floor.point.y)>.12f)continue;
                    Vector3 delta=position-p;
                    if(Physics.RaycastAll(p+Vector3.up*.8f,delta.normalized,delta.magnitude,~0,QueryTriggerInteraction.Ignore)
                        .Any(h=>!h.collider.transform.IsChildOf(furniture)))continue;
                    Vector3 sight=seat.InteractionPosition-(p+Vector3.up*1.55f);
                    var first=Physics.RaycastAll(p+Vector3.up*1.55f,sight.normalized,sight.magnitude+.05f,~0,QueryTriggerInteraction.Collide)
                        .OrderBy(h=>h.distance).FirstOrDefault();
                    if(first.collider!=null && first.collider.GetComponentInParent<Mirror.NetworkIdentity>()!=seat.GetComponent<Mirror.NetworkIdentity>())continue;
                    chosen=p;found=true;break;
                }
                if(found)break;
            }
            if(!found)throw new InvalidOperationException("No safe stand-up space for "+seat.name+" under "+furniture.name);
            Transform anchor=seat.transform.Find("StationStandPoint");
            if(anchor==null){anchor=new GameObject("StationStandPoint").transform;anchor.SetParent(seat.transform);}
            anchor.position=chosen;anchor.rotation=seat.SeatRotation;
            var so=new SerializedObject(seat);so.FindProperty("standPoint").objectReferenceValue=anchor;so.ApplyModifiedPropertiesWithoutUndo();configured++;
        }
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log("[StationPolish] Safe standing anchors configured for "+configured+" seats; weapon raised above table surface.");
    }

    [MenuItem("Tools/Interrogation Room/Station Rebuild/9 Align waiting benches")]
    public static void AlignWaitingBenches()
    {
        if(Application.isPlaying || SceneManager.GetActiveScene().path!="Assets/Scenes/Room.unity")
            throw new InvalidOperationException("Align benches in Room Edit Mode.");
        PlaceBounds("Korytarz_LawkaW",new Vector3(-2.1f,.52f,3.85f),180);
        PlaceBounds("Korytarz_LawkaE",new Vector3(1f,.52f,3.85f),180);
        PlaceBounds("Evidence_Bench",new Vector3(-8f,.52f,2.2f),180);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    [MenuItem("Tools/Interrogation Room/Station Rebuild/10 Space lounge furniture")]
    public static void SpaceLoungeFurniture()
    {
        if(Application.isPlaying || SceneManager.GetActiveScene().path!="Assets/Scenes/Room.unity")
            throw new InvalidOperationException("Space furniture in Room Edit Mode.");
        PlaceBounds("Sala_Sofa",new Vector3(-3f,.5f,7.5f),90);
        PlaceBounds("Sala_StolikKawowy",new Vector3(-4.9f,.3f,7.5f),90);
        PlaceBounds("Office_CoffeeTable",new Vector3(-9.1f,.3f,12.1f),90);
        PlaceBounds("Sala_SofaE",new Vector3(3.5f,.48f,7.55f),90);
        PlaceBounds("Sala_StolikKawowyE",new Vector3(1.5f,.3f,7.55f),90);
        GameObject.Find("SpawnPoint_SE").transform.position=new Vector3(.4f,.04f,7.45f);
        GameObject.Find("SpawnPoint_SW").transform.position=new Vector3(-.4f,.04f,7.45f);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    private static void Label(Transform parent,string name,string value,Vector3 position,float yaw,float size)
    {
        var go=new GameObject(name);go.transform.SetParent(parent);go.transform.SetPositionAndRotation(position,Quaternion.Euler(0,yaw,0));
        var t=go.AddComponent<TextMeshPro>();t.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Folder+"StationLabels.asset");
        t.text=value;t.fontSize=size;t.alignment=TextAlignmentOptions.Center;t.color=new Color(.18f,.22f,.2f);
        t.rectTransform.sizeDelta=new Vector2(3,.4f);
    }

    private static void StyleSigns(Transform station)
    {
        foreach(Transform sign in station.Cast<Transform>().Where(t=>t.name.StartsWith("Sign_")))
        {
            foreach(var t in sign.GetComponentsInChildren<TextMeshPro>()){t.fontSize=1.4f;t.color=new Color(.83f,.8f,.68f);}
            var plate=GameObject.CreatePrimitive(PrimitiveType.Cube);plate.name=sign.name+"_Plate";plate.transform.SetParent(details);
            plate.transform.SetPositionAndRotation(sign.position,sign.rotation);plate.transform.localScale=new Vector3(2.8f,.34f,.30f);
            plate.GetComponent<Renderer>().sharedMaterial=materials["Paint"];Object.DestroyImmediate(plate.GetComponent<Collider>());
        }
    }

    [MenuItem("Tools/Interrogation Room/Station Rebuild/8 Update wayfinding")]
    public static void UpdateWayfinding()
    {
        if(Application.isPlaying || SceneManager.GetActiveScene().path!="Assets/Scenes/Room.unity")
            throw new InvalidOperationException("Update signs in the Room scene in Edit Mode.");
        var labels=new Dictionary<string,string>{
            {"korytarz","KORYTARZ CENTRALNY"},{"pokoj-przesluchan","01  PRZESLUCHANIA"},
            {"sala-wspolna","02  SALA WSPOLNA"},{"archiwum","03  ARCHIWUM"},
            {"dowody","04  DEPOZYT"},{"biuro","05  BIURO"},{"pokoj-socjalny","06  SOCJALNY"},
            {"warsztat","07  WARSZTAT"},{"odprawy","08  ODPRAWY"},{"magazyn","09  MAGAZYN"},{"recepcja","10  RECEPCJA"}};
        var rooms=Object.FindObjectsByType<RoomVolume>(FindObjectsSortMode.None);
        var station=GameObject.Find("Map_Station");
        foreach(Transform sign in station.transform.Cast<Transform>().Where(t=>t.name.StartsWith("Sign_")))
        {
            var door=station.GetComponentsInChildren<NetworkDoor>().First(d=>d.name==sign.name.Substring(5));
            foreach(var text in sign.GetComponentsInChildren<TextMeshPro>())
            {
                float side=Mathf.Sign(text.transform.localPosition.z);
                Vector3 destination=door.transform.position-door.transform.forward*side*.7f;
                var room=rooms.FirstOrDefault(r=>r.Contains(destination));
                if(room==null)throw new InvalidOperationException("No destination room for sign "+sign.name);
                text.text=labels[room.RoomId];EditorUtility.SetDirty(text);
            }
        }
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    private static void RefineLighting(Transform station)
    {
        var glass=AssetDatabase.LoadAssetAtPath<Material>(Folder+"Glass.mat");
        glass.SetColor("_BaseColor",new Color(.42f,.53f,.56f));glass.SetColor("_EmissionColor",new Color(.18f,.25f,.28f));EditorUtility.SetDirty(glass);
        var oak=AssetDatabase.LoadAssetAtPath<Material>(Folder+"Oak.mat");
        oak.SetFloat("_BumpScale",0);oak.SetColor("_BaseColor",new Color(.38f,.29f,.20f));EditorUtility.SetDirty(oak);
        foreach(var light in station.GetComponentsInChildren<Light>())
        {
            light.shadowBias=.07f;light.shadowNormalBias=.35f;
            if(light.name.StartsWith("Practical_"))light.intensity=light.name.Contains("interrogation")?12:light.name.Contains("hall")?16:32;
            if(light.name=="Daylight")light.intensity=5;
        }
        var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(Folder+"StationPostFX.asset");
        if(profile.TryGet<ColorAdjustments>(out var color))color.postExposure.value=.35f;
        EditorUtility.SetDirty(profile);
        // Increase spatial resolution to avoid broad seams around wall trim and small props.
        Lightmapping.lightingSettings.lightmapResolution=20;
        Lightmapping.lightingSettings.directSampleCount=64;
        Lightmapping.lightingSettings.indirectSampleCount=192;
        EditorUtility.SetDirty(Lightmapping.lightingSettings);
    }
}
