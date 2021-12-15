using UnityEngine;

namespace Vox.Bvh
{

    public interface IBounds{
        Vector3Int Min { get; }
        Vector3Int Max { get; }
    }

    public interface IPrimitive{
        uint id {get;}
        byte type {get;}
        BoundsInt bounds {get;}
    }

    public struct BNode{
        public readonly BBounds bounds;
        public readonly int entry;
        public readonly int exit;
        public readonly int shape;

        public BNode(BBounds b, int ent, int ex, int s){
            bounds = b;
            entry = ent;
            exit = ex;
            shape = s;
        }
    }

    public struct BPrimitive{
        public int id;
        public Bounds bounds;
        public BPrimitive(int pid, Bounds b){
            id = pid;
            bounds = b;
        }
    }

    public struct BBounds {
        public Vector3 min;
        public Vector3 max;
        public Vector3 center => min + (max-min)*0.5f;
        public Vector3 size => max-min;
    }

    public struct Bounds128 : IBounds {
        public uint min;
        public uint max;
        
        public Bounds128(Vector3Int min, Vector3Int max){
            this.min = (uint)(
                (min.x & 255) |
                (min.y & 255) << 8 |
                (min.z & 255) << 16);
            this.max = (uint)(
                (max.x & 255) |
                (max.y & 255) << 8 |
                (max.z & 255) << 16);
        }

        public Vector3Int Min => new Vector3Int((int)min & 255, (int)(min>>8)&255, (int)(min>>16)&255);
        public Vector3Int Max => new Vector3Int((int)max & 255, (int)(max>>8)&255, (int)(max>>16)&255);
        public Vector3Int size => Max - Min;
    }

    public struct Node128{
        public readonly Bounds128 bounds;
        public readonly uint entry;
        public readonly uint exit;
        public readonly uint primitive;
        public readonly uint type;

        public Node128(Bounds128 b, uint entry, uint exit, uint shape, byte type=1){
            bounds = b;
            this.entry = entry;
            this.exit = exit;
            primitive = shape;
            this.type = type;
        }
    }
    
    public struct BPrim : IPrimitive{
        public uint id {get; }
        public BoundsInt bounds {get; }
        public byte type {get; }
        public BPrim(Vector3Int blockPosition, byte blockType = 1){
            id = (uint)GridManager.BlockToIndex(blockPosition);
            bounds = new BoundsInt(blockPosition, Vector3Int.one);
            type = blockType;
        }
    }
}