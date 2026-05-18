using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CookingIngredient : MonoBehaviour
{
    public enum Type { EmptyBowlStack, FoodIngredient, HoldingPlace }
    
    [Header("Settings")]
    public Type interactType;
    
    [Header("If Type = Empty Bowl Stack")]
    [Tooltip("Chồng tô này sẽ lấy ra tô Trắng hay tô Vàng?")]
    public BowlType bowlTypeToSpawn;

    [Header("If Type = Food Ingredient")]
    [Tooltip("Exact name must match the recipe. Valid names: Shrimp, TrungCut, Pork, HuTieu, Scallion, NuocLeoHuTieu, Beef, Blood Pudding, Bun, NuocLeoBunBo")]
    public string ingredientName;

    void OnMouseDown()
    {
        if (CookingManager.Instance == null) return;

        if (interactType == Type.EmptyBowlStack)
        {
            CookingManager.Instance.OnBowlStackClicked(bowlTypeToSpawn);
        }
        else if (interactType == Type.FoodIngredient)
        {
            CookingManager.Instance.OnIngredientClicked(ingredientName);
        }
        else if (interactType == Type.HoldingPlace)
        {
            CookingManager.Instance.OnHoldingPlaceClicked();
        }
    }
}
