using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "VoxelBlockManager", menuName = "Voxel/Block Manager")]
public class VoxelBlocksManager : ScriptableObject
{
    public static List<Texture2D> TextureCollection;
    public Texture2D TextureSheet;
    public int maxColumns = 8;
    public int textureSize = 128;
    public List<Texture2D> textures;
    public List<Block> blockTypes = new List<Block>();

    public int Columns {get; private set;}
    public int Rows {get; private set;}

    public float tileWidth, tileHeight;

    public void OnEnable() {
        VoxelBlocksManager.TextureCollection = textures;
    }

    public void GenerateTexture() {
        int count = textures.Count;
        textureSize = 128;
        maxColumns = 8;
        Rows = 1 + (count-1) / maxColumns;
        Columns = count >= maxColumns ? maxColumns : count;
        int width = Columns * textureSize;
        int height = Rows * textureSize;
        tileWidth = 1f/Columns;
        tileHeight = 1f/Rows;

        Texture2D sheet = new Texture2D(width, height);
        int c = 0;
        int r = 0;
        foreach(Texture2D tex in textures) {
            sheet.SetPixels(
                c * tex.width,
                r * tex.height,
                tex.width,
                tex.height,
                tex.GetPixels());
            c++;
            if(c >= maxColumns) {
                c = 0;
                r++;
            }
        }
        sheet.filterMode = FilterMode.Point;
        sheet.wrapMode = TextureWrapMode.Clamp;
        sheet.Apply();
        TextureSheet = sheet;
    }

    public Texture2DArray GenerateTextureArray(){
        var count = blockTypes.Count;
        var result = new Texture2DArray(textureSize, textureSize*6, count, TextureFormat.RGBA32, 4, false);

        for(int i = 0; i<count;i++){
            List<Color32> slices = new List<Color32>();
            for(int t=0; t<6;t++){
                slices.AddRange(textures[blockTypes[i][t]].GetPixels32());
            }
            result.SetPixels32(slices.ToArray(), i);
        }
        result.filterMode = FilterMode.Point;
        result.wrapMode = TextureWrapMode.Clamp;
        result.Apply();
        return result;
    }


    private Vector2Int BestDimensions(int count){
        var root = Mathf.Sqrt(count);
        return new Vector2Int(Mathf.CeilToInt(root), Mathf.FloorToInt(root));
    }
    

    private Vector2 IndexToPosition(int index) => new Vector2((index%Columns)*tileWidth, (index/Columns)*tileHeight);

    public IEnumerable<Vector2> GetBlockUvs(ushort blockType){
        var b = blockTypes[(int) blockType];
        return new[] {b.South, b.North, b.West, b.East, b.Down, b.Up}.Select(i => IndexToPosition(i));
    }

    public Vector2[] this[int textureIndex]{
        get{
            var pos = IndexToPosition(textureIndex);
            return new[] {
                new Vector2(0f      ,0f) + pos,
                new Vector2(tileWidth   ,0f) + pos,
                new Vector2(0f      ,tileHeight) + pos,
                new Vector2(tileWidth   ,tileHeight) + pos
            };
        }
    }
}

[System.Serializable]
public struct Block
{
    public int this[int direction] {
        get => direction switch {
            0 => South,
            1 => North,
            2 => East,
            3 => West,
            4 => Up,
            5 => Down,
            _ => -1
        };
    }

    public int[] GetAll(){
        return new[] {South, North, West, East, Down, Up};
    }

    public int FaceFromDirection(Direction direction)
    {
        switch (direction)
        {
            case Direction.SOUTH:
                return South;
            case Direction.NORTH:
                return North;
            case Direction.EAST:
                return East;
            case Direction.WEST:
                return West;
            case Direction.UP:
                return Up;
            case Direction.DOWN:
                return Down;
        }
        return 0;
    }
    public string Name;
    public int South;
    public int North;
    public int East;
    public int West;
    public int Up;
    public int Down;
}
