using UnityEngine;
using System.Collections.Generic;

public class NPCSpawner : MonoBehaviour
{
    [Header("Spawning Setup")]
    [Tooltip("Add multiple NPC Prefabs here to spawn random variations!")]
    public GameObject[] npcPrefabs;
    [Tooltip("Add multiple Spawn Points here. NPCs will pick one randomly when appearing.")]
    public Transform[] spawnPoints;
    [Tooltip("Add multiple Despawn Points here. NPCs will pick one randomly when leaving.")]
    public Transform[] despawnPoints;

    [Header("Settings")]
    public int maxCustomers = 4;
    public float spawnInterval = 10f;

    private float timer = 0f;
    private List<GameObject> activeCustomers = new List<GameObject>();

    void Update()
    {
        // Clean up any null references (if NPCs were destroyed)
        activeCustomers.RemoveAll(item => item == null);

        // Only spawn if Restaurant is open and we haven't reached max capacity
        if (RestaurantManager.Instance != null && RestaurantManager.Instance.isShopOpen)
        {
            if (activeCustomers.Count < maxCustomers)
            {
                timer -= Time.deltaTime;
                if (timer <= 0f)
                {
                    SpawnNPC();
                    timer = spawnInterval;
                }
            }
        }
    }

    void SpawnNPC()
    {
        if (npcPrefabs == null || npcPrefabs.Length == 0 || spawnPoints == null || spawnPoints.Length == 0) return;

        // Pick random prefab, spawn point, and despawn point
        GameObject prefabToSpawn = npcPrefabs[Random.Range(0, npcPrefabs.Length)];
        Transform randomSpawn = spawnPoints[Random.Range(0, spawnPoints.Length)];
        Transform randomDespawn = despawnPoints != null && despawnPoints.Length > 0 ? despawnPoints[Random.Range(0, despawnPoints.Length)] : null;

        GameObject newNpc = Instantiate(prefabToSpawn, randomSpawn.position, randomSpawn.rotation);
        activeCustomers.Add(newNpc);
        
        // Ensure the NPC knows where to go when it leaves
        NpcCustomer customer = newNpc.GetComponent<NpcCustomer>();
        if (customer != null)
        {
            customer.despawnPoint = randomDespawn;
        }
    }
}
