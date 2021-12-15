using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("Camera-Control/Mouse Look")]
public class CameraScript : MonoBehaviour
{
    public float MoveSpeed = 2;
    public float Sensitivity = 10;

    private float xr = 0;
    private float yr = 0;


    Quaternion original;

    public bool locked;

    void Start()
    {
        original = transform.localRotation;
    }

    void Update()
    {

        if(Input.GetKeyDown(KeyCode.Tab)){
            locked = !locked;
        }
        if(locked)
            return;
        float hz = Input.GetAxis("Horizontal");
        float vt = Input.GetAxis("Vertical");

        xr += Input.GetAxis("Mouse X") *  Sensitivity;
        yr += Input.GetAxis("Mouse Y") *  Sensitivity;

        yr = Mathf.Clamp(yr, -90f, 90f);
        

        Quaternion xq = Quaternion.AngleAxis(xr, Vector3.up);
        Quaternion yq = Quaternion.AngleAxis(-yr, Vector3.right);
        transform.localRotation = original * xq * yq;
        transform.Translate(hz * MoveSpeed * Time.deltaTime, 0, vt * MoveSpeed * Time.deltaTime);
    }
}
        
     
     
    
