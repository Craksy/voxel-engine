using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

using System.Linq;
using Source.Vox;
using Vox.Bvh;
using Vox;
using Utility;

namespace Vox.Rendering {
    public class OcclusionRenderer : MonoBehaviour
    {
        public VoxelBlocksManager blocksManager;
        public Material instanceMaterial, depthMat;
        public World world;
        public Camera mainCam;
        public ComputeShader cullingShader;
        public bool ShowStats;
        public GUISkin skin;
        [System.NonSerialized] public Bounds RenderBounds;
        public System.Diagnostics.Stopwatch bvhTimer = new System.Diagnostics.Stopwatch();

        private Mesh instanceMesh;
        private ComputeBuffer hitsBuffer, bvhBuffer, argsBuffer, instanceDataBuffer;
        private uint[] args = {0, 0, 0, 0, 0};
        private uint[] hits = new uint[128*128*128];
        private bool bufferDirty;
        private int screenWidth, screenHeight;
        private Texture2D redTexture;
        private RenderTexture renderTexture, depthmapDisplay;

        // private Dictionary<ChunkId, List<Node128>> Bvhs = new Dictionary<ChunkId, List<Node128>>();
        private Dictionary<ChunkId, BvhBuffer> BvhBuffers = new Dictionary<ChunkId, BvhBuffer>();
        // private Dictionary<ChunkId, int> BlockCounts = new Dictionary<ChunkId, int>();
        private Vector3 lastCamPos, lastSortPos;
        private Quaternion lastCamRot;

        private System.Diagnostics.Stopwatch totalTimer, cullingTimer, rectTimer, bufferTimer;
        private int memoryUsage, nodeCount, pixelsSaved;
        private int cs_CamToWorld, cs_CamInverseProjection, cs_PixelOffset, cs_Root, cs_ChunkOffset, cs_BvhBuffer;

        private bool cullingEnabled = true;

        private int releasesSkipped;
        private static readonly int InstanceDataBuffer = Shader.PropertyToID("InstanceDataBuffer");
        private static readonly int Textures = Shader.PropertyToID("_Textures");

        void Start()
        {
            Debug.Log($"{uint.MaxValue}");
            // Debug.Log($"{SystemInfo.SupportsTextureFormat(TextureFormat.RGBAHalf)}");
            depthmapDisplay = new RenderTexture(mainCam.pixelWidth, mainCam.pixelHeight, 0, RenderTextureFormat.ARGB32);
            depthmapDisplay.Create();
            totalTimer = new System.Diagnostics.Stopwatch();
            cullingTimer = new System.Diagnostics.Stopwatch();
            rectTimer = new System.Diagnostics.Stopwatch();
            bufferTimer = new System.Diagnostics.Stopwatch();
            instanceMesh = CubeMeshData.CreateMappedCube();
            PrepareShaders();
            bufferDirty = true;
        }

        // Update is called once per frame
        void Update()
        {
            if(Input.GetKeyDown(KeyCode.C))
                cullingEnabled = !cullingEnabled;
            if(bufferDirty || lastCamPos != mainCam.transform.position || lastCamRot != mainCam.transform.rotation){
                lastCamPos = mainCam.transform.position;
                lastCamRot = mainCam.transform.rotation;
                if(Vector3.Distance(lastSortPos, lastCamPos) > 5)
                    OrderTrees();
                if(cullingEnabled)
                    UpdateBuffers();
            }

            Graphics.DrawMeshInstancedIndirect(instanceMesh, 0, instanceMaterial, RenderBounds, argsBuffer, 0, null, UnityEngine.Rendering.ShadowCastingMode.Off, false, 1);
        }

        private void OnGUI() {
            if(ShowStats){
                GUI.skin = skin;
                var total = totalTimer.Elapsed;
                var countlabel = $"{BvhBuffers.Count} trees loaded, with a total of {nodeCount:N0} nodes";
                var timeLabel = $"Last update total: {total.Seconds}.{total.Milliseconds:000}";
                var memlabel =$"Memory consumption: {memoryUsage}MB";
                var bvhtime = $"Building took: {bvhTimer.Elapsed.Seconds}.{bvhTimer.Elapsed.Milliseconds:000} seconds";
                GUILayout.Box($"{bvhtime}\n{timeLabel}\n{countlabel}\n{memlabel}");
                RenderTexture.active = depthmapDisplay;
                Graphics.Blit(renderTexture, depthmapDisplay, depthMat);
                RenderTexture.active = null;
                GUILayout.Box(depthmapDisplay, GUILayout.MaxWidth(screenWidth/5), GUILayout.MaxHeight(screenHeight/5));
            }
        }

