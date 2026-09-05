using System;
using System.Collections.Generic;
using System.Linq;
using InterrogationRoom.Gameplay.Interaction;
using Mirror;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>Editor evidence for the built scene, using the player's collision envelope.</summary>
public static class StationRebuildValidation
{
    private const float Radius = .44f;

    [MenuItem("Tools/Interrogation Room/Station Rebuild/4 Validate traversal")]
    public static void ValidateMenu() => Debug.Log(Validate());

    public static string Validate()
    {
        if (Application.isPlaying || SceneManager.GetActiveScene().path != "Assets/Scenes/Room.unity")
            throw new InvalidOperationException("Validate the saved Room scene in Edit Mode.");
        Physics.SyncTransforms();
        var failures = new List<string>();
        var spawns = Object.FindObjectsByType<NetworkStartPosition>(FindObjectsSortMode.None);
        var doors = Object.FindObjectsByType<NetworkDoor>(FindObjectsSortMode.None);
        var rooms = Object.FindObjectsByType<RoomVolume>(FindObjectsSortMode.None);
        var playerPrefab=Object.FindFirstObjectByType<NetworkManager>().playerPrefab;
        float eyeHeight=new SerializedObject(playerPrefab.GetComponent<PlayerInteractor>()).FindProperty("serverViewHeight").floatValue;
        if (spawns.Length != 8) failures.Add("Expected eight spawn points.");
        if (doors.Length != 14) failures.Add("Expected fourteen network doors.");
        if (rooms.Select(r => r.RoomId).Distinct().Count() != 11)
            failures.Add("Expected ten rooms plus corridor.");
        foreach (var spawn in spawns)
            if (!Clear(spawn.transform.position, false)) failures.Add("Blocked spawn: " + spawn.name);
        foreach (var door in doors)
        {
            Vector3 p = door.transform.position;
            p.y = .04f;
            Vector3 direction = door.transform.forward;
            foreach(float distance in new[]{-.3f,-.15f,0f,.15f,.3f})
            {
                Vector3 probe=p+direction*distance+Vector3.up*.2f;
                if(!Physics.Raycast(probe,Vector3.down,out RaycastHit threshold,.4f,~0,QueryTriggerInteraction.Ignore) || Math.Abs(threshold.point.y)>.03f)
                    failures.Add("Missing or uneven threshold floor: "+door.name);
            }
            RaycastHit[] hits = Physics.CapsuleCastAll(p-direction*1.4f+Vector3.up*.5f,
                p-direction*1.4f+Vector3.up*1.4f, Radius, direction, 2.8f, ~0, QueryTriggerInteraction.Ignore);
            foreach (var hit in hits.Where(h => h.collider.GetComponentInParent<NetworkDoor>() == null))
                failures.Add("Blocked door approach " + door.name + ": " + hit.collider.name);
            if (!rooms.Any(r=>r.RoomId==door.RoomAId) || !rooms.Any(r=>r.RoomId==door.RoomBId))
                failures.Add("Unresolved acoustic portal: " + door.name);
            foreach (float side in new[] { -1f, 1f })
            {
                Vector3 approach = p + direction * side * 1.3f;
                if (Vector3.Distance(approach, door.InteractionPosition) >= 2.5f ||
                    !VisibleFrom(approach + Vector3.up * eyeHeight, door.InteractionPosition, door))
                    failures.Add("Door cannot be operated from side " + side + ": " + door.name);
            }
        }

        // Quarter-metre occupancy grid resolves approaches between a seat and its neighbours.
        // Doors are treated as open; walls and props remain solid.
        const int width = 121, depth = 109;
        var walkable = new bool[width,depth];
        var reached = new bool[width,depth];
        Func<int,int,Vector3> position = (x,z) => new Vector3(-15+x*.25f,.04f,-13+z*.25f);
        for (int x=0;x<width;x++)
        for (int z=0;z<depth;z++)
        {
            Vector3 p = position(x,z);
            bool floor = Physics.Raycast(p+Vector3.up*.2f,Vector3.down,out RaycastHit hit,.4f,~0,QueryTriggerInteraction.Ignore)
                && hit.normal.y>.7f && Math.Abs(hit.point.y)<.15f;
            walkable[x,z] = floor && Clear(p,true);
        }
        var queue = new Queue<Vector2Int>();
        Vector3 origin = spawns[0].transform.position;
        Vector2Int start = new Vector2Int(Mathf.RoundToInt((origin.x+15)*4),Mathf.RoundToInt((origin.z+13)*4));
        // A valid spawn can round onto an occupied neighbour on the coarse grid.
        var candidates = new List<Vector2Int>();
        for (int dx=-1; dx<=1; dx++)
        for (int dz=-1; dz<=1; dz++)
        {
            var candidate = start + new Vector2Int(dx,dz);
            if (candidate.x>=0 && candidate.y>=0 && candidate.x<width && candidate.y<depth &&
                walkable[candidate.x,candidate.y]) candidates.Add(candidate);
        }
        if (candidates.Count>0)
            start=candidates.OrderBy(p=>(position(p.x,p.y)-origin).sqrMagnitude).First();
        else failures.Add("No walkable grid cell next to spawn: "+spawns[0].name);
        if (walkable[start.x,start.y]) { queue.Enqueue(start); reached[start.x,start.y]=true; }
        var steps = new[] { Vector2Int.left,Vector2Int.right,Vector2Int.up,Vector2Int.down };
        while(queue.Count>0)
        {
            Vector2Int p=queue.Dequeue();
            foreach(Vector2Int step in steps)
            {
                Vector2Int n=p+step;
                if(n.x<0||n.y<0||n.x>=width||n.y>=depth||reached[n.x,n.y]||!walkable[n.x,n.y])continue;
                reached[n.x,n.y]=true;
                queue.Enqueue(n);
            }
        }
        var reachable = new List<Vector3>();
        for(int x=0;x<width;x++)for(int z=0;z<depth;z++)if(reached[x,z])reachable.Add(position(x,z));
        foreach(var room in rooms)
            if(!reachable.Any(p=>room.Contains(p+Vector3.up)))failures.Add("Unreachable room: "+room.name);
        var actions=Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
            .OfType<INetworkInteractable>().Where(a=>!(a is NetworkDoor)).ToArray();
        foreach(var action in actions)
        {
            Vector3 target=action.InteractionPosition;
            var component=(Component)action;
            if(!reachable.Any(p=>Vector3.Distance(p,target)<2.5f && VisibleFrom(p+Vector3.up*eyeHeight,target,component)))
                failures.Add("No reachable interaction with line of sight: "+AnimationUtility.CalculateTransformPath(component.transform,null));
        }
        foreach(var seat in actions.OfType<NetworkChairSeat>())
            if(!Clear(seat.GetStandPositionServer(),false))failures.Add("Blocked stand-up position: "+seat.name);
        foreach(var group in Object.FindObjectsByType<NetworkIdentity>(FindObjectsSortMode.None).GroupBy(n=>n.sceneId))
            if(group.Key==0 || group.Count()>1)failures.Add("Invalid/duplicate scene network identity: "+group.Key);
        string summary=$"[StationRebuild] {spawns.Length} spawns; {doors.Length} doorways; {rooms.Length} room volumes; {actions.Length} interactions; {reachable.Count} reachable grid cells.";
        return summary+"\n"+(failures.Count==0?"PASS: occupancy, doorway clearance, room reachability, interaction range and line of sight, stand-up clearance, portal IDs and network IDs.":"FAIL:\n"+string.Join("\n",failures.Distinct()));
    }

    private static bool VisibleFrom(Vector3 eye,Vector3 target,Component action)
    {
        Vector3 delta=target-eye;
        var identity=action.GetComponentInParent<NetworkIdentity>();
        foreach(var hit in Physics.RaycastAll(eye,delta.normalized,delta.magnitude+.05f,~0,QueryTriggerInteraction.Collide).OrderBy(h=>h.distance))
        {
            if(hit.collider.transform.IsChildOf(action.transform) ||
                identity!=null && hit.collider.GetComponentInParent<NetworkIdentity>()==identity)return true;
            return false;
        }
        return false;
    }

    private static bool Clear(Vector3 p,bool ignoreDoors) =>
        !Physics.OverlapCapsule(p+Vector3.up*.5f,p+Vector3.up*1.4f,Radius,~0,QueryTriggerInteraction.Ignore)
            .Any(c=>!ignoreDoors || c.GetComponentInParent<NetworkDoor>()==null);
}
