using System;
using Mirror;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

public class PlayerMovement : NetworkBehaviour
{
    public Rigidbody rb;
    [FormerlySerializedAs("speed")] public float runSpeed = 4f;
    public float walkMult = 0.4f;
    public float jumpForce = 4f;
    public float airControl = 0.02f;
    public Animator animator;
    public Collider playerCollider;

    [SyncVar] public bool canMove = true;
    [SyncVar] public bool isRunning = false;
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

    [Command]
    public void CmdSetIsRunning(bool value)
    {
        isRunning = value;
    }

    private void Update()
    {
        if (isOwned && canMove)
        {
            // Movement

            // Movement
            bool localIsRunning = Input.GetButton("Run");
            CmdSetIsRunning(localIsRunning);

            if (!isInAir)
            {
                moveX = Input.GetAxis("Horizontal") * (localIsRunning ? 1f : walkMult);
                moveZ = Input.GetAxis("Vertical") * (localIsRunning ? 1f : walkMult);

                moveX *= Time.deltaTime * runSpeed;
                moveZ *= Time.deltaTime * runSpeed;
            }
            else
            {
                // Inertia
                float newMoveX = Input.GetAxis("Horizontal") * Time.deltaTime * runSpeed;
                float newMoveZ = Input.GetAxis("Vertical") * Time.deltaTime * runSpeed;

                moveX = Mathf.Lerp(moveX, newMoveX, airControl);
                moveZ = Mathf.Lerp(moveZ, newMoveZ, airControl);
            }

            animator.SetFloat("forward", Input.GetAxis("Vertical") * (localIsRunning ? 1f : 0.5f));
            animator.SetFloat("left", -Input.GetAxis("Horizontal") * (localIsRunning ? 1f : 0.5f));
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