        private void UpdateBuffers(){
            totalTimer.Restart();
            //reset instance data counter
            instanceDataBuffer.SetCounterValue(0);

            //set camera matrices and position
            cullingShader.SetMatrix(cs_CamToWorld, mainCam.cameraToWorldMatrix);
            cullingShader.SetMatrix(cs_CamInverseProjection, mainCam.projectionMatrix.inverse);
            cullingShader.SetVector("origin", mainCam.transform.position);

            //reset depth buffer and ray cache
            RenderTexture.active = renderTexture;
            Graphics.Blit(redTexture, renderTexture);
            RenderTexture.active = null;


            //process BVH
            hitsBuffer.SetData(hits);
            foreach(var bvh in BvhBuffers){
                var bbuf = bvh.Value;

                //project outer bounds onto the screen to find the area to test against
                var screenRect = GetScreenRect(bbuf.rootBounds);
                if(!mainCam.pixelRect.Overlaps(screenRect, false))
                    continue;
                screenRect.ClampToRect(mainCam.pixelRect);
                var threadGroupX = Mathf.CeilToInt(screenRect.width/16f);
                var threadGroupY = Mathf.CeilToInt(screenRect.height/16f);

                //set shader variables
                cullingShader.SetInts(cs_PixelOffset, (int)screenRect.min.x, screenHeight-(int)screenRect.max.y);
                cullingShader.SetInt(cs_Root, bbuf.root);
                cullingShader.SetVector(cs_ChunkOffset, bbuf.offset);
                cullingShader.SetBuffer(0, cs_BvhBuffer, bbuf.buffer);

                //dispatch
                cullingShader.Dispatch(0, threadGroupX, threadGroupY, 1); //kernel that gets visible shapes
                cullingShader.Dispatch(1, 16, 16, 16); //kernel that fills the position buffer based on visible shapes
            }

            //update instance count
            ComputeBuffer.CopyCount(instanceDataBuffer, argsBuffer, 4);
            bufferDirty = false;
            totalTimer.Stop();
        }


        //project an AABB onto the viewport and return a Rect that encapsulates it
        private Rect GetScreenRect(Bounds bounds) {
            var cam = mainCam;
    
            var min = cam.WorldToScreenPoint(bounds.min);
            Vector2 max = min;
            var zValues = new float[8]; //track z values. No need to consider a box behind the camera
    
            var point = min;
            min = new Vector2(min.x >= point.x ? point.x : min.x, min.y >= point.y ? point.y : min.y);
            max = new Vector2(max.x <= point.x ? point.x : max.x, max.y <= point.y ? point.y : max.y);
            zValues[0] = point.z;
    
            point = cam.WorldToScreenPoint(new Vector3(bounds.max.x, bounds.min.y, bounds.min.z));
            min = new Vector2(min.x >= point.x ? point.x : min.x, min.y >= point.y ? point.y : min.y);
            max = new Vector2(max.x <= point.x ? point.x : max.x, max.y <= point.y ? point.y : max.y);
            zValues[1] = point.z;
    
    
            point = cam.WorldToScreenPoint(new Vector3(bounds.min.x, bounds.min.y, bounds.max.z));
            min = new Vector2(min.x >= point.x ? point.x : min.x, min.y >= point.y ? point.y : min.y);
            max = new Vector2(max.x <= point.x ? point.x : max.x, max.y <= point.y ? point.y : max.y);
            zValues[2] = point.z;
    
            point = cam.WorldToScreenPoint(new Vector3(bounds.max.x, bounds.min.y, bounds.max.z));
            min = new Vector2(min.x >= point.x ? point.x : min.x, min.y >= point.y ? point.y : min.y);
            max = new Vector2(max.x <= point.x ? point.x : max.x, max.y <= point.y ? point.y : max.y);
            zValues[3] = point.z;
    
            point = cam.WorldToScreenPoint(new Vector3(bounds.min.x, bounds.max.y, bounds.min.z));
            min = new Vector2(min.x >= point.x ? point.x : min.x, min.y >= point.y ? point.y : min.y);
            max = new Vector2(max.x <= point.x ? point.x : max.x, max.y <= point.y ? point.y : max.y);
            zValues[4] = point.z;
    
            point = cam.WorldToScreenPoint(new Vector3(bounds.max.x, bounds.max.y, bounds.min.z));
            min = new Vector2(min.x >= point.x ? point.x : min.x, min.y >= point.y ? point.y : min.y);
            max = new Vector2(max.x <= point.x ? point.x : max.x, max.y <= point.y ? point.y : max.y);
            zValues[5] = point.z;
    
            point = cam.WorldToScreenPoint(new Vector3(bounds.min.x, bounds.max.y, bounds.max.z));
            min = new Vector2(min.x >= point.x ? point.x : min.x, min.y >= point.y ? point.y : min.y);
            max = new Vector2(max.x <= point.x ? point.x : max.x, max.y <= point.y ? point.y : max.y);
            zValues[6] = point.z;
    
            point = cam.WorldToScreenPoint(bounds.max);
            min = new Vector2(min.x >= point.x ? point.x : min.x, min.y >= point.y ? point.y : min.y);
            max = new Vector2(max.x <= point.x ? point.x : max.x, max.y <= point.y ? point.y : max.y);
            zValues[7] = point.z;
    
            return Mathf.Max(zValues) < 0 ? Rect.zero : new Rect(min.x, screenHeight-max.y, max.x - min.x, max.y - min.y);
        }


