using System;
using UnityEngine;

namespace Vox{
    public class Chunk
    {
        public byte[] voxels;

        public byte this[int x, int y, int z] {
            get => voxels[GridManager.BlockToIndex(x, y, z)];
            set => voxels[GridManager.BlockToIndex(x,y,z)] = value;
        }

        public byte this[Vector3Int blockPos]
        {
            get => this[blockPos.x, blockPos.y, blockPos.z];
            set => this[blockPos.x, blockPos.y, blockPos.z] = value;
        }
    }

    public class ChunkId : IEquatable<ChunkId>
    {
        public readonly int X, Y, Z;

        public ChunkId(int x, int y, int z) {
            X = x;
            Y = y;
            Z = z;
        }

        public ChunkId(Vector3Int pos){
            X = pos.x;
            Y = pos.y;
            Z = pos.z;
        }

        public Vector3Int ChunkPosition => new Vector3Int(X, Y, Z);

        public override int GetHashCode() {
            unchecked {
                var hashcode = X;
                hashcode = (hashcode * 397) ^ Y;
                hashcode = (hashcode * 397) ^ Z;
                return hashcode;
            }
        }

        public bool Equals(ChunkId other) {
            return X == other.X && Y == other.Y && Z == other.Z;
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj))
                return false;
            return obj is ChunkId other && Equals(other);
        }

        public static bool operator ==(ChunkId lhs, ChunkId rhs) {
            return lhs.Equals(rhs);
        }
        public static bool operator !=(ChunkId lhs, ChunkId rhs) {
            return !lhs.Equals(rhs);
        }

        public override string ToString()
        {
            return $"ChunkId({X},{Y},{Z})";
        }
    }
}