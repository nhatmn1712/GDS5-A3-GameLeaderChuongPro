using UnityEngine;

public static class PlayerInventory
{
    // Bowl the player is currently holding
    public static string carryingBowl = "";

    // Walk-in customer order active
    public static bool hasActiveOrder = false;

    // Grab delivery: what food the player needs to cook and deliver
    public static string activeDeliveryFood = "";
}
