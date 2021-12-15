using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Source.Vox;
using UnityEngine;

namespace Vox.Rendering{
    public class InstanceRenderer : MonoBehaviour
    {
        public GameObject Template;
        public VoxelBlocksManager blocksManager;
        public Material instanceMaterial;
        public World world;
        public bool bufferDirty;

        public bool ShowStats;

        private Mesh instanceMesh;

        private ComputeBuffer argsBuffer, positionBuffer, uvBuffer, typeBuffer;
        private uint[] args = {0, 0, 0, 0, 0};
        private int instanceCount;
        private int cachedInstanceCount;

        private int chunkCount;
        private Dictionary<ChunkId, BufferRange> bufferOffsets = new Dictionary<ChunkId, BufferRange>();

        private List<Vector3> positions = new List<Vector3>(5000000);
        private List<uint> types = new List<uint>(500000);

        private int blockCounter = 0;

        private Dictionary<ushort, Vector2[]> UvCache;
        private System.Diagnostics.Stopwatch airtimer;
        private System.TimeSpan elapsedAir;

        private Bounds _renderBounds;
        public Bounds RenderBounds {
            get => _renderBounds;
            set {
                _renderBounds = value;
                chunkCount = (int)_renderBounds.size.x * (int)_renderBounds.size.y * (int)_renderBounds.size.z;
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            //RenderBounds = new Bounds(Vector3.one*50, Vector3.one*100f);
            instanceMesh = CubeMeshData.CreateMappedCube();
            PrepareMaterial();
            bufferDirty = true;
            argsBuffer = new ComputeBuffer(1, 5*sizeof(uint), ComputeBufferType.IndirectArguments);
        }

        // Update is called once per frame
        void Update()
        {
            if(bufferDirty || cachedInstanceCount != instanceCount)
                UpdateBuffer();
            if(instanceCount > 1)
                Graphics.DrawMeshInstancedIndirect(instanceMesh, 0, instanceMaterial, RenderBounds, argsBuffer);
        }


        private void PrepareMaterial(){
            blocksManager.GenerateTexture();
            var uvs = GenerateTextureData(new Vector2(blocksManager.tileWidth, blocksManager.tileHeight));
            uvBuffer?.Release();
            uvBuffer = new ComputeBuffer(uvs.Length, 8);
            uvBuffer.name = "Uv Buffer";
            uvBuffer.SetData(uvs);
            instanceMaterial.SetFloat("_TileWidth", blocksManager.tileWidth);
            instanceMaterial.SetFloat("_TileHeight", blocksManager.tileHeight);
            //instanceMaterial.mainTexture = blocksManager.TextureSheet;
            instanceMaterial.SetTexture("_Textures", blocksManager.GenerateTextureArray());
            Debug.Log(uvs[2*6+5]);
        }

        private Vector2[] GenerateTextureData(Vector2 tilesize){
            int ntex = blocksManager.blockTypes.Count;
            List<Vector2> result = new List<Vector2>();
            for(ushort i = 0; i<ntex;i++){
                result.AddRange(blocksManager.GetBlockUvs(i));
            }
            return result.ToArray();
        }

        //Update compute buffers with procedural mesh data.
        private void UpdateBuffer(){
            instanceCount = blockCounter;
            if(instanceCount < 1)
                return;

            positionBuffer?.Release();
            positionBuffer = new ComputeBuffer(positions.Count, 12);
            positionBuffer.name = "Position Buffer";
            positionBuffer.SetData(positions);
            
            typeBuffer?.Release();
            typeBuffer = new ComputeBuffer(types.Count, 4);
            typeBuffer.name = "Types Buffer";
            typeBuffer.SetData(types);


            instanceMaterial.SetBuffer("PositionBuffer", positionBuffer);
            instanceMaterial.SetBuffer("TypeBuffer", typeBuffer);
            //instanceMaterial.SetBuffer("TexBuffer", uvBuffer);


            if(instanceMesh != null){
                args[0] = (uint)instanceMesh.GetIndexCount(0);
                args[1] = (uint)instanceCount;
                args[2] = (uint)instanceMesh.GetIndexStart(0);
                args[3] = (uint)instanceMesh.GetBaseVertex(0);
            }else{
                args[0] = args[1] = args[2] = args[3] = 0;
            }

            argsBuffer.SetData(args);
            cachedInstanceCount = instanceCount;
            bufferDirty = false;
        }

        private Vector2[] UvsThroughCache(ushort blocktype){
            Vector2[] uvs;
            if(!UvCache.TryGetValue(blocktype, out uvs)){
                uvs = blocksManager.GetBlockUvs(blocktype).ToArray();;
                UvCache[blocktype] = uvs;
            }
            return uvs;
        }


        //Add a new chunk to be rendered
        public void AddChunk(ChunkId id){
            if(bufferOffsets.ContainsKey(id)){
                RemoveChunk(id);
                Debug.Log("removed chunk first");
            }

            var chunkPos = GridManager.ChunkToWorld(id);
            var chunk = world.chunks[id];
            int startidx = blockCounter;
            int size = 0;
            for (int x = 0; x < 16; x++)
                for (int y = 0; y < 16; y++)
                    for (int z = 0; z < 16; z++) {
                        var voxel = chunk[x,y,z];
                        if((ushort)voxel == 0)
                            continue;
                        positions.Add(new Vector3(x,y,z)+chunkPos);
                        types.Add(voxel);
                        size++;
                        blockCounter++;
                    }
            bufferOffsets.Add(id, new BufferRange(startidx, size)); 
            bufferDirty = true;
        }


        public void RemoveChunk(ChunkId id){
            BufferRange range; //range is buffer offset and size
            if(bufferOffsets.TryGetValue(id, out range)){ //grab the range if the chunk is being rendered
                bufferOffsets.Remove(id); 
                if(range.Size < 1)
                    return;
                blockCounter -= range.Size;

                //Actually modify the buffers
                positions.RemoveRange(range.Index, range.Size); //remove that range from the buffers
                types.RemoveRange(range.Index, range.Size); //there are 6 uv coords per instance.

                //finally remove the entry from the indices(range) dict, and shift all indices above it by it's size.
                var shift = from idx in bufferOffsets where idx.Value.Index >= range.End select idx.Key;
                foreach(var chunkid in shift){
                    bufferOffsets[chunkid].Shift(range.Size);
                }
                bufferDirty = true;
            }
        }

        private void OnGUI() {
            if(!ShowStats)
                return;
            GUI.Label(new Rect(0,0,200,30), $"Instances: <b>{instanceCount:N0}</b>");
            GUI.Label(new Rect(0,30,200,30), $"Chunks: <b>{bufferOffsets.Count}</b>");
            //GUI.Label(new Rect(0,150,200,30), $"Air: <b>{elapsedAir.Seconds:00}:{elapsedAir.Milliseconds:000}</b>");
        }


        private void OnDisable() {
            positionBuffer?.Release();
            positionBuffer = null;
            typeBuffer?.Release();
            typeBuffer = null;
            uvBuffer?.Release();
            uvBuffer = null;
            argsBuffer?.Release();
            argsBuffer = null;
        }
    }

    public class BufferRange {
        public int Index {get; private set;}
        public int Size {get; private set;}
        public int End => Index+Size;

        public BufferRange(int start, int size) {
            Index = start;
            Size = size;
        }

        public void Shift(int offset){
            Index -= offset;
        }
    }
}