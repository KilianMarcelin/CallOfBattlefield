using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.PlayerLoop;

public class SpiderAI : NetworkBehaviour
{

    public Transform playerTransform;
    public NavMeshAgent agent;
    public Animator animator;
    private bool inAttackRange = false;
    [SerializeField] private float attackRange = 3f;
    private float cooldown = 0f;
    private float cooldownMax = 2f;
    [SyncVar] private float life = 100.0f;
    public float attackCooldown = 0.0f;
    public float timeBetweenAttack = 1.0f;
    public float attackDamage = 20.0f;
    public UnityEvent<SpiderAI, PlayerScript> clientAttackCallback; 


    void GetPlayers()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player"); // Getting all players
        float minDist = float.MaxValue;
        // Getting nearest player
        foreach (GameObject player in players)
        {
            // Debug.Log(player);
            float dist = Vector3.Distance(player.transform.position, transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                playerTransform = player.transform;
            }
        }
    }

    /*
     * NOTE : Damage and spider lifetime was done really late in development
     * the code is not clean, could be much better, but it works. 
     */
    [Server]
    public void Damage(float damage)
    {
        life -= damage;
        if (life <= 0)
        {
            NetworkManager.Destroy(this.gameObject);
        }
    }
    
    [Command(requiresAuthority = false)]
    public void CmdDamage(float damage)
    {
        Damage(damage);
    }
    
    // Update is called once per frame
    // Move towards player selected in Start, when the spider is within attackRange, it attacks the player
    void Update()
    {
        if (!isServer) return;

        attackCooldown -= Time.deltaTime;
        
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
                agent.isStopped = false;
                animator.SetBool("walking", true);
                animator.SetBool("attack", false);
            }
            // We are close enough for attacks
            else
            {
                agent.isStopped = true;
                animator.SetBool("walking", false);
                animator.SetBool("attack", true);

                if (attackCooldown <= 0)
                {
                    Attack();
                    attackCooldown = timeBetweenAttack;
                }
            }

        }
        else
        {
            animator.SetBool("walking", false);
            animator.SetBool("attack", false);
        }
    }

    [Server]
    public void Attack()
    {
        var player = playerTransform.GetComponent<PlayerScript>();
        player.ServerDamage(attackDamage);
        RpcClientCallbacks(this, player);
    }

    [ClientRpc]
    public void RpcClientCallbacks(SpiderAI ai, PlayerScript player)
    {
        clientAttackCallback.Invoke(ai, player);
    }
}
