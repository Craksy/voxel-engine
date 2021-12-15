using System;
using System.Collections;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

namespace Vox.Octree
{
    public static class OctreeBuilder{
        private static int DepthToSize(int logSize, int depth){
            return (int)Mathf.Pow(2, logSize-depth);
        }

        private static BoundsInt PositionToBounds(Vector3Int position, int depth){
            return new BoundsInt(position, Vector3Int.one*DepthToSize(6, depth));
        }

        private static Vector3Int DataToPoint(uint val) => new Vector3Int((int)(val & 63), (int)((val>>6)&63), (int)((val>>12)&63));

        public static void Insert2(OPoint2 point, List<ONode3> tree, ushort idx, int depth){
            var node = tree[idx];
            var ind = new String('-', 3*(6-depth));
            Debug.Log($"{ind}inserting point at {idx}, depth {depth}");

            if((node.children.All(c => c==uint.MaxValue) && !node.leaf) || depth == 0){
                Debug.Log($"{ind}leaf. setting current node");
                var val = (uint)(
                    (ushort)(point.position.x & 63) | 
                    (ushort)((point.position.y & 63) << 6) | 
                    (ushort)((point.position.z & 63) << 12) | 
                    (ushort)((point.data << 18)));
                tree[idx] = new ONode3(node.position, node.parent, val);
                return;
            }

            int extent = 1<<(depth-1);
            var pos = point.position;
            Vector3Int offset = new Vector3Int(
                pos.x>=node.position.x+extent ?1:0,
                pos.y>=node.position.y+extent ?1:0,
                pos.z>=node.position.z+extent ?1:0);
            int octant = offset.x<<2|offset.y<<1|offset.z;
            ushort cidx = node.GetChild(octant);
            Debug.Log($"{ind}passing to child at octant {octant}");
            if(cidx == ushort.MaxValue){
                cidx = (ushort)tree.Count;
                tree[idx].SetChild(cidx, octant);
                var child = new ONode3(node.position+offset*extent, idx);
                Debug.Log($"{ind}child didn't exist. creating. cidx: {cidx}");
                tree.Add(child);
            }
            Insert2(point, tree, cidx, depth-1);

            if(node.leaf){
                Debug.Log($"{ind}passing leaf down");
                Insert2(node.Value, tree, idx, depth);
            }
        }

        public static void Insert(OPoint point, ref List<ONodeFlat> tree, ushort idx){
            var node = tree[idx];
            ushort depth = node.depth;

            if((node.children.All(c => c==ushort.MaxValue) && !node.leaf) || depth == 0){
                node.value = point;
                node.leaf = true;
                return;
            }

            int extent = 1<<(depth-1);
            Vector3Int offset = new Vector3Int(
                point.position.x>=node.position.x+extent ?1:0,
                point.position.y>=node.position.y+extent ?1:0,
                point.position.z>=node.position.z+extent ?1:0);
            int octant = offset.x<<2|offset.y<<1|offset.z;
            ushort cidx = node.children[octant];
            if(cidx == ushort.MaxValue){
                cidx = (ushort)tree.Count;
                node.children[octant] = cidx;
                var child = new ONodeFlat(node.position+offset*extent, (ushort)(depth - 1), idx);
                tree.Add(child);
            }
            Insert(point, ref tree, cidx);

            if(node.leaf){
                node.leaf = false;
                Insert(node.value, ref tree, idx);
            }
        }
    }
}