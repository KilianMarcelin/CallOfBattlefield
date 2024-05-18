using System;
using Mirror;
using UnityEngine;
using UnityEngine.AI;

public class MobSpawner : NetworkBehaviour
{
    public float timeBetweenSpawns = 2.0f;
    private float timeUntilSpawn = 0.0f;
    public GameObject spider;


    private void Update()
    {
        if (!isServer) return;

        timeUntilSpawn -= Time.deltaTime;

        if (timeUntilSpawn <= 0)
        {
            NavMeshHit hit;
            NavMesh.SamplePosition(transform.position, out hit, Mathf.Infinity, NavMesh.GetAreaFromName("Everything"));
            GameObject go = NetworkManager.Instantiate(spider, hit.position, Quaternion.identity);
            NetworkServer.Spawn(go);
            timeUntilSpawn = timeBetweenSpawns;
        }
    }
}