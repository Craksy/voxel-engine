using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Source.Vox;
using UnityEngine;

using Vox.Rendering;

namespace Vox {

    public class VoxelEngine : MonoBehaviour
    {
        public VoxelBlocksManager BlocksManager;
        public Material material;
        public ChunkRenderer chunkRenderer;
        public Transform playerpos;

        private World world;
        private Vector3Int CurrentChunk => new Vector3Int(
            (int)playerpos.position.x>>7,
            (int)playerpos.position.y>>7,
            (int)playerpos.position.z>>7);
        private Vector3Int lastCenterChunk;
        private const int RenderDistance = 5;
        private Queue<Vector3Int> renderQueue;
        private Queue<Vector3Int> unloadQueue;
        private BoundsInt renderBounds;

        private Vector3Int debugGizmoCube;

        private Queue<float> fpsList;

        private void Start()
        {
            world = new World();
            chunkRenderer.world = world;
            fpsList = new Queue<float>(60);
            renderQueue = new Queue<Vector3Int>();
            unloadQueue = new Queue<Vector3Int>();
            BlocksManager.GenerateTexture();
            renderBounds = new BoundsInt(Vector3Int.one*-RenderDistance, Vector3Int.one*RenderDistance*2);
            lastCenterChunk = CurrentChunk;
            renderBounds.position = CurrentChunk - Vector3Int.one*RenderDistance;

            PositionPlayer();
            LoadAroundPlayer();
            while(world.chunks.Count < 50){
                ProcessRenderQueue();
            }
        }

        private void PositionPlayer() {
            if (!Physics.Raycast(playerpos.position, Vector3.down, out var hit, 100,
                LayerMask.NameToLayer("Ground"))) return;
            Debug.Log("Position player: " + hit.point);
            var position = playerpos.position;
            playerpos.position = new Vector3(position.x, hit.point.y +1, position.z);
        }

        public void PlayerHitBlock(RaycastHit hit){
            var point = HitToBlock(hit.point - hit.normal*.4f);
            world[point.x, point.y, point.z] = 0;
            RedrawChunkAndNeighbors(point);
        }

        public void PlayerPlaceBlock(RaycastHit hit, byte block){
            var point = HitToBlock(hit.point + hit.normal*.4f);
            world[point.x, point.y, point.z] = block;
            RedrawChunkAndNeighbors(point);
        }

        public void RedrawChunkAndNeighbors(Vector3Int worldpos){
            var id = GridManager.ChunkIdFromWorld(worldpos);
            chunkRenderer.AddChunk(id);
            for(var i = 0; i<6; i++){
                Vector3Int checkpos = worldpos + CubeMeshData.DirectionToPosition[i];
                ChunkId check = GridManager.ChunkIdFromWorld(checkpos);
                if (check == id) continue;
                chunkRenderer.AddChunk(check);
            }
        }

        public void HighlightBlock(RaycastHit hit){
            var point = HitToBlock(hit.point + hit.normal*.4f);
            debugGizmoCube = Vector3Int.RoundToInt(point);
        }

        private Vector3Int HitToBlock(Vector3 point) => Vector3Int.RoundToInt(point);

        private void OnDrawGizmos() {
            Gizmos.color = new Color(0, 1, 0, 0.5f);
            Gizmos.DrawCube(debugGizmoCube, Vector3.one*1.1f);
        }

        private void Update(){
            if(CurrentChunk != lastCenterChunk){
                renderBounds.position = CurrentChunk - Vector3Int.one*RenderDistance;
                lastCenterChunk = CurrentChunk;
                LoadAroundPlayer();
            }
            ProcessRenderQueue();
            ProcessUnloadQueue();
            fpsList.Enqueue(1f/Time.deltaTime);
        }

        private void ProcessRenderQueue(){
            var count = renderQueue.Count > 5 ? 5 : renderQueue.Count;

            for(var i =0; i< count;i++){
                var pos = renderQueue.Dequeue();
                var id = new ChunkId(pos.x, pos.y, pos.z);
                if (world.chunks.ContainsKey(id)) continue;
                chunkRenderer.AddChunk(id);
            }
        }

        private void ProcessUnloadQueue(){
            var count = unloadQueue.Count > 5 ? 5 : unloadQueue.Count;

            for(var i =0; i< count; i++){
                var pos = unloadQueue.Dequeue();
                var id = new ChunkId(pos.x, pos.y, pos.z);
                if (!world.chunks.ContainsKey(id)) continue;
                world.Unload(pos);
                chunkRenderer.RemoveChunk(id);
            }
        }

        private void OnGUI() {
            GUI.Label(new Rect(0,0,150,50), "Chunks: "+world.chunks.Count);
            GUI.Label(new Rect(0,30,150,50), "Pos: " + CurrentChunk);
            GUI.Label(new Rect(0,60,150,50), "Render: "+renderQueue.Count);
            GUI.Label(new Rect(0,90,150,50), "Unload: "+unloadQueue.Count);
            GUI.Label(new Rect(0,120,150,50), "FPS: "+ fpsList.Average());
        }

        private void LoadAroundPlayer(){
            var timer = new System.Diagnostics.Stopwatch();
            timer.Start();
            foreach(var pos in renderBounds.allPositionsWithin){
                if(pos.x<0||pos.y<0||pos.z<0||pos.y>6)
                    continue;
                var id = new ChunkId(pos.x, pos.y, pos.z);
                if(!renderQueue.Contains(pos) && !world.chunks.ContainsKey(id)){
                    renderQueue.Enqueue(pos);
                }
            }
            renderQueue = new Queue<Vector3Int>(renderQueue.OrderBy(v => Vector3Int.Distance(lastCenterChunk, v)));

            Vector3Int pos2;
            foreach(var id in world.chunks.Keys){
                pos2 = new Vector3Int(id.X, id.Y, id.Z);
                if(pos2.x<0||pos2.y<0||pos2.z<0||pos2.y>6)
                    continue;
                if(!renderBounds.Contains(new Vector3Int(id.X, id.Y, id.Z))){
                    unloadQueue.Enqueue(pos2);
                }
            }
            timer.Stop();
        }
    }

}