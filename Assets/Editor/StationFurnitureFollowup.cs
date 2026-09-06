using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class StationFurnitureFollowup
{
    [MenuItem("Tools/Interrogation Room/Station Rebuild/32 Correct furniture placement")]
    public static void Apply()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (Application.isPlaying || scene.path != "Assets/Scenes/Room.unity")
            throw new InvalidOperationException("Open Room in Edit Mode.");

        // Visibility data no longer matches the relocated furniture. Rebuild it
        // after layout approval; clearing it does not bake or clear lighting.
        StaticOcclusionCulling.Clear();
        foreach (var table in GameObject.Find("Map_Station/RepeatedFixturesPolish").GetComponentsInChildren<Transform>()
                     .Where(t => t.name == "RefinedBriefingTable").ToArray())
            UnityEngine.Object.DestroyImmediate(table.gameObject);
        foreach (string name in new[] { "Briefing_TableA", "Briefing_TableB" })
            foreach (var r in GameObject.Find(name).GetComponentsInChildren<Renderer>()) r.enabled = true;
        Center("Briefing_TableA", new Vector3(10.2f,.39f,11.55f));
        Center("Briefing_TableB", new Vector3(11.8f,.39f,11.55f));
        Center("Briefing_ChairA", new Vector3(10.2f,.49f,10.60f));
        Center("Briefing_ChairB", new Vector3(11.8f,.49f,10.60f));
        Center("Briefing_ChairC", new Vector3(10.2f,.49f,12.50f));
        Center("Briefing_ChairD", new Vector3(11.8f,.49f,12.50f));
        Stand("Briefing_ChairA", new Vector3(8.9f,.02f,10.60f));
        Stand("Briefing_ChairB", new Vector3(13.1f,.02f,10.60f));
        Stand("Briefing_ChairC", new Vector3(8.9f,.02f,12.50f));
        Stand("Briefing_ChairD", new Vector3(13.1f,.02f,12.50f));
        Center("Przesluchania_KrzesloN", new Vector3(0,.49f,2.12f));
        Center("Przesluchania_KrzesloS", new Vector3(0,.49f,.38f));
        Center("Korytarz_Wycieraczka", new Vector3(0,.006f,-2.50f));

        var desk = GameObject.Find("Archiwum_Biurko");
        var deskRenderer = desk.transform.Find("Visual_Desk_Wood").GetComponent<Renderer>();
        var shift = new Vector3(0,0,-4.10f-deskRenderer.bounds.center.z);
        foreach (string name in new[] { "Archiwum_Biurko", "Archiwum_Monitor", "Archiwum_Klawiatura", "Archiwum_Kosz", "Spot_Typewriter", "B5 Receipt Clue", "ArchiveDetailedPhone" })
            GameObject.Find(name).transform.position += shift;
        Center("Archiwum_Krzeslo", new Vector3(-10.1f,.57f,-4.0f));
        Stand("Archiwum_Krzeslo", new Vector3(-10.4f,.02f,-2.65f));
        var side = GameObject.Find("B4 Suspicious Item");
        var top = side.GetComponentsInChildren<Renderer>().First(r=>r.name=="Visual_CoffeeTable_DarkWood");
        side.transform.position += new Vector3(-7.65f-top.bounds.center.x,0,-4.48f-top.bounds.center.z);
        var token = GameObject.Find("GameplayItem_SuspiciousToken").transform;
        token.position = new Vector3(-7.65f,token.position.y,-4.48f);
        PrefabUtility.RecordPrefabInstancePropertyModifications(token);

        foreach (string name in new[] { "B5 Vent Control", "B5 Gate Control" })
        {
            var root = GameObject.Find(name);
            var scan = root.GetComponentsInChildren<Renderer>().First(r => r.name == "ScannedVisual").transform;
            // Imported open cabinet faces local +Z after this correction.
            scan.localRotation = Quaternion.Euler(0,180,0);
            var mesh = scan.GetComponent<MeshFilter>().sharedMesh.bounds;
            var matrix = Matrix4x4.TRS(scan.position,scan.rotation,scan.lossyScale);
            var b = new Bounds(matrix.MultiplyPoint3x4(mesh.center), Vector3.zero);
            for (int i=0;i<8;i++)
                b.Encapsulate(matrix.MultiplyPoint3x4(mesh.center+Vector3.Scale(mesh.extents,
                    new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1))));
            float dx = name == "B5 Vent Control" ? -5.94f-b.min.x : 5.94f-b.max.x;
            scan.position += Vector3.right * dx;
            PrefabUtility.RecordPrefabInstancePropertyModifications(scan);
            EditorUtility.SetDirty(scan);
            var box = root.GetComponent<BoxCollider>();
            if (box != null)
            {
                box.center = root.transform.InverseTransformPoint(b.center+Vector3.right*dx);
                box.size = new Vector3(b.size.z,b.size.y,b.size.x);
                EditorUtility.SetDirty(box);
            }
        }
        const string chairPath = "Assets/Art/Environment/StationRebuild/BriefingChairWarm.mat";
        var chairMaterial = AssetDatabase.LoadAssetAtPath<Material>(chairPath);
        if (chairMaterial == null)
        {
            chairMaterial = new Material(GameObject.Find("Briefing_ChairA").GetComponentsInChildren<Renderer>().First(r=>r.enabled).sharedMaterial);
            chairMaterial.SetColor("_BaseColor",new Color(.82f,.72f,.59f));
            AssetDatabase.CreateAsset(chairMaterial,chairPath);
        }
        foreach (string name in new[] { "Briefing_ChairA", "Briefing_ChairB", "Briefing_ChairC", "Briefing_ChairD" })
            foreach (var r in GameObject.Find(name).GetComponentsInChildren<Renderer>().Where(r=>r.enabled)) r.sharedMaterial=chairMaterial;
        Physics.SyncTransforms();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }

    private static void Stand(string name, Vector3 position)
    {
        var go = GameObject.Find(name);
        var seat = go.GetComponent<InterrogationRoom.Gameplay.Interaction.NetworkChairSeat>();
        var so = new SerializedObject(seat);
        var point = so.FindProperty("standPoint").objectReferenceValue as Transform;
        if (point == null)
        {
            point = new GameObject("StandPoint").transform;
            point.SetParent(go.transform, false);
            so.FindProperty("standPoint").objectReferenceValue = point;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        point.position = position;
        PrefabUtility.RecordPrefabInstancePropertyModifications(point);
    }

    private static void Center(string name, Vector3 target)
    {
        var go = GameObject.Find(name);
        var rs = go.GetComponentsInChildren<Renderer>().Where(r => r.enabled).ToArray();
        var b = rs[0].bounds;
        foreach (var r in rs.Skip(1)) b.Encapsulate(r.bounds);
        go.transform.position += target-b.center;
        PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);
    }
}
