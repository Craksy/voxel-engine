using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Direction{
    SOUTH,
    NORTH,
    EAST,
    WEST,
    UP,
    DOWN
}

public static class CubeMeshData 
{
    static Vector3[] vertices = {
        //south
        new Vector3(-1, -1, -1),
        new Vector3( 1, -1, -1),
        new Vector3(-1,  1, -1),
        new Vector3( 1,  1, -1),

        //north
        new Vector3( 1, -1, 1),
        new Vector3(-1, -1, 1),
        new Vector3( 1,  1, 1),
        new Vector3(-1,  1, 1),
    };

    static int[][] faces = {
        new int[] {0,1,2,3},
        new int[] {4,5,6,7},
        new int[] {1,4,3,6},
        new int[] {5,0,7,2},
        new int[] {2,3,7,6},
        new int[] {5,4,0,1},
    };

    public static Vector3Int[] DirectionToPosition = {
        Vector3Int.back,
        Vector3Int.forward,
        Vector3Int.right,
        Vector3Int.left,
        Vector3Int.up,
        Vector3Int.down,
    };

    public static Vector3[] GetFaceVertices(int direction, Vector3 position){
        Vector3[] verts = new Vector3[4];
        for(int i = 0; i<4; i++){
            verts[i] = vertices[faces[(int)direction][i]] * 0.5f + position;
        }
        return verts;
    }


    //Create a cube with identical UVs for all 6 sides.
    public static Mesh CreateCube(){
        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();
        List<Vector2> uvs = new List<Vector2>();
        for(int i = 0;i<6;i++){
            verts.AddRange(GetFaceVertices(i,Vector3.zero));
            int vcount = verts.Count;
            tris.Add(vcount-4); //0
            tris.Add(vcount-2); //2
            tris.Add(vcount-1); //3
            tris.Add(vcount-4); //0
            tris.Add(vcount-1); //3
            tris.Add(vcount-3); //1
            uvs.Add(new Vector2(0, 0));
            uvs.Add(new Vector2(1, 0));
            uvs.Add(new Vector2(0, 1));
            uvs.Add(new Vector2(1, 1));
        }
        Mesh mesh = new Mesh();
        mesh.vertices = verts.ToArray();
        mesh.triangles = tris.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.RecalculateNormals();
        return mesh;
    }

    //Create a cube with identical UVs for all 6 sides.
    public static Mesh CreateMappedCube(){
        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();
        List<Vector2> uvs = new List<Vector2>();
        var stride = 1f/6f;
        for(int i = 0;i<6;i++){
            verts.AddRange(GetFaceVertices(i,Vector3.zero));
            int vcount = verts.Count;
            tris.Add(vcount-4); //0
            tris.Add(vcount-2); //2
            tris.Add(vcount-1); //3
            tris.Add(vcount-4); //0
            tris.Add(vcount-1); //3
            tris.Add(vcount-3); //1
            uvs.Add(new Vector2(0, i*stride));
            uvs.Add(new Vector2(1, i*stride));
            uvs.Add(new Vector2(0, i*stride+stride));
            uvs.Add(new Vector2(1, i*stride+stride));
        }
        Mesh mesh = new Mesh();
        mesh.vertices = verts.ToArray();
        mesh.triangles = tris.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.RecalculateNormals();
        return mesh;
    }

}
