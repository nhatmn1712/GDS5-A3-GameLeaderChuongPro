using UnityEngine;
using System.Collections.Generic;

public class RestaurantManager : MonoBehaviour
{
    public static RestaurantManager Instance { get; private set; }

    [Header("Shop Status")]
    public bool isShopOpen = true;

    [Header("Important Locations")]
    [Tooltip("The waypoint where NPCs stand to order food.")]
    public Transform orderSpot;
    
    [Header("Tables")]
    [Tooltip("Drag all your TableDelivery objects here.")]
    public List<TableDelivery> allTables;
    
    private List<TableDelivery> availableTables = new List<TableDelivery>();
    private bool isOrderSpotTaken = false;

    void Awake()
    {
        // Singleton pattern so other scripts can easily find it
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // All tables are available at start
        availableTables.AddRange(allTables);
    }

    /// <summary>
    /// NPCs call this to check if they can get in line to order.
    /// </summary>
    public bool TryReserveOrderSpot()
    {
        if (!isShopOpen) return false;
        if (isOrderSpotTaken) return false;
        if (availableTables.Count == 0) return false; // Don't let them order if no tables are free!

        isOrderSpotTaken = true;
        return true;
    }

    /// <summary>
    /// Called when the NPC finishes ordering and walks to their table.
    /// </summary>
    public void ReleaseOrderSpot()
    {
        isOrderSpotTaken = false;
    }

    /// <summary>
    /// NPCs call this after ordering to get an assigned table.
    /// </summary>
    public TableDelivery TryGetTable()
    {
        if (availableTables.Count > 0)
        {
            TableDelivery table = availableTables[0];
            availableTables.RemoveAt(0); // Take it out of the available list
            return table;
        }
        return null;
    }

    /// <summary>
    /// Called when the NPC leaves the table.
    /// </summary>
    public void ReleaseTable(TableDelivery table)
    {
        if (!availableTables.Contains(table))
        {
            availableTables.Add(table);
        }
    }
}
