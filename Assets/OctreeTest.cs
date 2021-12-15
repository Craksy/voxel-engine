using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using EasyButtons;
using UnityEngine;
using Vox.Octree;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;
using RayMask = System.Tuple<UnityEngine.Vector3, UnityEngine.Vector3, UnityEngine.Vector3, int>;


public class OctreeTest : MonoBehaviour
{

    public Mesh cube;
    public Material material;

    public int NodeId;


    private int[] grid = new int[2];
    private ONode tree;
    private List<ONodeFlat> flatTree = new List<ONodeFlat>();
    private List<Transform> cubes;
    private List<ONodeFlat> visitedNodes = new List<ONodeFlat>();
    private ONodeFlat lastQuery;
    private BoundsInt entry;
    private OPoint lastHist;
    private Ray lastRay;
    private Vector3 originOffset;
    private HashSet<int> hits = new HashSet<int>();
    private Color[] depthColors = {Color.red, Color.green, Color.blue, Color.cyan, Color.magenta, Color.yellow};
    private int lastIters;



    private int[,] octantLookup = {
        {4,2,1},
        {5,3,8},
        {6,8,3},
        {7,8,8},
        {8,6,5},
        {8,7,8},
        {8,8,7},
        {8,8,8},
    };

    void Start() {
        tree = new ONode(new BoundsInt(0,0,0,16,16,16));
        grid = GetRandomGrid(Vector3Int.one*32, 0.02f);
    }

    private void Update() {
        if(Input.GetButtonDown("Fire1")){
            var r = Camera.main.ScreenPointToRay(Input.mousePosition);
            // Debug.DrawRay(r.origin, r.direction*50, Color.green, 10);
			lastRay = r;
            CheckRayFlat(r, flatTree);
        }
        if(Input.GetButtonDown("Jump")){
            CheckScreen();
        }
    }

    private void OnDrawGizmos(){
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(Vector3.one*16, Vector3.one*32);
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(entry.center, entry.size);
        Gizmos.color = Color.magenta;
        Gizmos.DrawRay(lastRay.origin, lastRay.direction*50);
        Gizmos.DrawWireSphere(originOffset, 1f);
        var lhb = new BoundsInt(lastHist.position, Vector3Int.one);
        Gizmos.DrawWireCube(lhb.center, lhb.size);

        foreach(var n in visitedNodes){
            Gizmos.color = depthColors[6-n.depth];
            var b = new BoundsInt(n.position, Vector3Int.one*(int)Mathf.Pow(2,n.depth));
            Gizmos.DrawWireCube(b.center, b.size);
        }
        if(lastQuery != null){
            Gizmos.color = depthColors[6-lastQuery.depth];
            var b = new BoundsInt(lastQuery.position, Vector3Int.one*(int)Mathf.Pow(2,lastQuery.depth));
            Gizmos.DrawWireCube(b.center, b.size);
        }
    }

    [Button("Query")]
    private void Query(){
        var node = flatTree[NodeId];
        lastQuery = node;
        Debug.Log($"--QUERY--\n{ChildInfo(NodeId)}");
    }

    [Button("Clear Visited")]
    private void ClearVisited() => visitedNodes.Clear();

    private void CheckRayFlat(Ray ray, List<ONodeFlat> tree){
        var timer = new Stopwatch();
        timer.Start();

        var rm = GetRayMask(ray, new BoundsInt(Vector3Int.zero, Vector3Int.one*(int)Mathf.Pow(2,tree[0].depth)));
        var tmin = Mathf.Max(rm.tmin.x, rm.tmin.y, rm.tmin.z);
        var tmax = Mathf.Min(rm.tmax.x, rm.tmax.y, rm.tmax.z);
        if(tmin > tmax)
            return;

        var res = CheckIntersectFlat(rm, tree, ray);
        timer.Stop();
        if(res != null){
            Debug.Log($"HIT!! {res?.data}");
            Debug.Log($"Nodes visited: {visitedNodes.Count}");
            Debug.Log($"Iterations: {lastIters}");
            Debug.Log($"took: {timer.Elapsed.Seconds}{timer.ElapsedMilliseconds:000}");
            lastHist = res.Value;
        }
    }

    private OPoint? CheckLeafFlat(ONodeFlat node, Ray ray){
        var idir = new Vector3(1/ray.direction.x, 1/ray.direction.y, 1/ray.direction.z);
        var t0 = Vector3.Scale(node.value.position - ray.origin, idir);
        var t1 = Vector3.Scale(node.value.position+Vector3Int.one - ray.origin, idir);
        var s0 = Vector3.Min(t0, t1);
        var s1 = Vector3.Max(t0, t1);
        var tmin = Mathf.Max(s0.x, s0.y, s0.z);
        var tmax = Mathf.Min(s1.x, s1.y, s1.z);
        if (tmin < tmax)
            return node.value;
        return null;
    }

