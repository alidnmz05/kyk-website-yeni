using YurtMenu.API.Models;

public class Menu
{
    public int Id { get; set; }

    public string Date { get; set; } = string.Empty; // 🔹 Tarih string

    public int MealType { get; set; }

    public string? First { get; set; }
    public string? FirstCalories { get; set; }

    public string? Second { get; set; }
    public string? SecondCalories { get; set; }

    public string? Third { get; set; }
    public string? ThirdCalories { get; set; }

    public string? Fourth { get; set; }
    public string? FourthCalories { get; set; }

    public int? TotalCalories { get; set; }

    public int CityId { get; set; }
    public City? City { get; set; }
}
