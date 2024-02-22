using System;
using Mirror;
using UnityEngine;

public class PlayerLook : NetworkBehaviour
{
    
    public float unzoomedFOV = 90.0f;
    public float zoomedFOV = 30.0f;
    public float zoomRate = 0.3f;
    public Animator animator;
    public Camera mainCamera;

    private float verticalAngle = 0.0f;

    public override void OnStartAuthority()
    {
        mainCamera.gameObject.tag = "MainCamera";
        mainCamera.gameObject.SetActive(true);
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        if (isOwned)
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");
            
            verticalAngle += mouseY;
            if (verticalAngle < -90f) verticalAngle = -90f;
            else if (verticalAngle > 90f) verticalAngle = 90f;
            
            animator.SetFloat("aim", verticalAngle / 90f);

            // Camera rotation 2
            mainCamera.transform.localRotation = Quaternion.AngleAxis(verticalAngle, Vector3.left);
            transform.Rotate(0, mouseX, 0);
            
            if (Input.GetButton("Fire2"))
            {
                mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, zoomedFOV, zoomRate);
            } else {
                mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, unzoomedFOV, zoomRate);
            } 
        }
    }
}