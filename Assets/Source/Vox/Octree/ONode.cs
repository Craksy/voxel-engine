using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Vox.Octree
{

    [System.Serializable]
    public struct OPoint2 {
        public uint data;

        public Vector3Int position => new Vector3Int((int)(data & 63), (int)((data>>6)&63), (int)((data>>12)&63));

        public OPoint2(uint value){
            data = value;
        }

        public OPoint2(Vector3Int position, int value){
            if(value < 0)
                data = uint.MaxValue;
            else
                data = (uint)(
                    (position.x & 63) | 
                    (position.y & 63) << 6 | 
                    (position.z&63) << 12 | 
                    (value &255 << 18));
        }
    }

    public struct ONode3{
        public uint[] children;
        public uint value;
        public uint parent;


        public OPoint2 Value {
            get =>new OPoint2(
                new Vector3Int( 
                    (int)(value & 63),
                    (int)((value >> 6) & 63), 
                    (int)((value >> 12) & 63)), 
                (int)(value >> 18));
            set {
                Debug.Log($"setting value. current: {this.value}, new: {value}");
                this.value = (uint)(
                    (ushort)(value.position.x & 63) | 
                    (ushort)((value.position.y & 63) << 6) | 
                    (ushort)((value.position.z & 63) << 12) | 
                    (ushort)((value.data << 18)));
                Debug.Log($"new value: {this.value}");
            }
        }

        public bool leaf => value != uint.MaxValue;

        public Vector3Int position => new Vector3Int(
                    (int)(value & 63),
                    (int)((value >> 6) & 63), 
                    (int)((value >> 12) & 63));

        public ONode3(Vector3Int position, uint parent, uint value = uint.MaxValue){
            children = new uint[4];
            for(int i = 0; i< 4; i++)
                children[i] = uint.MaxValue;
            this.parent = parent;
            this.value = value;
        }

        public void SetChild(uint value, int octant){
           var c = children[octant/2];
           Debug.Log($"{octant} goes into {octant/2}/{octant&1}");
            if ((octant & 1) == 1)
                c = (c & 0xffff) | (value << 16);
            else
                c = (c & 0xffff0000) | (value & 0xffff);
            children[octant/2] = c;
        }

        public ushort GetChild(int octant){
            return (ushort)((children[octant/2] >> (octant&1)*16)&0xffff);
        }
    }

    [System.Serializable]
    public class ONode2{
        public uint[] children = new uint[8];
        public uint _value;
        public OPoint2 value {
            get {
                return new OPoint2(new Vector3Int((int)(_value & 63), (int)((_value>>6)&63), (int)((_value>>12)&63)), (int)(_value >> 18));
            }
            set {
                _value = (uint)((ushort)(value.position.x&63) | (ushort)((value.position.y&63)<<6) | (ushort)((value.position.z&63) << 12) | (ushort)((value.data << 18)));
            }
        }
        public uint parent;
        [System.NonSerialized] public Vector3Int position;
        public bool leaf => value.data != uint.MaxValue;
        // public Vector3Int ValuePosition => new Vector3Int((int)(value & 63), (int)((value>>6)&63), (int)((value>>12)&63));

        public ONode2(Vector3Int position, uint parent) {
            this.position = position;
            for(int i = 0; i<8;i++ ){
            }
            this.parent = parent;
            this.value = new OPoint2(uint.MaxValue);
        }

        public void SetChild(uint value, int octant){
           var c = children[octant/2];
           Debug.Log($"{octant} goes into {octant/2}/{octant&1}");
            if ((octant & 1) == 1)
                c = (c & 0xffff) | (value << 16);
            else
                c = (c & 0xffff0000) | (value & 0xffff);
            children[octant/2] = c;
        }

        public ushort GetChild(int octant){
            return (ushort)((children[octant/2] >> (octant&1)*16)&0xffff);
        }
    }

    public struct OPoint{ //reduce to 1 int
        public Vector3Int position;
        public int data;
    }

    public class ONodeFlat{
        public Vector3Int position; // Reduce to 1 int
        public ushort[] children; // reduce to 4 ints
        public OPoint value; // 1 int
        public ushort parent; //1 int combined
        public bool leaf;
        [System.NonSerialized]public ushort depth;

        public ONodeFlat(Vector3Int position, ushort depth, ushort parent){
            this.position = position;
            this.depth = depth;
            this.children = Enumerable.Repeat(ushort.MaxValue, 8).ToArray();
            this.parent = parent;
        }
    }

    public class ONode {
        public BoundsInt bounds;
        public ONode[] children;
        public OPoint value;
        public bool leaf;

        public ONode(BoundsInt bounds){
            this.bounds = bounds;
            children = new ONode[8];
        }

        public void Insert(OPoint point){
            if(!bounds.Contains(point.position))
                return;
    
            if((children.All(c => c==null) && !leaf) || bounds.size == Vector3Int.one){
                value = point;
                leaf = true;
                return;
            }

            Vector3Int offset = new Vector3Int(
                point.position.x >= bounds.center.x ? 1 : 0,
                point.position.y >= bounds.center.y ? 1 : 0,
                point.position.z >= bounds.center.z ? 1 : 0);
            int octant = offset.x<<2|offset.y<<1|offset.z;
            if(children[octant] == null){
                Vector3Int extents = bounds.size/2;
                Vector3Int cp = bounds.position + extents*offset;
                children[octant] = new ONode(new BoundsInt(bounds.position+extents*offset, extents));
            }

            children[octant].Insert(point);
            if(leaf){
                leaf = false;
                Insert(value);
            }
        }
    }
}