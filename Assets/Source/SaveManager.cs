using System;
using System.IO;
using UnityEngine;

using Vox;

public static class SaveManager
{
    public static string SavesBasePath = Application.persistentDataPath + "/saves/";
    public static string CurrentSavePath = SavesBasePath + "bigg128/";

    private static Vector3Int _segmentShape = new Vector3Int(4, 4, 4);
    public static int voxelStride = 1;

    //private static int sectionSize => _segmentShape.x * _segmentShape.y * _segmentShape.z; 
    private static int chunkByteSize => chunkSize * voxelStride;
    private static int chunkSize {
        get {
            var cs = GridManager.ChunkShape;
            return cs.x*cs.y*cs.z;
        }
    }

    /// <summary>
    /// Get the segment that a given chunk belongs to.
    /// </summary>
    /// <param name="chunkpos">Chunk offset (id)</param>
    /// <returns>Segment coordinate</returns>
    private static Vector3Int ChunkToSegmentPos(Vector3Int chunkpos) => new Vector3Int(chunkpos.x/_segmentShape.x, chunkpos.y/_segmentShape.y, chunkpos.z/_segmentShape.z);

    private static string PositionToFileName(Vector3Int position) => $"S{position.x}_{position.y}_{position.z}.sav";

    private static FileStream GetFileStreamFromPos(Vector3Int chunkPos){
        var segmentPos = ChunkToSegmentPos(chunkPos);
        //string path = ApplicationDataManager.CurrentSavePath + "/" + PositionToFileName(segmentPos);
        var path = Path.Combine(CurrentSavePath, PositionToFileName(segmentPos));
        var fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite);
        return fs;
    }

    public static void SaveChunk(Vector3Int chunkPos, byte[] data){
        //find the segment to save to
        var position = ChunkToSegmentPos(chunkPos);
        
        //find the chunkOffset within that segment
        var chunkOffset = chunkPos - Vector3Int.Scale(position, _segmentShape);
        
        //calculate how many bytes to offset to get to that chunk
        var byteOffset = (chunkOffset.x*_segmentShape.y*_segmentShape.z+chunkOffset.y*_segmentShape.z+chunkOffset.z)*chunkByteSize;
        var bytes = new byte[chunkByteSize];

        var fs = GetFileStreamFromPos(chunkPos);
        Buffer.BlockCopy(data, 0, bytes, 0, bytes.Length);
        Debug.Log($"saving chunk at {chunkPos} in segment {position}. offset within segment is {chunkOffset}({byteOffset} bytes)");
        using(var writer = new BinaryWriter(fs)){
            writer.Seek(byteOffset, SeekOrigin.Begin);
            writer.Write(bytes);
        }
        fs.Close();
    }

    public static byte[] LoadChunk(Vector3Int chunkPos){
        Vector3Int position = ChunkToSegmentPos(chunkPos);
        FileStream fs = GetFileStreamFromPos(chunkPos);
        Vector3Int chunkoffset = chunkPos - Vector3Int.Scale(position, _segmentShape);
        int byteoffset = (chunkoffset.x*_segmentShape.y*_segmentShape.z+chunkoffset.y*_segmentShape.z+chunkoffset.z)*chunkByteSize;
        byte[] buffer = new byte[chunkByteSize];
        fs.Seek(byteoffset, SeekOrigin.Begin);
        using(BinaryReader reader = new BinaryReader(fs)){
            buffer = reader.ReadBytes(chunkByteSize);
        }
        fs.Close();
        byte[] chunk = new byte[chunkSize];
        Buffer.BlockCopy(buffer, 0, chunk, 0, buffer.Length);
        return chunk;
    }
}
