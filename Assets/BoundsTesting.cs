using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EasyButtons;

public class BoundsTesting : MonoBehaviour
{


    [Header("Shape")]
    public Vector3 Center;
    public Vector3 Size;

    public List<Transform> Objects;

    private List<Bounds> bounds;

    private int currentBounds = 0;


    // Start is called before the first frame update
    void Start()
    {
        bounds = new List<Bounds>();
        Reset();
    }

    // Update is called once per frame
    void Update()
    {
        bounds[currentBounds] = new Bounds(Center, Size);
    }

    private void OnGUI() {
        if(GUILayout.Button("Next"))
            SwitchCurrent();
        if(GUILayout.Button("Add"))
            AddBox();
        if(GUILayout.Button("Contain"))
            ContainAll();
        if(GUILayout.Button("Mark"))
            MarkInCurrent();
        if(GUILayout.Button("Partition"))
            PartitionCurrent();
    }

    private void OnDrawGizmos() {
        Gizmos.color = Color.white;
        for(int i = 0; i< bounds.Count; i++){
            if(i == currentBounds)
                continue;
            Gizmos.DrawWireCube(bounds[i].center, bounds[i].size);
        }
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(bounds[currentBounds].center, bounds[currentBounds].size);
    }

    [Button("Next Box")]
    private void SwitchCurrent(){
        currentBounds = (currentBounds+1)%bounds.Count;
        Center = bounds[currentBounds].center;
        Size = bounds[currentBounds].size;
    }

    [Button("Add Box")]
    private void AddBox(){
        bounds.Add(new Bounds(Vector3.zero, Vector3.one));
        SwitchCurrent();
    }

    [Button("Mark Contained")]
    private void MarkInCurrent(){
        foreach(var obj in Objects){
            if(bounds[currentBounds].Contains(obj.position)){
                obj.GetComponent<MeshRenderer>().material.SetColor("_Color", Color.green);
            }else{
                obj.GetComponent<MeshRenderer>().material.SetColor("_Color", Color.red);
            }
        }
    }

    private void ContainAll(){
        var b = new Bounds();
        foreach(var go in Objects){
            b.Encapsulate(go.GetComponent<MeshRenderer>().bounds);
        }
        bounds[currentBounds] = b;
        Center = b.center;
        Size = b.size;
    }

    [Button("Partition current")]
    private void PartitionCurrent(){
        var p = Partition(bounds[currentBounds]);
        bounds.Add(p);
        SwitchCurrent();
    }

    private void Reset() {
        bounds.Clear();
        bounds.Add(new Bounds(Vector3.zero, Vector3.one));
    }

    private Bounds Partition(Bounds b){
        var nb = new Bounds(b.center - Vector3.Scale(b.extents, Vector3.right)*0.5f, b.size - Vector3.Scale(b.extents, Vector3.right));
        return nb;
    }
}
