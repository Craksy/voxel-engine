using System.Collections.Generic;
using UnityEngine;

using Vox.Bvh;

namespace Vox.Rendering
{
    public class BvhBuffer {
        public readonly ComputeBuffer buffer;
        public readonly int nodeCount;
        public readonly int root;
        public Vector3 offset;
        public Bounds rootBounds;

        public BvhBuffer(List<Node128> tree, Vector3 offset){
            nodeCount = tree.Count;
            root = tree.Count-1;
            this.offset = offset;
            var rb = tree[root].bounds;
            var size = Vector3.one*128;
            rootBounds = new Bounds(rb.Min+size*0.5f+offset, size);
            buffer = new ComputeBuffer(nodeCount, 24);
            buffer.name = $"BvhBuffer_{GridManager.BlockToIndex((int)offset.x,(int)offset.y,(int)offset.z)}";
            buffer.SetData(tree);
        }
    }

}