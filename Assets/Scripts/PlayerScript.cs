﻿using Mirror;
using UnityEngine;

public class PlayerScript : NetworkBehaviour
{
    public Rigidbody rb;
    public float speed = 4f;
    public float jumpForce = .3f;
    public Animator animator;
    public GameObject sourceObject;

    [SyncVar] public float health = 100f;

    private bool hasJumped = false;

    private float verticalAngle = 0.0f;

    [Server]
    public void ServerHit(float damage)
    {
        health -= damage;
        Debug.Log("Took damage, health: " + health);

        if (health <= 0)
        {
        }
    }

    public override void OnStartAuthority()
    {
        Camera.main.transform.SetParent(transform);
        Camera.main.transform.localPosition = new Vector3(0, 1.7f, 0);

        // aim
        sourceObject.transform.SetParent(Camera.main.transform);
        sourceObject.transform.localPosition = new Vector3(0, 0, 20);

        name = "LocalPlayer" + Random.Range(1, 1000);

        // Lock cursor
        Cursor.lockState = CursorLockMode.Locked;
    }

    public bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, 1.02f);
    }

    void Update()
    {
        if (!isOwned)
        {
            return;
        }

        float moveX = Input.GetAxis("Horizontal") * Time.deltaTime * speed;
        float moveZ = Input.GetAxis("Vertical");

        if (moveZ < 0)
        {
            moveZ *= .5f;
        }

        moveZ *= Time.deltaTime * speed;

        animator.SetFloat("forward", Input.GetAxis("Vertical"));
        animator.SetFloat("left", -Input.GetAxis("Horizontal"));

        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        transform.Rotate(0, moveX, 0);
        transform.Translate(moveX, 0, moveZ);


        verticalAngle += mouseY;
        if (verticalAngle < -90f) verticalAngle = -90f;
        else if (verticalAngle > 90f) verticalAngle = 90f;

        animator.SetFloat("aim", verticalAngle / 90f);

        Camera.main.transform.localRotation = Quaternion.AngleAxis(verticalAngle, Vector3.left);
        transform.Rotate(0, mouseX, 0);

        if (Input.GetButton("Jump") && !hasJumped)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            hasJumped = true;
        }
        else if (IsGrounded())
        {
            hasJumped = false;
        }

        if (Input.GetButton("Fire1"))
        {
            Debug.Log("Shooting");
            Vector3 origin = Camera.main.transform.position;
            Vector3 direction = Camera.main.transform.forward;
            CmdShoot(origin, direction);
        }
    }

    [Command]
    public void CmdShoot(Vector3 origin, Vector3 direction)
    {
        RaycastHit hit;
        if (Physics.Raycast(origin, direction, out hit, 100f))
        {
            PlayerScript player = hit.transform.GetComponent<PlayerScript>();
            if (player != null)
            {
                player.ServerHit(10f);
            }
        }
    }
}