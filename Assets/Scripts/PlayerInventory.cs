using UnityEngine;

public static class PlayerInventory
{
    // Lưu trữ chuỗi tên tô đang cầm (VD: "HuTieu", "BunBoKhongHanh")
    // Static giúp biến này tồn tại xuyên suốt các Scene
    public static string carryingBowl = "";

    // Kiểm tra xem có khách hàng nào đang đợi món không
    public static bool hasActiveOrder = false;
}