        //Add a chunk and generate a BVH for it
        public void AddChunk(ChunkId id){
            var chunk = world.chunks[id];
            var primitives = new List<IPrimitive>();
            var count = 0;
            for (var x = 0; x < 128; x++)
                for (var y = 0; y < 128; y++)
                    for (var z = 0; z < 128; z++) {
                        var voxel = chunk[x,y,z];
                        if (voxel == 0)
                            continue;
                        count++;
                        var pos = new Vector3Int(x,y,z);
                        primitives.Add(new BPrim(pos, voxel));
                    }

            Debug.Log("count: " + count);
            var tree = TreeBuilder.BuildTree128(primitives);
            BvhBuffers.Add(id, new BvhBuffer(tree, id.ChunkPosition*128));
            Debug.Log($"{id} had {count} solid blocks which resulted in a tree of {tree.Count} nodes");
            bufferDirty = true;
        }

        public void OrderTrees(){
            BvhBuffers = BvhBuffers.OrderBy(tree => Vector3.Distance(mainCam.transform.position, tree.Value.offset)).ToDictionary((t => t.Key), (t => t.Value));
            lastSortPos = mainCam.transform.position;
        }

        public void CalculateMemoryUse(){
            nodeCount = BvhBuffers.Sum(tree => tree.Value.nodeCount);
            memoryUsage = nodeCount*24/1024/1024;
        }

        public void RemoveChunk(ChunkId id){
            BvhBuffers[id].buffer.Dispose();
            BvhBuffers.Remove(id);
        }

        private void PrepareShaders(){
            //get property IDs for frequently accessed data
            cs_BvhBuffer = Shader.PropertyToID("BvhBuffer");
            cs_CamInverseProjection = Shader.PropertyToID("_CamInverseProjection");
            cs_CamToWorld = Shader.PropertyToID("_CamToWorld");
            cs_ChunkOffset = Shader.PropertyToID("chunkOffset");
            cs_PixelOffset = Shader.PropertyToID("pixelOffset");
            cs_Root = Shader.PropertyToID("root");

            //setup screen dimensions
            screenWidth = mainCam.pixelWidth;
            screenHeight = mainCam.pixelHeight;
            cullingShader.SetInt("width", screenWidth);
            cullingShader.SetInt("height", screenHeight);

            //prepare buffer with args for the instancing shader
            args = new[] {
                instanceMesh.GetIndexCount(0),
                (uint)0,
                instanceMesh.GetIndexStart(0),
                instanceMesh.GetBaseVertex(0),
                (uint)0
            };
            argsBuffer = new ComputeBuffer(5, sizeof(uint), ComputeBufferType.IndirectArguments);
            argsBuffer.SetData(args);

            //Prepare textures
            blocksManager.GenerateTexture();
            instanceMaterial.SetTexture(Textures, blocksManager.GenerateTextureArray());

            //prepare the instance data buffer
            instanceDataBuffer?.Release();
            instanceDataBuffer = new ComputeBuffer(500000, 16, ComputeBufferType.Append);
            instanceDataBuffer.name = "InstanceDataBuffer";
            instanceMaterial.SetBuffer(InstanceDataBuffer, instanceDataBuffer);
            cullingShader.SetBuffer(1, InstanceDataBuffer, instanceDataBuffer);

            //create the initial depthbuffer texture, and prepare the renderTexture
            redTexture = new Texture2D(screenWidth, screenHeight, TextureFormat.RFloat, 0, false);
            var pixelData = Enumerable.Repeat(1000f, screenWidth*screenHeight).ToArray();
            redTexture.SetPixelData(pixelData, 0);
            redTexture.Apply();

            renderTexture = new RenderTexture(screenWidth, screenHeight, 0, RenderTextureFormat.RFloat);
            renderTexture.name = "depthBuffer";
            renderTexture.enableRandomWrite = true;
            renderTexture.Create();
            cullingShader.SetTexture(0, "depthBuffer", renderTexture);

            //prepare the hits buffer
            hitsBuffer?.Release();
            hitsBuffer = new ComputeBuffer(hits.Length, 4);
            hitsBuffer.name = "HitsBuffer";
            cullingShader.SetBuffer(0, "HitsBuffer", hitsBuffer);
            cullingShader.SetBuffer(1, "HitsBuffer", hitsBuffer);

        }

        private Vector2[] GenerateTextureData(Vector2 tilesize){
            var ntex = blocksManager.blockTypes.Count;
            var result = new List<Vector2>();
            for(ushort i = 0; i<ntex;i++){
                result.AddRange(blocksManager.GetBlockUvs(i));
            }
            return result.ToArray();
        }

        private void OnDestroy() {
            argsBuffer?.Dispose();
            instanceDataBuffer?.Dispose();
            hitsBuffer?.Dispose();
            bvhBuffer?.Dispose();
            foreach(var b in BvhBuffers){
                b.Value.buffer.Dispose();
            }
        }
    }

}