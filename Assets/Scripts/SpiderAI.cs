using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.PlayerLoop;

public class SpiderAI : MonoBehaviour
{

    public Transform playerTransform;
    public NavMeshAgent agent;
    public Animator animator;
    private bool inAttackRange = false;
    [SerializeField] private float attackRange = 3f;
    private float cooldown = 0f;
    private float cooldownMax = 2f;


    void GetPlayers()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player"); // Getting all players
        float minDist = float.MaxValue;
        // Getting nearest player
        foreach (GameObject player in players)
        {
            Debug.Log(player);
            float dist = Vector3.Distance(player.transform.position, transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                playerTransform = player.transform;
            }
        }
    }
    
    // Update is called once per frame
    // Move towards player selected in Start, when the spider is within attackRange, it attacks the player
    void Update()
    {
        if (cooldown < cooldownMax) cooldown += Time.deltaTime;
        else
        {
            cooldown = 0;
            GetPlayers();
        }
        
        
        if (playerTransform)
        {
            if (Vector3.Distance(playerTransform.position, transform.position) > attackRange)
            {
                agent.SetDestination(playerTransform.position);
                animator.SetBool("walking", true);
                animator.SetBool("attack", false);
            }
            else
            {
                animator.SetBool("walking", false);
                animator.SetBool("attack", true);
            }

        }
        else
        {
            animator.SetBool("walking", false);
            animator.SetBool("attack", false);
        }
    }

}
