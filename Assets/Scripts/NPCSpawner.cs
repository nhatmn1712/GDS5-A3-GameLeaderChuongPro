using UnityEngine;
using System.Collections.Generic;

public class NPCSpawner : MonoBehaviour
{
    [Header("Spawning Setup")]
    [Tooltip("Add multiple NPC Prefabs here to spawn random variations!")]
    public GameObject[] npcPrefabs;
    [Tooltip("Where NPCs appear.")]
    public Transform spawnPoint;
    [Tooltip("Where NPCs walk to when they leave the shop.")]
    public Transform despawnPoint;

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
        if (npcPrefabs == null || npcPrefabs.Length == 0 || spawnPoint == null) return;

        // Pick a random prefab from the list
        GameObject prefabToSpawn = npcPrefabs[Random.Range(0, npcPrefabs.Length)];

        GameObject newNpc = Instantiate(prefabToSpawn, spawnPoint.position, spawnPoint.rotation);
        activeCustomers.Add(newNpc);
        
        // Ensure the NPC knows where to go when it leaves
        NpcCustomer customer = newNpc.GetComponent<NpcCustomer>();
        if (customer != null)
        {
            customer.despawnPoint = despawnPoint;
        }
    }
}
