using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.AI;

public class MobSpawner : NetworkBehaviour
{
    public float timeBetweenSpawns = 2.0f;
    private float timeUntilSpawn = 0.0f;
    public GameObject spider;

    public int maxEnnemyCount = 5;
    // Keep track of how many mobs have spawned
    public List<GameObject> ennemies;

    private void Update()
    {
        if (!isServer) return;

        timeUntilSpawn -= Time.deltaTime;

        if (timeUntilSpawn <= 0 && ennemies.Count < maxEnnemyCount)
        {
            NavMeshHit hit;
            NavMesh.SamplePosition(transform.position, out hit, Mathf.Infinity, NavMesh.GetAreaFromName("Everything"));
            GameObject go = NetworkManager.Instantiate(spider, hit.position, Quaternion.identity);
            NetworkServer.Spawn(go);
            timeUntilSpawn = timeBetweenSpawns;
            ennemies.Add(go);
        }

        // Remove dead ennemies
        ennemies = ennemies.Where(e => e).ToList();
    }
}