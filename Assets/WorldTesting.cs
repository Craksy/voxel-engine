using System.Collections;
using System.Collections.Generic;
using System.IO;
using Source.Vox;
using UnityEngine;

using Vox;
using Vox.WorldGeneration;

public class WorldTesting : MonoBehaviour
{
    public Mesh cube;
    public Material material;
    private World world;
    // Start is called before the first frame update
    void Start()
    {
        GridManager.ChunkShape = new Vector3Int(64,64,64);
        SaveManager.CurrentSavePath = Path.Combine(SaveManager.SavesBasePath, "Actual64");
        SaveManager.voxelStride = 1;
        var conf = new HillWorldConfig{
            Freqency = 0.15f,
            Amplitude = 0.8f,
            Persistence = 0.1f,
        };
        world = new World();
        for(int x=0; x<10; x++){
            for(int y=0; y<2; y++){
                for(int z=0; z<10; z++){
                    world.LoadChunk(new Vector3Int(x,y,z));
                }
            }
        }
        WorldGeneration.GenerateSurface2(world, Vector2Int.zero, conf);
        for(int x=0; x<10; x++){
            for(int y=0; y<2; y++){
                for(int z=0; z<10; z++){
                    world.Unload(new Vector3Int(x,y,z));
                }
            }
        }
    }

    private void RenderChunk(Chunk chunk){
        int count = 0;
        int totalcount = 0;
        for(int i = 0; i < chunk.voxels.Length; i++){
            var pos = GridManager.IndexToBlock(i);
            totalcount++;
            if(chunk[pos.x, pos.y, pos.z] != 0){
                var obj = new GameObject($"voxel {pos}", typeof(MeshRenderer), typeof(MeshFilter));
                obj.transform.position = pos + Vector3.one*0.5f;
                obj.GetComponent<MeshFilter>().mesh = cube;
                count++;
            }
        }

        Debug.Log($"render chunk of {count} voxels");
        Debug.Log($"total: {totalcount} voxels");
    }

    void Update()
    {
    }
}
