public class MenuInputDTO
{
    public string Date { get; set; } = string.Empty; // 🔹 Tarih string

    public int MealType { get; set; }

    public string? First { get; set; }
    public List<int>? FirstCalories { get; set; }

    public string? Second { get; set; }
    public List<int>? SecondCalories { get; set; }

    public string? Third { get; set; }
    public List<int>? ThirdCalories { get; set; }

    public string? Fourth { get; set; }
    public List<int>? FourthCalories { get; set; }

    public int? TotalCalories { get; set; }

    public int CityId { get; set; }

    // ✅ yeni (opsiyonel): stabil anahtar
    public string? ItemId { get; set; }

}
