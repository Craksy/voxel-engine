using System.Collections;
using System.Collections.Generic;
using Source.Vox;
using UnityEngine;

namespace Vox.Rendering
{
    public class ChunkRenderer : MonoBehaviour, IVoxelRenderer
    {
        public VoxelBlocksManager BlocksManager;
        public Material material;

        public World world;

        private Dictionary<ChunkId, Transform> chunkMeshes = new Dictionary<ChunkId, Transform>();


        public Mesh RenderChunk(Chunk chunk, Vector3 offset) {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var uvs = new List<Vector2>();

            for(int x = 0; x<GridManager.chunkWidth;x++)
                for(int y = 0; y<GridManager.chunkHeight;y++)
                    for(int z = 0; z<GridManager.chunkDepth;z++){
                        if(chunk[x,y,z] == 0) 
                            continue;
                        CreateCube(new Vector3(x,y,z), offset, chunk, vertices, triangles, uvs);
                    }

            return MakeMesh(chunk, vertices, triangles, uvs);
        }

        private int GetNeighbor(Vector3 position, Chunk chunk, int direction){
            Vector3Int npos = Vector3Int.FloorToInt(position) + CubeMeshData.DirectionToPosition[direction];

            if(world.chunks.ContainsKey(GridManager.ChunkIdFromWorld(npos.x,npos.y,npos.z)))
                return world[npos.x, npos.y, npos.z];
            return 0;
        }


        private void CreateCube(Vector3 position, Vector3 offset, Chunk chunk, List<Vector3> vertices, List<int> triangles, List<Vector2> uvs){
            for (var i = 0; i < 6; i++) {
                var n = GetNeighbor(position+offset, chunk, i);
                var type = BlocksManager.blockTypes[chunk[(int)position.x, (int)position.y, (int)position.z]];
                if (n != 0) continue;
                CreateQuad(position, i, vertices, triangles);
                uvs.AddRange(BlocksManager[type[i]]);
            }
        }

        private void CreateQuad(Vector3 position, int direction, List<Vector3> vertices, List<int> triangles){
            vertices.AddRange(CubeMeshData.GetFaceVertices(direction, position));
            int vcount = vertices.Count;

            triangles.Add(vcount-4); //0
            triangles.Add(vcount-2); //2
            triangles.Add(vcount-1); //3
            triangles.Add(vcount-4); //0
            triangles.Add(vcount-1); //3
            triangles.Add(vcount-3); //1
        }

        private Mesh MakeMesh(Chunk chunk, List<Vector3> vertices, List<int> triangles, List<Vector2> uvs){
            Mesh mesh = new Mesh();
            mesh.Clear();
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.RecalculateNormals();
            return mesh;
        }

        public void AddChunk(ChunkId id)
        {
            Debug.Log("adding chunk " + id);
            var pos = id.ChunkPosition;
            var chunk = world.LoadChunk(pos);
            var chunkObj = new GameObject($"Chunk ({id.X},{id.Y},{id.Z}", typeof(MeshRenderer), typeof(MeshFilter));
            chunkObj.GetComponent<MeshFilter>().mesh = RenderChunk(chunk, GridManager.ChunkToWorld(pos));
            chunkObj.GetComponent<MeshRenderer>().material.mainTexture = BlocksManager.TextureSheet;
            chunkMeshes.Add(id, chunkObj.transform);
        }

        public void RemoveChunk(ChunkId id)
        {
            Destroy(chunkMeshes[id].gameObject);
            chunkMeshes.Remove(id);
        }
    }

}