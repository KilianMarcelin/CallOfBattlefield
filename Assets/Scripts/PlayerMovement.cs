using System;
using Mirror;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour
{
    public Rigidbody rb;
    public float speed = 4f;
    public float jumpForce = 4f;
    public float airControl = 0.02f;
    public Animator animator;
    public Collider playerCollider;

    [SyncVar] public bool canMove = true;
    private float moveX = 0, moveZ = 0;

    [SyncVar] private int isInAir_count = 0;
    public bool isInAir
    {
        get { return isInAir_count <= 0; }
    }

    [Server]
    public void ServerResetPosition()
    {
        ServerSetPosition(NetworkManager.singleton.GetStartPosition().position);
    }
    
    [Server]
    public void ServerSetCanMove(bool value)
    {
        canMove = value;
    }

    [Server]
    public void ServerSetPosition(Vector3 position)
    {
        rb.useGravity = true;
        playerCollider.enabled = true;
        // transform.position = NetworkManager.singleton.GetStartPosition().position;
        transform.position = position;
    }

    [Command]
    public void CmdTranslate(Vector3 translation)
    {
        transform.Translate(translation);
    }

    [Command]
    public void CmdAddForce(Vector3 force, ForceMode mode)
    {
        rb.AddForce(force, mode);
    }

    private void Update()
    {
        if (isOwned && canMove)
        {
            // Movement
            
            // Movement
            if (!isInAir)
            {
                moveX = Input.GetAxis("Horizontal");
                moveZ = Input.GetAxis("Vertical");
            
                moveX *= Time.deltaTime * speed;
                moveZ *= Time.deltaTime * speed;
            }
            else
            {
                // Inertia
                moveX += Input.GetAxis("Horizontal") * airControl * Time.deltaTime * speed;
                moveZ += Input.GetAxis("Vertical") * airControl * Time.deltaTime * speed;
            }

            animator.SetFloat("forward", Input.GetAxis("Vertical"));
            animator.SetFloat("left", -Input.GetAxis("Horizontal"));
            animator.SetBool("isInAir", isInAir);

            CmdTranslate(new Vector3(moveX, 0, moveZ));

            // Jumping
            if (!isInAir && Input.GetButtonDown("Jump"))
            {
                CmdAddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
            }
        }
    }

    // Is grounded
    private void OnTriggerEnter(Collider other)
    {
        isInAir_count += 1;
    }

    private void OnTriggerExit(Collider other)
    {
        isInAir_count -= 1;
    }
}