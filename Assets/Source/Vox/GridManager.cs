using UnityEngine;

namespace Vox 
{
    public static class GridManager{
        /* 
        position names:
        "Block": chunk local coordinate
        "World": world space coordinate
        "Chunk": chunk position (not scaled by chunksize)
        */

        private static Vector3Int _bitShape;
        private static Vector3Int _bitMask;
        private static Vector3Int _chunkShape;

        public static Vector3Int ChunkShape {
            get => _chunkShape;
            set {
                if(!(Mathf.IsPowerOfTwo(value.x) && Mathf.IsPowerOfTwo(value.y) && Mathf.IsPowerOfTwo(value.z)))
                    throw new System.Exception($"Cannot set chunkshape of {value}. All dimensions must be a power of 2");
                _chunkShape = value;
                _bitShape = new Vector3Int(
                    Mathf.CeilToInt(Mathf.Log(value.x, 2)),
                    Mathf.CeilToInt(Mathf.Log(value.y, 2)),
                    Mathf.CeilToInt(Mathf.Log(value.z, 2))
                );
                _bitMask = new Vector3Int(value.x-1, value.y-1, value.z-1);
                Debug.Log($"set new shape of {_chunkShape}. ({_bitShape}/{_bitMask})");
            }
        }
        public static int chunkWidth => ChunkShape.x;
        public static int chunkHeight => ChunkShape.y;
        public static int chunkDepth => ChunkShape.z;


        //Convertion helpers
        public static ChunkId ChunkIdFromWorld(int x, int y, int z) 
            => new ChunkId(x >> _bitShape.x, y >> _bitShape.y, z>>_bitShape.z);
        public static ChunkId ChunkIdFromWorld(Vector3Int position) => ChunkIdFromWorld(position.x, position.y, position.z);

        public static Vector3Int WorldToBlock(int x, int y, int z) => new Vector3Int(x&_bitMask.x, y&_bitMask.y, z&_bitMask.z);

        public static Vector3Int ChunkToWorld(Vector3Int chunkPosition) => Vector3Int.Scale(chunkPosition, ChunkShape);
        public static Vector3Int ChunkToWorld(ChunkId id) => Vector3Int.Scale(id.ChunkPosition, ChunkShape);

        public static Vector3Int IndexToBlock(int index) {
            return new Vector3Int(
                index/(chunkHeight*chunkDepth), 
                index/chunkDepth%chunkHeight,
                index % chunkDepth);
        }

        public static int BlockToIndex(int x, int y, int z) => x*chunkHeight*chunkDepth + y*chunkDepth + z;
        public static int BlockToIndex(Vector3Int blockPosition) => BlockToIndex(blockPosition.x, blockPosition.y, blockPosition.z);

        public static int WorldToIndex(int x, int y, int z) 
            => (x>>_bitShape.x)*chunkHeight*chunkDepth + (y>>_bitShape.y)*chunkDepth + (z>>_bitShape.z);
    }
}