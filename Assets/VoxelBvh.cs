using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Vox.Bvh;



public class VoxelBvh : MonoBehaviour
{
    public const int DIMS = 32;
    public const int CHUNK_SIZE = DIMS*DIMS*DIMS;

    public Mesh mesh;
    public Material material;
    public ComputeShader shader;
    
    [Header("Bounding Boxes")]
    public bool ShowParents;
    public bool DrawBoxes;
    public bool DrawAll;
    public bool lookAt;

    [Range(0, 15)]
    public int Depth;


    private uint[] chunk;
    private List<Transform> mflist = new List<Transform>();
    private BNode[] heirarchy3;

    private List<Bounds> errorPrims = new List<Bounds>();
    private Bounds lastCheck;

    private int nextNodeId;

    private List<Bounds> shaderHits;
    private List<BPrimitive> primitives;

    private int currentNode;

    private Vector3 camOldPos;
    private System.TimeSpan lastGpuCheck;


    private ComputeBuffer bvhBuffer, rayBuffer, hitsBuffer;

    void Start() {
        var nodesize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(BNode));
        Debug.Log("Nodesize: " + nodesize);
        primitives = GenerateChunk();
        RenderChunk();
        //FIX: just commented this out to get rid of errors. The method used no longer exists
        // var tree3 = Vox.Bvh.TreeBuilder.BuildTree(primitives);
        // var root3 = tree3.Count;
        // heirarchy3 = tree3.ToArray();
        // currentNode = tree3.Count-1;

        camOldPos = Camera.main.transform.position;

        CheckBvhGPU(heirarchy3, Camera.main.ScreenPointToRay(Vector3.one));
    }

    private void Update() {
        if(Input.GetButtonDown("Fire1")){
            var r =Camera.main.ScreenPointToRay(Input.mousePosition);
            CheckNewRays();
        }

        if(lookAt && Camera.main.transform.position != camOldPos){
            camOldPos = Camera.main.transform.position;
            Camera.main.transform.LookAt(new Vector3(16, 16, 16));
            var r =Camera.main.ScreenPointToRay(Input.mousePosition);
            CheckNewRays();
        }
    }

    private void OnDrawGizmos() {
        Gizmos.color = Color.red;
        if(shaderHits != null){
            foreach(var h in shaderHits){
                Gizmos.DrawWireCube(h.center, h.size);
            }
        }
    }

    private void OnGUI() {
        GUILayout.Label($"<color=red>Last check: {lastGpuCheck.Seconds:00}:{lastGpuCheck.Milliseconds:000}</color>");
    }

    private void CheckBvhGPU(Vox.Bvh.BNode[] tree, Ray r){
        bvhBuffer = new ComputeBuffer(tree.Length, 36);
        bvhBuffer.SetData(tree);


        shader.SetInt("height", Screen.height);
        shader.SetInt("width", Screen.width);
        shader.SetInt("root", tree.Length-1);
        shader.SetBuffer(0, "Bvh", bvhBuffer);
    }

    private void CheckNewRays(){
        var timer = new System.Diagnostics.Stopwatch();

        timer.Start();
        var rays = new Ray[Screen.width*Screen.height];
        for (int x = 0; x < Screen.width; x++) {
            for (int y = 0; y < Screen.height; y++) {
                rays[x*Screen.height+y] = Camera.main.ScreenPointToRay(new Vector3(x,y));
            }
        }

        rayBuffer?.Release();
        rayBuffer = new ComputeBuffer(rays.Length, 24);
        rayBuffer.SetData(rays.ToArray());

        hitsBuffer?.Release();
        var hits = new int[primitives.Count];
        hitsBuffer = new ComputeBuffer(rays.Length, 4);
        hitsBuffer.SetData(hits);

        shader.SetBuffer(0, "Rays", rayBuffer);
        shader.SetBuffer(0, "Hits", hitsBuffer);

        shader.Dispatch(0, Screen.width/8, Screen.height/8, 1);
        hitsBuffer.GetData(hits);

        timer.Stop();

        lastGpuCheck = timer.Elapsed;

        shaderHits = new List<Bounds>();
        Debug.Log($"Count: {hits.Length}\n True: {hits.Count(h => h == 0)}\nFalse: {hits.Count(h => h>0)}");
        for(int i = 0; i<primitives.Count; i++){
            var p = primitives[i];
            var ob = mflist[p.id];
            if(hits[i] > 0){
                //b = mflist[i].GetComponent<MeshRenderer>().bounds;
                ob.GetComponent<MeshRenderer>().enabled = true;
            }else{
                Bounds b = new Bounds();
                b = ob.GetComponent<MeshRenderer>().bounds;
                shaderHits.Add(b);
                ob.GetComponent<MeshRenderer>().enabled = false;
                //ob.GetComponent<MeshRenderer>().material.SetColor("_Color", Color.red);
            }
        }

        hitsBuffer.Release();
        rayBuffer.Release();

    }


    private List<BPrimitive> GenerateChunk(){
        chunk = new uint[CHUNK_SIZE];
        List<BPrimitive> primitives =  new List<BPrimitive>();
        var nprims = 0;

        for(uint i = 0; i<CHUNK_SIZE; i++){
            var chance = (uint)Random.Range(0, 100);
            if(chance <= 50){
                chunk[i] = (uint)1;
                var prim = new BPrimitive(primitives.Count, new Bounds(new Vector3(i/(DIMS*DIMS), (i/DIMS)%DIMS, i%DIMS)+Vector3.one*0.5f, Vector3.one));
                primitives.Add(prim);
                nprims++;
            }else{
                chunk[i] = (uint)0;
            }
        }
        return primitives;
    }

    private void RenderChunk(){
        mflist = new List<Transform>(chunk.Length);
        for(int x = 0; x<DIMS;x++)
            for(int y = 0; y<DIMS;y++)
                for(int z = 0; z<DIMS;z++){
                    if(chunk[x*DIMS*DIMS+y*DIMS+z] == 0)
                        continue;
                    var obj = new GameObject($"cube{x},{y},{z}", typeof(MeshRenderer), typeof(MeshFilter));
                    obj.transform.position = new Vector3(x,y,z)+Vector3.one*0.5f;
                    //obj.transform.localScale = new Vector3(2,2,2);
                    obj.GetComponent<MeshFilter>().mesh = mesh;
                    obj.GetComponent<MeshRenderer>().material = material;
                    mflist.Add(obj.transform);
                }
    }

    private void OnDestroy() {
        rayBuffer?.Dispose();
        bvhBuffer?.Dispose();
        hitsBuffer?.Dispose();
    }
}
