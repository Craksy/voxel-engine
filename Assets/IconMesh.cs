using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class IconMesh : MonoBehaviour
{

    public VoxelBlocksManager blocksManager;

    private MeshRenderer meshRenderer;
    private Mesh mesh;
    public RenderTexture rtext;
    public List<Texture2D> staticTextures;
    public Camera iconCamera;

    private Quaternion baseRotation;

    void Start()
    {
        baseRotation = transform.rotation;
        meshRenderer = GetComponent<MeshRenderer>();
        blocksManager.GenerateTexture();
        meshRenderer.material.mainTexture = blocksManager.TextureSheet;
        mesh = GetComponent<MeshFilter>().mesh;
    }

    public void Init(){
        Block type = blocksManager.blockTypes[1];
        List<Vector3> verts = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();


        for(int i = 0; i<6; i++){
            verts.AddRange(CubeMeshData.GetFaceVertices(i, Vector3.zero));
            int vcount = verts.Count;
            triangles.Add(vcount-4); //0
            triangles.Add(vcount-2); //2
            triangles.Add(vcount-1); //3
            triangles.Add(vcount-4); //0
            triangles.Add(vcount-1); //3
            triangles.Add(vcount-3); //1
            uvs.AddRange(blocksManager[type[i]]);
        }

        mesh.Clear();
        mesh.vertices = verts.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.RecalculateNormals();

        staticTextures = new List<Texture2D>();
        for(int i = 1; i<blocksManager.blockTypes.Count; i++){
            Redraw(i);
            iconCamera.Render();
            var tex = new Texture2D(512, 512, TextureFormat.RGBA32, false);
            RenderTexture.active = rtext;
            tex.ReadPixels(new Rect(0,0,rtext.width, rtext.height), 0, 0);
            tex.Apply();
            staticTextures.Add(tex);
        }
    }

    public void Redraw(int blocktype){
        transform.rotation = baseRotation;
        Block type = blocksManager.blockTypes[blocktype];
        List<Vector2> uvs = new List<Vector2>();

        for(int i = 0; i<6; i++){
            uvs.AddRange(blocksManager[type[i]]);
        }

        mesh.uv = uvs.ToArray();
        mesh.RecalculateNormals();
    }

    void Update()
    {
        transform.Rotate(new Vector3(0, 45, 0)*Time.deltaTime, Space.Self);
    }
}
