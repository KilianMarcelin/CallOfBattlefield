using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class SpiderAI : MonoBehaviour
{

    public Transform playerTransform;
    public NavMeshAgent agent;
    public Animator animator;
    private bool inAttackRange = false;
    [SerializeField] private float attackRange = 3f;


    void Start()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        float minDist = float.MinValue;
        foreach (GameObject player in players)
        {
            float dist = Vector3.Distance(player.transform.position, transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                playerTransform = player.transform;
            }
        }
    }
    
    // Update is called once per frame
    void Update()
    {
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
