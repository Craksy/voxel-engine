using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Vox;

public class PlayerScript : MonoBehaviour
{
    public Camera PlayerCamera;
    public float MoveSpeed = 2;
    public float MaxSpeed = 20;
    public float JumpForce = 5;
    public float Sensitivity = 10;
    public float GravityValue = -9.82f;
    public VoxelEngine voxelEngine;

    private float xr = 0;
    private float yr = 0;
    private Quaternion originalCam, originalPlayer;
    private CharacterController controller;
    private Ray ray;

    private Vector3 playerVelocity;
    private float playerSpeed;
    private Vector3 moveDir;

    private PlayerInventoryScript inventoryScript;

    void Start()
    {
        originalCam = PlayerCamera.transform.localRotation;
        originalPlayer = transform.localRotation;
        controller = GetComponent<CharacterController>();
        inventoryScript = GetComponent<PlayerInventoryScript>();
    }

    void Update()
    {
        if(GameManager.gamePaused)
            return;
        xr += Input.GetAxis("Mouse X") *  Sensitivity;
        yr += Input.GetAxis("Mouse Y") *  Sensitivity;
        yr = Mathf.Clamp(yr, -90, 90);


        Quaternion xq = Quaternion.AngleAxis(xr, Vector3.up);
        Quaternion yq = Quaternion.AngleAxis(-yr, Vector3.right);
        PlayerCamera.transform.localRotation = originalCam * yq;
        transform.localRotation = originalPlayer * xq;

        bool playerGrounded = controller.isGrounded;

        if(playerGrounded){
            float hz = Input.GetAxis("Horizontal");
            float vt = Input.GetAxis("Vertical");
            if(hz != 0 || vt != 0){
                if(playerSpeed < MaxSpeed)
                    playerSpeed+=MoveSpeed*Time.deltaTime;
                moveDir = transform.TransformDirection(hz, 0f, vt).normalized;
            }else{
                playerSpeed = 0;
            }
            if(Input.GetButtonDown("Jump")){
                playerVelocity.y = Mathf.Sqrt(JumpForce * -3f * GravityValue);
            }
            if(playerVelocity.y < 0){
                playerVelocity.y = 0f;
            }
        }else{
            if(playerSpeed > 0){
                playerSpeed += -3f * Time.deltaTime;
                if(playerSpeed<0) playerSpeed = 0f;
            }
        }

        if(MoveSpeed != 0)
            controller.Move(moveDir * playerSpeed * Time.deltaTime);

        playerVelocity.y += GravityValue * Time.deltaTime;

        controller.Move(playerVelocity * Time.deltaTime);

        var center = PlayerCamera.pixelRect.center;
        ray = PlayerCamera.ScreenPointToRay(new Vector3(center.x, center.y));
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 15f, LayerMask.GetMask("Ground"))) {
            voxelEngine.HighlightBlock(hit);
        }
        if(Input.GetButtonDown("Fire1")){
            voxelEngine.PlayerHitBlock(hit);
        }
        if(Input.GetButtonDown("Fire2")){
            voxelEngine.PlayerPlaceBlock(hit, inventoryScript.CurrentBlock);
        }
    }

    private Vector3Int FloorSign(Vector3 v){
        return new Vector3Int(
            v.x>0 ? Mathf.FloorToInt(v.x):Mathf.CeilToInt(v.x),
            v.y>0 ? Mathf.FloorToInt(v.y):Mathf.CeilToInt(v.y),
            v.z>0 ? Mathf.FloorToInt(v.z):Mathf.CeilToInt(v.z));
    }
}
