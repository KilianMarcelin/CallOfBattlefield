using System;
using Mirror;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class PlayerMovement : NetworkBehaviour
{
    public CharacterController cr;
    public float runSpeed = 7f;
    public float walkSpeed = 5f;
    public float jumpHeight = 4f;
    public float movementControl = 0.1f;
    public float airControl = 0.02f;
    public Animator animator;
    public Animator fpGlobalAnimator;
    public float fpGlobalAninmatorChangeSpeed = 0.1f;
    // public Collider playerCollider;

    [SyncVar] public bool canMove = true;
    [SyncVar] public bool isRunning = false;
    private float inputX = 0, inputZ = 0;
    private float moveX = 0, moveZ = 0;
    public float velocityY = 0;
    private float animatorInputX = 0, animatorInputZ = 0;
    private float fpGlobalAnimatorInput = 0;

    [Client]
    public void ClientResetPosition()
    {
        Debug.Log("Resetting position");
        transform.position = NetworkManager.singleton.GetStartPosition().position;
    }

    [Server]
    public void ServerSetCanMove(bool value)
    {
        canMove = value;
    }

    // Probably going to change this, server authoritative movement
    // is more secure but client authoritative is more accurate and 
    // less awful to control.
    /*
    [Command]
    public void CmdTranslate(Vector3 translation)
    {
        transform.Translate(translation);
    }

    [Command]
    public void CmdAddForce(Vector3 force, ForceMode mode)
    {
        rb.AddForce(force, mode);
    }*/

    // Leftover code to notify remote client that this player is running
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

            // Calc input values
            bool localIsRunning = Input.GetButton("Run");
            CmdSetIsRunning(localIsRunning);

            float newInputX = Input.GetAxis("Horizontal");
            float newInputZ = Input.GetAxis("Vertical");

            inputX = Mathf.Lerp(inputX, newInputX, cr.isGrounded ? movementControl : airControl);
            inputZ = Mathf.Lerp(inputZ, newInputZ, cr.isGrounded ? movementControl : airControl);

            // CmdUpdateWalkValue(Mathf.Sqrt(inputX*inputX + inputZ*inputZ) * (localIsRunning ? 2.0f : 1.0f));

            // Update the fp global animator (breathing/walk/run)
            fpGlobalAnimatorInput = Mathf.Lerp(fpGlobalAnimatorInput,
                // Min so the input direction is never greater than 1
                Mathf.Min(Mathf.Sqrt(inputX * inputX + inputZ * inputZ), 1f) 
                * (localIsRunning ? 2.0f : 1.0f) 
                * (cr.isGrounded ? 1.0f : 0.0f), 
                fpGlobalAninmatorChangeSpeed);
            fpGlobalAnimator.SetFloat("speed", fpGlobalAnimatorInput);

            // Calc movement values
            moveX = inputX * Time.deltaTime * (localIsRunning ? runSpeed : walkSpeed);
            moveZ = inputZ * Time.deltaTime * (localIsRunning ? runSpeed : walkSpeed);

            // moveX = Mathf.Lerp(moveX, newMoveX, isInAir ? airControl : groundControl);
            // moveZ = Mathf.Lerp(moveZ, newMoveZ, isInAir ? airControl : groundControl);

            // Update the third person animator (will automatically be updated on remote clients)
            animatorInputX = Mathf.Lerp(animatorInputX, -inputX * (localIsRunning ? 1.5f : 1.0f), movementControl);
            animatorInputZ = Mathf.Lerp(animatorInputZ, inputZ * (localIsRunning ? 1.5f : 1.0f), movementControl);

            animator.SetFloat("forward", animatorInputZ);
            animator.SetFloat("left", animatorInputX);
            animator.SetBool("isInAir", !cr.isGrounded);

            // Jumping
            if (cr.isGrounded && Input.GetButtonDown("Jump"))
            {
                velocityY = jumpHeight;
            }
            else if (cr.isGrounded)
            {
                velocityY = -1f;
            }
            else
            {
                velocityY += Physics.gravity.y * Time.deltaTime;
            }

            // Apply movement
            Vector3 movement = new Vector3(moveX, velocityY * Time.deltaTime, moveZ);
            movement = transform.rotation * movement;
            cr.Move(movement);
        }
    }
}