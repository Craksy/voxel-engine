using System.Collections.Generic;
using UnityEditor.UI;
using UnityEngine;
using Vox;

namespace Source.Vox {
    public class World
    {
        public Dictionary<ChunkId, Chunk> chunks = new Dictionary<ChunkId, Chunk>();

        public byte this[int x, int y, int z]
        {
            get => chunks[GridManager.ChunkIdFromWorld(x,y,z)][GridManager.WorldToBlock(x,y,z)];
            set {
                try {
                    chunks[GridManager.ChunkIdFromWorld(x,y,z)][GridManager.WorldToBlock(x,y,z)] = value;
                }
                catch (System.Exception e) {
                    Debug.Log($"Failed to set block at {x},{y},{z} ({GridManager.ChunkIdFromWorld(x,y,z)})");
                    Debug.Log($"Number of chunks: {chunks.Count}");
                    Debug.Log(chunks.Keys.ToString());
                    throw;
                }
            }
        }

        public void Unload(Vector3Int pos) => Unload(pos.x, pos.y, pos.z);
        
        public void Unload(int x, int y, int z) {
            var id = new ChunkId(x,y,z);
            SaveManager.SaveChunk(new Vector3Int(x,y,z), chunks[id].voxels);
            chunks.Remove(id);
        }

        public Chunk LoadChunk(Vector3Int pos){
            var voxels = SaveManager.LoadChunk(pos);
            var chunk = new Chunk();
            chunk.voxels = voxels;
            chunks.Add(new ChunkId(pos), chunk);
            return chunk;
        }
    }
}