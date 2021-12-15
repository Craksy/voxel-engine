using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vox.Rendering{
    public interface IVoxelRenderer
    {
        void AddChunk(ChunkId id);
        void RemoveChunk(ChunkId id);
    }
}