    private string ChildInfo(int nodeId){
        if(nodeId == ushort.MaxValue)
            return "exit node";
        var node = flatTree[nodeId];
        var cs = string.Join(", ", node.children.Select(c => c.ToString()));
        return $"id: {nodeId}\nposition: {node.position}\ndepth:{node.depth}\nleaf:{node.leaf}\nchildren: {cs}";
    }

    private OPoint? CheckIntersectFlat((Vector3,Vector3,int) rm, List<ONodeFlat> tree, Ray original_ray){
        var octant = -1;
        lastIters = 0;
        var nodeId = 0;
        Vector3 tm = new Vector3();
        var qt0 = new Vector3();
        var qt1 = new Vector3();
        var (t0,t1,mask) = rm;
        var stack = new Stack<(Vector3 t0, Vector3 tm, Vector3 t1, int)>();
        var node = tree[0];
        Vector3[] ts = new Vector3[3];
        int iterations = 0;
        var indent = "";
        visitedNodes.Clear();
        while(nodeId != ushort.MaxValue && iterations < 100){
            lastIters++;
            iterations++;
            node = tree[nodeId];
            indent = new String('-', 3*(6-node.depth)) + "|";
            // Current node exhausted. pop
            if(octant > 7){
                if(stack.Count < 1)
                    break;
                nodeId = node.parent;
                (t0,tm,t1,octant) = stack.Pop();
                ts[0] = t0;
                ts[1] = tm;
                ts[2] = t1;
                continue;
            }
            if(octant >= 0)
                Debug.Log($"{indent}Octant: {octant}\n{ChildInfo(node.children[octant])}");
            if(octant < 0){
                tm = (t0+t1)*0.5f;
                octant = 0;
                var major = Enumerable.Range(0, 3).Aggregate((a,b) => t0[a]>t0[b]?a:b);
                for(int i = 0; i<3; i++)
                    if(i != major && tm[i] < t0[major]) octant |= 1<<(2-i);
                ts[0] = t0;
                ts[1] = tm;
                ts[2] = t1;
                Debug.Log($"{indent}first: {octant}\n{ChildInfo(node.children[octant])}");
            }
            var cidx = node.children[octant^mask];
            var omask = new Vector3Int(octant>>2, octant>>1&1, octant&1);
            for(int i=0;i<3;i++){
                qt0[i] = ts[omask[i]][i];
                qt1[i] = ts[omask[i]+1][i];
            }
            int exitPlane;
            if(qt1.x < qt1.y && qt1.x < qt1.z)
                exitPlane = 0;
            else
                exitPlane = (qt1.y < qt1.x && qt1.y < qt1.z) ? 1 : 2;
            octant = octantLookup[octant, exitPlane];
            if(cidx == ushort.MaxValue)
                continue;

            visitedNodes.Add(tree[cidx]);
            if(tree[cidx].leaf){
                Debug.Log($"{indent}hit leaf\n{ChildInfo(cidx)}", cubes[tree[cidx].value.data]);
                var res = CheckLeafFlat(tree[cidx], original_ray);
                if(res != null)
                    return res;
            }else{
                stack.Push((t0,tm,t1,octant));
                nodeId = cidx;
                octant = -1;
                t0 = qt0;
                t1 = qt1;
            }
        }
        return null;
    }

    private (Vector3 tmin, Vector3 tmax, int dirMask) GetRayMask(Ray ray, BoundsInt bounds){
        var mask = 0;
        var rdir = new Vector3(ray.direction.x, ray.direction.y, ray.direction.z);
        var ro = new Vector3(ray.origin.x, ray.origin.y, ray.origin.z);
        if(ray.direction.x<0){
            mask += 4;
            rdir.x *= -1;
            ro.x = bounds.min.x + bounds.max.x - ray.origin.x;
        }
        if(ray.direction.y<0){
            mask += 2;
            rdir.y *= -1;
            ro.y = bounds.min.y + bounds.max.y - ray.origin.y;
        }
        if(ray.direction.z<0){
            mask += 1;
            rdir.z *= -1;
            ro.z = bounds.min.z + bounds.max.z - ray.origin.z;
        }
        

        var invDir = new Vector3(1/rdir.x, 1/rdir.y, 1/rdir.z);
        var t0 = Vector3.Scale((bounds.min - ro), invDir);
        var t1 = Vector3.Scale((bounds.max - ro), invDir);
        return (tmin: t0, tmax: t1, dirMask: mask);
    }

    private void CheckScreen(){
        hits.Clear();
        foreach(var c in cubes)
            c.GetComponent<MeshRenderer>().material.SetColor("_Color", Color.red);
        for(int x = 0; x<Camera.main.pixelWidth; x++){
            for(int y = 0; y<Camera.main.pixelHeight; y++){
                var r = Camera.main.ScreenPointToRay(new Vector3(x,y));
                CheckRay(r, tree);
            }
        }
        foreach(var h in hits){
            cubes[h].GetComponent<MeshRenderer>().material.SetColor("_Color", Color.green);
        }

    }

