using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class BoundingTree{
    public Bounds bounds = new Bounds(Vector3.negativeInfinity, Vector3.negativeInfinity);
    public BoundingTree[] children;
    public List<Bounds> objects;
    public List<Transform> gameObjects;
    
    public BoundingTree(IEnumerable<Transform> objects){
        this.gameObjects = objects.ToList();
        foreach(var b in objects)
            bounds.Encapsulate(b.GetComponent<MeshRenderer>().bounds);
    }
}

public class BVH_Test : MonoBehaviour
{
    public List<Transform> gameObjects;
    public List<Transform> visibleGameObjects;

    public Mesh mesh;
    public Material material;

    private int recurssionLevel = 1;

    private BoundingTree root;
    private BoundingTree currentTree;

    private Color[] levelColors = {Color.red, Color.blue, Color.green, Color.yellow, Color.magenta, Color.cyan};


    void Start()
    {
        visibleGameObjects = new List<Transform>();
        InitObjects(500, 150f);
        root = CalculateBVH(gameObjects);
        currentTree = root;
    }

    private void CheckVisible(){
        RaycastHit hit;
        visibleGameObjects = new List<Transform>();
        for(int x=0; x<Screen.width; x++){
            for(int y=0;y<Screen.height; y++){
                var ray = Camera.main.ScreenPointToRay(new Vector3(x,y));
                if(Physics.Raycast(ray, out hit, 300f, LayerMask.NameToLayer("Everything"))){
                    Debug.Log("Hit");
                    visibleGameObjects.Add(hit.collider.transform);
                    hit.collider.gameObject.layer = 5;
                }
            }
        }
        foreach(var o in gameObjects){
            o.GetComponent<MeshRenderer>().material.SetColor("_Color", visibleGameObjects.Contains(o) ? Color.green : Color.red);
        }
    }

    private void CheckVisibleBVH(){
        visibleGameObjects = new List<Transform>();
        for(int x=0; x<Screen.width; x++){
            for(int y=0;y<Screen.height; y++){
                var ray = Camera.main.ScreenPointToRay(new Vector3(x,y));
                Transform hit;
                if(CheckRay(ray, root, out hit)){
                    visibleGameObjects.Add(hit);
                }
            }
        }
        foreach(var o in gameObjects){
            o.GetComponent<MeshRenderer>().material.SetColor("_Color", visibleGameObjects.Contains(o) ? Color.green : Color.red);
        }
    }

    private bool CheckRay(Ray ray, BoundingTree tree, out Transform gameobject){
        if(tree.bounds.IntersectRay(ray)){
            if(tree.children != null){
                bool c1 = tree.children[0].bounds.IntersectRay(ray);
                bool c2 = tree.children[1].bounds.IntersectRay(ray);
                if(c1 && c2){
                    if(tree.children[0].bounds.SqrDistance(Camera.main.transform.position) < tree.children[1].bounds.SqrDistance(Camera.main.transform.position)){
                        return CheckRay(ray, tree.children[0], out gameobject) || CheckRay(ray, tree.children[1], out gameobject);
                    }else{
                        return CheckRay(ray, tree.children[1], out gameobject) || CheckRay(ray, tree.children[0], out gameobject);
                    }
                }
                return CheckRay(ray, tree.children[0], out gameobject) || CheckRay(ray, tree.children[1], out gameobject);
            }else{
                gameobject = tree.gameObjects[0];
                return true;
            }
        }
        gameobject = null;
        return false;
    }

    private void InitObjects(int count, float spread){
        for(int i = 0; i<count; i++){
            var obj = new GameObject($"Object - {i}", typeof(MeshRenderer), typeof(MeshFilter), typeof(BoxCollider));
            obj.GetComponent<MeshRenderer>().material = material;
            obj.GetComponent<MeshFilter>().mesh = mesh;
            obj.transform.position = new Vector3(Random.Range(0f, spread), Random.Range(0f, spread), Random.Range(0f, spread));
            obj.transform.localScale = Vector3.one * Random.Range(.5f, 8f);
            obj.GetComponent<MeshRenderer>().material.SetColor("_Color", Random.ColorHSV());
            obj.layer = LayerMask.NameToLayer("Ground");
            gameObjects.Add(obj.transform);
        }
    }

    private void OnGUI() {
        GUI.Label(new Rect(0, 30, 150, 30), $"Level: {recurssionLevel}");
        recurssionLevel = (int)GUI.HorizontalScrollbar(new Rect(0, 60, 100, 30), recurssionLevel, 1, 1, 30);
        if(GUI.Button(new Rect(0, 0, 100, 30), "Check")){
            CheckVisible();
        }
        if(GUI.Button(new Rect(0, 90, 100, 30), "Check BVH")){
            CheckVisibleBVH();
        }
    }

    private void OnDrawGizmos() {
        if(currentTree == null)
            return;
        DrawBounds(currentTree, recurssionLevel, false);
    }

    private void DrawBounds(BoundingTree tree, int depth = 1, bool drawParents = true){
        Gizmos.color = levelColors[depth%levelColors.Length];
        if(tree.children != null && depth > 0){
            DrawBounds(tree.children[0], depth-1, drawParents);
            DrawBounds(tree.children[1], depth-1, drawParents);
        }else{
            Gizmos.DrawWireCube(tree.bounds.center, tree.bounds.size);
        }
    }

    private BoundingTree CalculateBVH(List<Transform> objects){
        var tree = new BoundingTree(objects);
        if(objects.Count > 1){
            var b = tree.bounds;
            var ma = LargestAxis(b.size);
            Bounds lb = new Bounds(b.center-ma*0.25f , b.size-ma*0.5f);
            //List<Bounds> lc = objects.Where(b => lb.Contains(b.center)).ToList();
            List<Transform> l1 = new List<Transform>();
            List<Transform> l2 = new List<Transform>();
            foreach(var o in objects){
                //if(lb.Contains(o.GetComponent<MeshRenderer>().bounds.center))
                if(lb.Contains(o.position))
                    l1.Add(o);
                else
                    l2.Add(o);
            }

            Debug.AssertFormat(l1.Count > 0 && l2.Count > 0, "L1: {0}, L2: {1}", l1.Count, l2.Count);
            if(l1.Count == 0){
                var o = l2[0];
                l2.Add(o);
                l1.Remove(o);
            }else if(l2.Count == 0){
                var o = l1[0];
                l1.Add(o);
                l2.Remove(o);
            }

            tree.children = new BoundingTree[2];
            tree.children[0] = CalculateBVH(l1);
            tree.children[1] = CalculateBVH(l2);
        }
        return tree;
    }

    private Vector3 LargestAxis(Vector3 vec){
        Vector3 ma = Vector3.zero;
        float mv = 0;
        for(int i = 0; i<3;i++)
            if(vec[i] > mv){
                mv = vec[i];
                ma = Vector3.zero;
                ma[i] = mv;
            }
        return ma;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}