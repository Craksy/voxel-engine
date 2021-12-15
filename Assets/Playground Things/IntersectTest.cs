using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


[System.Serializable]
public struct BBounds {
    public Vector3 min;
    public Vector3 max;
    public BBounds(Vector3 min, Vector3 max){
        this.min = min;
        this.max = max;
    }
}

[System.Serializable]
public struct ISect{
    public Vector3 hit1; 
    public Vector3 hit2; 
    public float distance;
    public bool intersect;
}


[System.Serializable]
public struct BRay {
    public Vector3 origin;
    public Vector3 direction;
}

public struct BBTree{
    public BBounds bounds;
}

public class IntersectTest : MonoBehaviour
{

    public Transform Cube;
    public List<Transform> cubes;
    private Ray ray;
    private Bounds box;

    public ComputeShader shader;

    public BBounds Shape;
    public bool showRay;

    private ISect lastHit = new ISect();

    [Range(0f, 20f)]
    public float rt = 0;

    // Start is called before the first frame update
    void Start()
    {
        var hd = (Shape.max-Shape.min);
        box = new Bounds(Shape.min+hd*0.5f, hd);
        rt = 0f;
    }

    private void CreateObjects(){
    }

    private void BuildBVH(List<Bounds> cubes){
        var b = new Bounds(Vector3.negativeInfinity, Vector3.negativeInfinity);
        foreach(var c in cubes){
            b.Encapsulate(c);
        }
    }

    private void CheckBoxGPU(){
        var count = Screen.width * Screen.height;
        Debug.Log(count);
        var rays = new Ray[count];
        for(int x = 0; x<Screen.width; x++){
            for(int y =0; y<Screen.height; y++){
                //var br = new BRay();
                var r = Camera.main.ScreenPointToRay(new Vector3(x,y));
                rays[x*Screen.height+y] = r;
            }
        }
        var compbuf = new ComputeBuffer(count, 24);
        compbuf.SetData(rays);
        shader.SetBuffer(0, "Rays", compbuf);

        int[] hits = new int[cubes.Count];
        var hitbuf = new ComputeBuffer(cubes.Count, 4);
        hitbuf.SetData(hits);
        shader.SetBuffer(0, "Hits", hitbuf);


        var boxbuf = new ComputeBuffer(cubes.Count, 24);
        boxbuf.SetData(cubes.Select(c => c.GetComponent<MeshRenderer>().bounds).ToArray());
        shader.SetBuffer(0, "Boxes", boxbuf);

        
        shader.SetFloats("_bmin", box.min.x, box.min.y, box.min.z);
        shader.SetFloats("_bmax", box.max.x, box.max.y, box.max.z);
        shader.SetFloat("height", Screen.width);
        shader.SetInt("boxcount", cubes.Count);

        shader.Dispatch(0, Screen.width/8, Screen.height/8, 1);
        hitbuf.GetData(hits);


        compbuf.Dispose();
        hitbuf.Dispose();


        Debug.Log($"count hits: {hits.Count(b => b!=0)}");

        for(int i = 0; i<hits.Length; i++){
            cubes[i].GetComponent<MeshRenderer>().material.SetColor("_Color", hits[i] != 0 ? Color.green : Color.red);
        }
    }