    private void CheckRay(Ray ray, ONode node){
        var (t0, t1, mask) = GetRayMask(ray, node.bounds);
        var tmin = Mathf.Max(t0.x, t0.y, t0.z);
        var tmax = Mathf.Min(t1.x, t1.y, t1.z);
        if(tmin > tmax)
            return;
        var res = CheckIntersect(node,t0, t1, mask, ray);
        if(res != null){
            Debug.Log($"HIT! {res.Value.position}");
            hits.Add(((OPoint)res).data);
            // cubes[lastHist.data].GetComponent<MeshRenderer>().material.SetColor("_Color", Color.gray);
            // lastHist = (OPoint)res;
            // cubes[lastHist.data].GetComponent<MeshRenderer>().material.SetColor("_Color", Color.green);
        }
    }

    private OPoint? CheckLeaf(ONode node, Ray ray){
        var idir = new Vector3(1/ray.direction.x, 1/ray.direction.y, 1/ray.direction.z);
        var t0 = Vector3.Scale(node.value.position - ray.origin, idir);
        var t1 = Vector3.Scale(node.value.position+Vector3Int.one - ray.origin, idir);
        var s0 = Vector3.Min(t0, t1);
        var s1 = Vector3.Max(t0, t1);
        var tmin = Mathf.Max(s0.x, s0.y, s0.z);
        var tmax = Mathf.Min(s1.x, s1.y, s1.z);
        if (tmin < tmax)
            return node.value;
        return null;
    }


    private OPoint? CheckIntersect(ONode node, Vector3 t0, Vector3 t1, int mask, Ray originalRay){
        if(node.leaf)
            return CheckLeaf(node, originalRay);
        var tm = (t0+t1)*0.5f;
        var octant = 0;

        var major = Enumerable.Range(0, 3).Aggregate((a,b) => t0[a]>t0[b]?a:b);
        for(int i = 0; i<3; i++){
            if(i != major && tm[i] < t0[major]) octant |= 1<<(2-i);
        }
        var entryPos = Vector3.Scale(new Vector3(octant>>2, octant>>1&1, octant&1),Vector3.one*8)+Vector3.one*0.5f;
        if(node.children[octant] != null)
            entry = node.children[octant].bounds;

        var ts = new[] {t0, tm, t1};
        while(octant < 8){
            var child = node.children[octant ^ mask];
            var omask = new Vector3Int(octant>>2, octant>>1&1, octant&1);
            Vector3 qt0 = new Vector3();
            Vector3 qt1 = new Vector3();
            for(int i = 0; i<3; i++){
                qt0[i] = ts[omask[i]][i];
                qt1[i] = ts[omask[i]+1][i];
            }
            if(child != null){
                var result = CheckIntersect(child, qt0, qt1, mask, originalRay);
                if(result != null)
                    return result;
            }
            int exitPlane;
            if(qt1.x < qt1.y && qt1.x < qt1.z)
                exitPlane = 0;
            else
                exitPlane = (qt1.y < qt1.x && qt1.y < qt1.z) ? 1 : 2;
            octant = octantLookup[octant, exitPlane];
        }
        return null;
    }

    private void RenderGrid(){
        cubes = new List<Transform>();
        for(int i = 0; i<grid.Length;i++){
            if(grid[i] == 1){
                Vector3 pos = new Vector3(i/(16*16), (i/16)%16, i%16);
                GameObject c = new GameObject($"cube{pos.x},{pos.y}{pos.z}", typeof(MeshFilter), typeof(MeshRenderer));
                c.GetComponent<MeshFilter>().mesh = cube;
                c.GetComponent<MeshRenderer>().material = material;
                c.transform.position = pos + Vector3.one*0.5f;
                cubes.Add(c.transform);
            }
        }
    }

    private int[] GetRandomGrid(Vector3Int shape, float chance){
        cubes = new List<Transform>();
        flatTree.Add(new ONodeFlat(new Vector3Int(0,0,0), 6, ushort.MaxValue));
        var grid = new int[shape.x*shape.y*shape.z];
        for(int i=0; i<grid.Length; i++){
            if(Random.Range(0f,1f) <= chance){
                grid[i] = 1;
                Vector3Int pos = new Vector3Int(i/(32*32), (i/32)%32, i%32);
                tree.Insert(new OPoint{position = pos, data=cubes.Count});
                OctreeBuilder.Insert(new OPoint{position = pos, data=cubes.Count}, ref flatTree, 0);

                GameObject c = new GameObject($"cube{pos.x},{pos.y}{pos.z}", typeof(MeshFilter), typeof(MeshRenderer));
                c.GetComponent<MeshFilter>().mesh = cube;
                c.GetComponent<MeshRenderer>().material = material;
                c.transform.position = pos + Vector3.one*0.5f;
                cubes.Add(c.transform);
            }
        }
        Debug.Log($"tree size: {flatTree.Count}");
        return grid;
    }
}
