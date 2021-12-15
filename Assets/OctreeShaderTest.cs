using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

using Vox;
using Vox.Octree;

public class OctreeShaderTest : MonoBehaviour
{

    public ComputeShader shader;
    public Material instanceMaterial;

    private ComputeBuffer octreeBuffer, hitsBuffer, argsBuffer, instanceDataBuffer;

    private uint[] args = {0, 0, 0, 0, 0};
    private uint[] hits = new uint[64*64*64];

    private Mesh instanceMesh;
    private List<Transform> cubes;
    private List<ONode3> flatTree = new List<ONode3>();
    private Mesh cube;
    private Material cubeMat;

    private int[] grid;

    void Start() {
        var testTree = new List<ONode3>();
        testTree.Add(new ONode3(Vector3Int.zero, ushort.MaxValue));
        Debug.Log($"Size: {Marshal.SizeOf(typeof(ONode3))}");
        Debug.Log($"leaf: {testTree[0].leaf}, value: {testTree[0].value}");

        OctreeBuilder.Insert2(new OPoint2(Vector3Int.zero, 2), testTree, 0, 6);
        // testTree[0].SetChild(123, 2);
        // Debug.Log($"{testTree[0].GetChild(0)}");
        // Debug.Log($"{testTree[0].GetChild(1)}");
        // Debug.Log($"{testTree[0].GetChild(2)}");
        // Debug.Log($"{testTree[0].GetChild(3)}");
        // Debug.Log($"{testTree[0].GetChild(4)}");
        // Debug.Log($"{testTree[0].GetChild(5)}");
        // Debug.Log($"{testTree[0].GetChild(6)}");
        // Debug.Log($"{testTree[0].GetChild(7)}");

        Debug.Log($"leaf: {testTree[0].leaf}, value: {testTree[0].value}");
        OctreeBuilder.Insert2(new OPoint2(Vector3Int.one, 2), testTree, 0, 6);


        // instanceMesh = CubeMeshData.CreateMappedCube();
        // grid = GetRandomGrid(Vector3Int.one*64, 0.02f);
        // PrepareShaders();
    }

    void Update() {
        if(Input.GetButtonDown("Fire1"))
            CheckRay();
    }

    private void CheckRay(){
        shader.SetMatrix("_CamToWorld", Camera.main.cameraToWorldMatrix);
        shader.SetMatrix("_CamToWorld", Camera.main.projectionMatrix.inverse);
        shader.SetVector("origin", Camera.main.transform.position);
        shader.SetInt("width", Camera.main.pixelWidth);
        shader.SetInt("height", Camera.main.pixelHeight);
        shader.SetVector("offsetBounds", Vector3.one*64);
        shader.SetVector("chunkOffset", Vector3.zero);

        shader.Dispatch(0, 1,1,1);
        var hdata = new uint[hits.Length];
        hitsBuffer.GetData(hdata);
    }

    private void PrepareShaders(){
        args = new[] {
            (uint)instanceMesh.GetIndexCount(0),
            (uint)0,
            (uint)instanceMesh.GetIndexStart(0),
            (uint)instanceMesh.GetBaseVertex(0),
            (uint)0
        };
        hitsBuffer = new ComputeBuffer(hits.Length, sizeof(uint));
        hitsBuffer.SetData(hits);
        argsBuffer = new ComputeBuffer(5, sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(args);
        shader.SetBuffer(0, "Tree", octreeBuffer);
    }

    private int[] GetRandomGrid(Vector3Int shape, float chance){
        cubes = new List<Transform>();
        flatTree.Add(new ONode3(new Vector3Int(0,0,0), ushort.MaxValue));
        var timer = new System.Diagnostics.Stopwatch();
        var grid = new int[shape.x*shape.y*shape.z];
        for(int i=0; i<grid.Length; i++){
            if(UnityEngine.Random.Range(0f,1f) <= chance){
                grid[i] = 1;
                timer.Start();
                Vector3Int pos = new Vector3Int(i/(64*64), (i/64)%64, i%64);
                OctreeBuilder.Insert2(new OPoint2(pos, i), flatTree, 0, 6);
                timer.Stop();

                GameObject c = new GameObject($"cube{pos.x},{pos.y}{pos.z}", typeof(MeshFilter), typeof(MeshRenderer));
                c.GetComponent<MeshFilter>().mesh = cube;
                c.GetComponent<MeshRenderer>().material = cubeMat;
                c.transform.position = pos + Vector3.one*0.5f;
                cubes.Add(c.transform);
            }
        }
        Debug.Log($"tree size: {flatTree.Count}");
        Debug.Log($"Built tree in {timer.Elapsed.Seconds}:{timer.Elapsed.Milliseconds:000}");
        octreeBuffer = new ComputeBuffer(flatTree.Count, 24);
        octreeBuffer.SetData(flatTree.ToArray());
        return grid;
    }

    private void OnDisable() {
        octreeBuffer?.Dispose();
        hitsBuffer?.Dispose();
        instanceDataBuffer?.Dispose();
        argsBuffer?.Dispose();
    }
}