    private void OnDrawGizmos() {
        Gizmos.color = box.IntersectRay(ray) ?  Color.red: Color.green;
        if(box != null)
            Gizmos.DrawWireCube(box.center, box.size);

        Gizmos.color = Color.red;
        if(showRay)
            Gizmos.DrawRay(ray.origin, ray.direction*50f);

        if(true){
            var h = lastHit;
            var h1 = h.hit1;
            var h2 = h.hit2;

            Gizmos.color = Color.green;

            //Gizmos.DrawLine(new Vector3(box.min.x, ), ray.origin+Vector3.Scale(ray.direction,h2));
            var hmin = Mathf.Max(h1.x, h1.y, h1.z);
            var hmax = Mathf.Min(h2.x, h2.y, h2.z);
            var pos1 = ray.origin + ray.direction*hmin; 
            var pos2 = ray.origin + ray.direction*hmax; 
            Gizmos.DrawWireSphere(pos1, 0.2f);
            Gizmos.DrawWireSphere(pos2, 0.2f);
            Gizmos.DrawLine(pos1, pos2);
            
            //Gizmos.DrawWireSphere(ray.origin + new Vector3(ray.direction.x*h1.x, ray.direction.y*h1.y,ray.direction.z*h1.z), 0.2f);
            //Gizmos.DrawWireSphere(ray.origin + Vector3.Scale(ray.direction, h2), 0.2f);
        }
    }

    private void OnGUI() {
        GUI.Label(new Rect(0, 0, 50, 50), "Size");
        GUI.Label(new Rect(Screen.width-200, 0, 200, 20), $"h1:{lastHit.hit1}");
        GUI.Label(new Rect(Screen.width-200, 20, 200, 20), $"h2:{lastHit.hit2}");
        GUI.Label(new Rect(Screen.width-200, 30, 200, 20), $"r:{ray.origin} - {ray.direction}");
        GUI.Label(new Rect(Screen.width-200, 40, 200, 20), $"t:{ray.origin + ray.direction*rt}");
        box.size = GUI.HorizontalSlider(new Rect(45,5, 100, 20), box.size.x, 1f, 5f)*Vector3.one;
        GUI.Label(new Rect(50, 20, 50, 50), "Center");
        box.center = new Vector3(
            GUI.HorizontalSlider(new Rect(0,40,50, 20), box.center.x, -10f, 10f), 
            GUI.HorizontalSlider(new Rect(50,40,50, 20), box.center.y, -10f, 10f), 
            GUI.HorizontalSlider(new Rect(100,40,50, 20), box.center.z, -10f, 10f)); 
        rt = GUI.HorizontalSlider(new Rect(0,60,100, 20), rt, 1f, 10f);
        GUI.Label(new Rect(0, 70, 200, 20), $"rt:{rt}");
        if(GUI.Button(new Rect(0, 80, 100, 30), "GPU RAYS")){
            CheckBoxGPU();
        }
    }

    private Vector3 VDiv(Vector3 a, Vector3 b) => new Vector3(a.x/b.x, a.y/b.y, a.z/b.z);

    private ISect GetIntersection(Bounds bounds, Ray ray){
        //t0x = (B0x - Ox) / Dx 
        Vector3 tmin = new Vector3(
            (bounds.min.x-ray.origin.x)/ray.direction.x,
            (bounds.min.y-ray.origin.y)/ray.direction.y,
            (bounds.min.z-ray.origin.z)/ray.direction.z);
        Vector3 tmax = new Vector3(
            (bounds.max.x-ray.origin.x)/ray.direction.x,
            (bounds.max.y-ray.origin.y)/ray.direction.y,
            (bounds.max.z-ray.origin.z)/ray.direction.z);

        Vector3 b2 = Vector3.Min(tmin, tmax);
        Vector3 b3 = Vector3.Max(tmin, tmax);

        float cmin = b2.x;
        float cmax = b3.x;

        ISect res = new ISect();
        res.hit1 = b2;
        res.hit2 = b3;
        res.distance = Mathf.Max(b2.x, b2.y, b2.z);
        res.intersect = !(Mathf.Max(b2.x, b2.y, b2.z) > Mathf.Min(b3.x, b3.y, b3.z));
        return res;
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetButtonDown("Fire1")){
            ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Debug.Log("update ray");
            lastHit = GetIntersection(box,ray);
            Debug.Log("Distance: "+lastHit.distance);
            if(lastHit.intersect){
                Debug.Log("Hit!");
            }
            
        }

        Cube.localScale = box.size*0.9f;
    }
}
