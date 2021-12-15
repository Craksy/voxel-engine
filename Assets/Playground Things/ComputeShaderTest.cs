using System.Collections;
using System.Collections.Generic;
using UnityEngine;



struct Cube {
    public Vector3 position;
    public Color color;
}

public class ComputeShaderTest : MonoBehaviour
{

    public ComputeShader computeShader;

    public RenderTexture texture;
    public Mesh mesh;
    public Material material;
    public int dim;
    public int iterations;

    private List<GameObject> objects;
    private Cube[] cubes;

    // Start is called before the first frame update
    void Start()
    {
        var cam = Camera.main;
        var halfdim = dim/2;
        cam.transform.position = new Vector3(halfdim, 15, halfdim-0.5f);
        cam.orthographicSize = halfdim;
        objects = new List<GameObject>();
        cubes = new Cube[dim*dim];
        for(int x = 0; x<dim; x++){
            for(int y = 0; y<dim; y++){
                CreateCube(x,y);
            }
        }

        Debug.Log(objects.Count);
    }

    private void CreateCube(int x, int y){
        var cube = new GameObject($"Cube {x},{y}", typeof(MeshRenderer), typeof(MeshFilter));
        cube.GetComponent<MeshFilter>().mesh = mesh;
        cube.GetComponent<MeshRenderer>().material = material;

        cube.transform.position = new Vector3(x,Random.Range(0f, 10f),y);
        cube.GetComponent<MeshRenderer>().material.SetColor("_Color", Random.ColorHSV());

        objects.Add(cube);

        var cubedat = new Cube();
        cubedat.position = cube.transform.position;
        cubedat.color = cube.GetComponent<MeshRenderer>().material.GetColor("_Color");
        cubes[x*dim+y] = cubedat;
    }

    private void OnGUI() {
        if(GUI.Button(new Rect(0, 0, 100, 30), "Randomize")){
            RandomizeCubes();
        }
        if(GUI.Button(new Rect(0, 40, 100, 30), "GPU Rand")){
            RandomizeCubesGPU();
        }
    }

    private void RandomizeCubes(){
        foreach(var cube in objects){
            var v = cube.transform.position;
            var c = cube.GetComponent<MeshRenderer>().material.GetColor("_Color");
            for(int i = 0; i<iterations; i++){
                v = new Vector3(v.x,Random.Range(0f, 10f),v.z);
                c = Random.ColorHSV();
            }
            cube.transform.position = v;
            cube.GetComponent<MeshRenderer>().material.SetColor("_Color", c);
        }
    }

    private void RandomizeCubesGPU(){
        var datasize = 7*sizeof(float);
        var dataCount = cubes.Length;
        var computeBuffer = new ComputeBuffer(dataCount, datasize);
        computeBuffer.SetData(cubes);

        computeShader.SetBuffer(0, "cubes", computeBuffer);
        computeShader.SetFloat("count", dataCount);
        computeShader.SetInt("iterations", iterations);

        computeShader.Dispatch(0, dataCount/10, 1, 1);
        computeBuffer.GetData(cubes);

        for(int i = 0; i<dataCount; i++){
            var c = cubes[i];
            var obj = objects[i];
            obj.transform.position = c.position;
            obj.GetComponent<MeshRenderer>().material.SetColor("_Color", c.color);
        }

        computeBuffer.Dispose();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
