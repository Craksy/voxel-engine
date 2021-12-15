using System.Collections.Generic;
using System.Linq;
using Source.Vox;
using UnityEngine;

using Vox.Rendering;

namespace Vox {

public class InstancedVoxelEngine : MonoBehaviour
{
    public VoxelBlocksManager BlocksManager;
    private Material material;
    public InstanceRenderer instanceRenderer;
    public World world;
    public Transform playerpos;

    private Vector3Int CurrentChunk => new Vector3Int((int)playerpos.position.x>>4,(int)playerpos.position.y>>4,(int)playerpos.position.z>>4);
    private Vector3Int lastCenterChunk;
    private readonly int renderDistance = 12;
    private Queue<Vector3Int> renderQueue;
    private Queue<Vector3Int> unloadQueue;
    private BoundsInt renderBounds;

    // Start is called before the first frame update
    void Start()
    {
        renderQueue = new Queue<Vector3Int>();
        unloadQueue = new Queue<Vector3Int>();
        world = new World();
        instanceRenderer.world = world;
        renderBounds = new BoundsInt(Vector3Int.one*-renderDistance, Vector3Int.one*renderDistance*2);
        lastCenterChunk = CurrentChunk;
        renderBounds.position = CurrentChunk - Vector3Int.one*renderDistance;
        instanceRenderer.RenderBounds = new Bounds(renderBounds.center*16, renderBounds.size*16);
        LoadAroundPlayer();
    }

                    //o.uv = TRANSFORM_TEX(v.uv, _MainTex)*tileSize + UvBuffer[TypeBuffer[instanceId]+idx];
    // Update is called once per frame
    void Update()
    {
        if(CurrentChunk != lastCenterChunk){
            renderBounds.position = CurrentChunk - Vector3Int.one*renderDistance;
            instanceRenderer.RenderBounds = new Bounds(renderBounds.center*16, renderBounds.size*16);
            lastCenterChunk = CurrentChunk;
            LoadAroundPlayer();
        }

        var rqc = renderQueue.Count;
        int rpc = 0;
        if(rqc > 5000)
            rpc = rqc - 5000;
        else if(rqc > 2500)
            rpc = rqc - 2500;
        else if(rqc > 500)
            rpc = 20;
        else if(rqc > 0)
            rpc = 1;
            //rpc = Mathf.Min(rqc, 15);

        for(int i =0; i< rpc;i++)
            ProcessRenderQueue();


        rqc = unloadQueue.Count;
        rpc = 0;
        if(rqc > 5000)
            rpc = 50;
        else if(rqc > 2500)
            rpc = 25;
        else if(rqc > 0)
            rpc = Mathf.Min(rqc, 10);

        for(int i =0; i< rpc;i++)
            ProcessUnloadQueue();

    }

    private void ProcessRenderQueue(){
        var pos = renderQueue.Dequeue();
        var id = new ChunkId(pos);
        if(!world.chunks.ContainsKey(id)){
            world.LoadChunk(pos);
            instanceRenderer.AddChunk(id);
        }
    }

    private void ProcessUnloadQueue(){
        if(unloadQueue.Count < 1)
            return;
        var pos = unloadQueue.Dequeue();
        var id = new ChunkId(pos);
        if(world.chunks.ContainsKey(id)){
            instanceRenderer.RemoveChunk(id);
            world.Unload(pos);
        }
    }

    private void OnGUI() {
        GUI.Label(new Rect(0,60,200,30), $"Render Queue: <b>{renderQueue.Count}</b>");
        GUI.Label(new Rect(0,90,200,30), $"Unload Queue: <b>{unloadQueue.Count}</b>");
    }

    private void LoadAroundPlayer(){
        Debug.Log("loading around");
        foreach(var pos in renderBounds.allPositionsWithin){
            if(pos.x<0||pos.y<1||pos.z<0||pos.y>6)
                continue;
            ChunkId id = new ChunkId(pos.x, pos.y, pos.z);
            if(!world.chunks.ContainsKey(id)){
                renderQueue.Enqueue(pos);
            }
        }
        renderQueue = new Queue<Vector3Int>(renderQueue.OrderBy(pos => Vector3Int.Distance(pos, lastCenterChunk)));
        if(renderQueue.Count > 5000){
            renderQueue = new Queue<Vector3Int>(renderQueue.Where(pos => renderBounds.Contains(pos)));
        }

        Vector3Int pos2;
        foreach(var id in world.chunks.Keys){
            pos2 = id.ChunkPosition;
            if(pos2.x<0||pos2.y<0||pos2.z<0||pos2.y>6)
                continue;
            if(!renderBounds.Contains(pos2)){
                unloadQueue.Enqueue(pos2);
            }
        }
        unloadQueue = new Queue<Vector3Int>(unloadQueue.OrderBy(pos => Vector3Int.Distance(pos, lastCenterChunk)));
    }
}

}