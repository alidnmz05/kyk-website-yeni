using YurtMenu.API.Data;   // AppDbContext için
using Microsoft.EntityFrameworkCore;

namespace YurtMenu.API.Helpers
{
    public static class StaticFoodCache
    {
        public static Dictionary<string, int> Foods = new();

        public static void LoadFromDatabase(AppDbContext db)
        {
            try
            {
                // Verileri önce veritabanından ham haliyle çekiyoruz (.AsEnumerable())
                // Çünkü PostgreSQL .ToLowerInvariant() ve .Trim() fonksiyonlarını SQL'e çeviremez.
                var allFoods = db.FoodDictionary
                    .AsNoTracking()
                    .AsEnumerable() 
                    .ToList();

                // Duplicate'leri uygulama tarafında (bellekte) groupla ve ilk değeri al
                Foods = allFoods
                    .GroupBy(f => (f.Name ?? "").ToLowerInvariant().Trim())
                    .ToDictionary(
                        g => g.Key,
                        g => g.First().Calories
                    );

                // Duplicate kontrolü ve loglama
                var duplicates = allFoods
                    .GroupBy(f => (f.Name ?? "").ToLowerInvariant().Trim())
                    .Where(g => g.Count() > 1)
                    .ToList();

                if (duplicates.Any())
                {
                    Console.WriteLine($"\n⚠️  UYARI: {duplicates.Count} adet tekrarlayan yemek bulundu:\n");
                    foreach (var dup in duplicates)
                    {
                        var items = dup.ToList();
                        Console.WriteLine($"📌 '{dup.Key}':");
                        foreach (var item in items)
                        {
                            Console.WriteLine($"   - ID: {item.Id}, Kalori: {item.Calories}, Orijinal: '{item.Name}'");
                        }
                        Console.WriteLine();
                    }
                    Console.WriteLine($"ℹ️  Not: Her duplicate için ilk kayıt kullanılıyor.\n");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ HATA: StaticFoodCache veritabanından yüklenirken hata oluştu: {ex.Message}");
                Foods = new Dictionary<string, int>(); 
            }
        }

        public static int GetCalories(string foodName)
        {
            return Foods.TryGetValue(foodName.ToLowerInvariant().Trim(), out var cal)
                ? cal
                : 0;
        }
    }
}