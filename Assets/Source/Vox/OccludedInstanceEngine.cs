using System.Collections.Generic;
using Source.Vox;
using UnityEngine;
using Vox.Rendering;

namespace Vox {
    public class OccludedInstanceEngine : MonoBehaviour
    {
        public VoxelBlocksManager BlocksManager;
        private Material material;
        public OcclusionRenderer instanceRenderer;
        public World world;
        public Transform playerpos;

        private Vector3Int CurrentChunk => new Vector3Int(
            (int)playerpos.position.x>>7,
            (int)playerpos.position.y>>7,
            (int)playerpos.position.z>>7);
        private Vector3Int lastCenterChunk;
        public int renderDistance = 5;
        private BoundsInt renderBounds;
        private bool loadAround;

        void Start() {
            loadAround = true;
            GridManager.ChunkShape = new Vector3Int(128,128,128);
            SaveManager.voxelStride = 1;
            //SaveManager.CurrentSavePath = Path.Combine(SaveManager.SavesBasePath, "");
            world = new World();

            instanceRenderer.world = world;
            renderBounds = new BoundsInt(Vector3Int.one*-renderDistance, Vector3Int.one*renderDistance*2);
            lastCenterChunk = CurrentChunk;
            renderBounds.position = CurrentChunk - Vector3Int.one*renderDistance;
            instanceRenderer.RenderBounds = new Bounds(renderBounds.center*128, renderBounds.size*128);
            LoadAroundPlayer();

        }

        // Update is called once per frame
        void Update() {
            if (CurrentChunk == lastCenterChunk || !loadAround) return;
            renderBounds.position = CurrentChunk - Vector3Int.one*renderDistance;
            instanceRenderer.RenderBounds = new Bounds(renderBounds.center*128, renderBounds.size*128);
            lastCenterChunk = CurrentChunk;
            LoadAroundPlayer();
        }

        private void LoadAroundPlayer(){
            Debug.Log($"Loading");
            //for every position within render distance, load any chunk that isn't already loaded
            var toLoad = new List<ChunkId>();
            var toUnload = new List<ChunkId>();
            foreach(var pos in renderBounds.allPositionsWithin){
                if(pos.x<0||pos.y<0||pos.z<0)
                    continue;
                var id = new ChunkId(pos.x, pos.y, pos.z);
                if(!world.chunks.ContainsKey(id)){
                    toLoad.Add(id);
                }
            }

            //for every loaded chunk, unload any that is no longer within render distance
            foreach(var id in world.chunks.Keys){
                var pos = id.ChunkPosition;
                if(pos.x<0||pos.y<0||pos.z<0)
                    continue;
                if(!renderBounds.Contains(pos)){
                    toUnload.Add(id);
                }
            }

            instanceRenderer.bvhTimer.Restart();
            foreach(var id in toLoad){
                world.LoadChunk(id.ChunkPosition);
                instanceRenderer.AddChunk(id);
            }
            foreach(var id in toUnload){
                world.Unload(id.ChunkPosition);
                instanceRenderer.RemoveChunk(id);
            }
            instanceRenderer.OrderTrees();
            instanceRenderer.bvhTimer.Stop();

            instanceRenderer.CalculateMemoryUse();
        }
    }
}