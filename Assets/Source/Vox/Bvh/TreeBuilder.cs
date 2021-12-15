using System.Collections.Generic;
using UnityEngine;

namespace Vox.Bvh
{
    public static class TreeBuilder
    {

        public static List<Node128> BuildTree128(List<IPrimitive> primitives){
            List<Node128> tree = new List<Node128>();
            CreateNode128(uint.MaxValue, primitives, tree);
            return tree;
        }

        private static BoundsInt PartitionBounds(BoundsInt b){
            Vector3Int ma = Vector3Int.right;
            if(b.size.y > b.size.x)
                ma = b.size.y > b.size.z ? Vector3Int.up : Vector3Int.forward;
            else
                ma = b.size.x > b.size.z ? Vector3Int.right : Vector3Int.forward;
            
            return new BoundsInt(b.position, b.size - Vector3Int.Scale(b.size, ma)/2);
        }

        private static BoundsInt GetBounds(List<IPrimitive> primitives){
            if(primitives.Count<1)
                return new BoundsInt();
            var min = primitives[0].bounds.min;
            var max = primitives[0].bounds.max;
            foreach(var p in primitives){
                min = Vector3Int.Min(min, p.bounds.min);
                max = Vector3Int.Max(max, p.bounds.max);
            }
            return new BoundsInt(min, max-min);
        }

        private static void CreateNode128(uint exit, List<IPrimitive> primitives, List<Node128> tree){
            BoundsInt b = GetBounds(primitives);
            Bounds128 bb = new Bounds128(b.min, b.max);
            if(primitives.Count<2){
                var prim = primitives[0];
                Node128 node = new Node128(bb, uint.MaxValue, exit, prim.id, prim.type);
                tree.Add(node);
                return;
            }

            var childList1 = new List<IPrimitive>();
            var childList2 = new List<IPrimitive>();
            BoundsInt partition = PartitionBounds(b);
            for(int i = 0; i<primitives.Count; i++){
                var p = primitives[i];
                if(partition.Contains(p.bounds.position))
                    childList1.Add(p);
                else
                    childList2.Add(p);
            }

            CreateNode128(exit, childList2, tree);
            uint cid2 = (uint)(tree.Count-1); 
            CreateNode128(cid2, childList1, tree);
            uint cid1 = (uint)(tree.Count-1); 
            Node128 n = new Node128(bb,cid1, exit, uint.MaxValue);
            tree.Add(n);
        }
    }
}