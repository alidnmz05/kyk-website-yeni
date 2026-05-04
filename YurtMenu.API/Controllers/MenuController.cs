using Google.Cloud.Firestore.V1;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using YurtMenu.API.Data;
using YurtMenu.API.DTO;
using YurtMenu.API.Models;
using System.Text;
using System.Security.Cryptography;
using System.Globalization;

namespace YurtMenu.API.Controllers
{


    [ApiController]
    [Route("api/[controller]")]
    public class MenuController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly FirestoreDb _firestore;


        // 0: kahvaltı, 1: yemek
        private static string TopCollection(int mealType) => mealType == 0 ? "breakfast" : "meal";

        private static (string yyyyMM, string yyyyMMdd) SplitDate(string date)
        {
            var d = DateOnly.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            var yyyyMM = $"{d.Year:D4}-{d.Month:D2}";
            var yyyyMMdd = $"{d.Year:D4}-{d.Month:D2}-{d.Day:D2}";
            return (yyyyMM, yyyyMMdd);
        }

        // /{top}/{cityId}/months/{yyyyMM}/days/all  -> TEK DOC
        private DocumentReference DaysSingleDocRef(int cityId, int mealType, string date)
        {
            var (yyyyMM, _) = SplitDate(date);
            var top = TopCollection(mealType);
            return _firestore.Document($"{top}/{cityId}/months/{yyyyMM}/days/all");
        }

        private static string Normalize(string? s) => (s ?? "").Trim().ToLowerInvariant();
        private static string BuildItemId(string first, string second, string third, string fourth)
        {
            var raw = $"{Normalize(first)}|{Normalize(second)}|{Normalize(third)}|{Normalize(fourth)}";
            using var sha = SHA256.Create();
            return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(raw)))[..16];
        }

        private static Dictionary<string, object> ToFsItem(MenuInputDTO p)
        {
            var id = string.IsNullOrWhiteSpace(p.ItemId)
                ? BuildItemId(p.First, p.Second, p.Third, p.Fourth)
                : p.ItemId!;
            return new Dictionary<string, object>
            {
                ["itemId"] = id,
                ["first"] = p.First ?? string.Empty,
                ["firstCalories"] = p.FirstCalories != null ? p.FirstCalories.ToArray() : Array.Empty<int>(),
                ["second"] = p.Second ?? string.Empty,
                ["secondCalories"] = p.SecondCalories != null ? p.SecondCalories.ToArray() : Array.Empty<int>(),
                ["third"] = p.Third ?? string.Empty,
                ["thirdCalories"] = p.ThirdCalories != null ? p.ThirdCalories.ToArray() : Array.Empty<int>(),
                ["fourth"] = p.Fourth ?? string.Empty,
                ["fourthCalories"] = p.FourthCalories != null ? p.FourthCalories.ToArray() : Array.Empty<int>(),
                ["totalCalories"] = p.TotalCalories ?? 0
            };
        }

        private static List<Dictionary<string, object>> MergeItems(
            List<Dictionary<string, object>> existing,
            List<Dictionary<string, object>> incoming,
            int maxItemsPerDay = 30)
        {
            var map = new Dictionary<string, Dictionary<string, object>>();
            foreach (var it in existing)
                if (it.TryGetValue("itemId", out var v) && v is string key && !string.IsNullOrEmpty(key))
                    map[key] = it;

            foreach (var it in incoming)
                if (it.TryGetValue("itemId", out var v) && v is string key && !string.IsNullOrEmpty(key))
                    map[key] = it; // aynı id geldiyse güncelle

            var merged = map.Values.ToList();
            if (merged.Count > maxItemsPerDay)
                merged = merged.Take(maxItemsPerDay).ToList();
            return merged;
        }

        private static List<Dictionary<string, object>> ReadItemsFromDayEntry(object? entry)
        {
            var list = new List<Dictionary<string, object>>();
            if (entry is IDictionary<string, object> dayDict &&
                dayDict.TryGetValue("items", out var itemsObj) &&
                itemsObj is IEnumerable<object> raw)
            {
                foreach (var o in raw)
                {
                    if (o is Dictionary<string, object> d) list.Add(d);
                    else if (o is IDictionary<string, object> id)
                        list.Add(id.ToDictionary(k => k.Key, v => v.Value));
                }
            }
            return list;
        }

        public MenuController(AppDbContext context, FirestoreDb firestore)
        {
            _context = context;
            _firestore = firestore ?? throw new ArgumentNullException(nameof(firestore));

        }

        [Authorize]
        [HttpPost("kaydet")]
        public async Task<IActionResult> Kaydet([FromBody] List<MenuInputDTO> dtoList)
        {
            // 🔹 Log dosya yolu
            var logFile = Path.Combine("C:\\Logs", "MenuFirestoreCityLog.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(logFile)!);

            // 🔹 START
            try
            {
                await System.IO.File.AppendAllTextAsync(logFile,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] START kaydet | count={(dtoList?.Count ?? 0)}{Environment.NewLine}");
            }
            catch { /* file log hatası akışı bozmasın */ }

            if (dtoList == null || dtoList.Count == 0)
            {
                try
                {
                    await System.IO.File.AppendAllTextAsync(logFile,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ABORT  kaydet | empty payload{Environment.NewLine}");
                }
                catch { }
                return BadRequest("Payload boş olamaz.");
            }

            // 1) POSTGRES upsert
            foreach (var dto in dtoList)
            {
                var existing = await _context.Menus
                    .FirstOrDefaultAsync(m =>
                        m.CityId == dto.CityId &&
                        m.Date == dto.Date &&
                        m.MealType == dto.MealType);
                if (existing != null) _context.Menus.Remove(existing);

                _context.Menus.Add(new Menu
                {
                    Date = dto.Date,
                    MealType = dto.MealType,
                    CityId = dto.CityId,
                    First = dto.First,
                    FirstCalories = string.Join(",", dto.FirstCalories ?? new List<int>()),
                    Second = dto.Second,
                    SecondCalories = string.Join(",", dto.SecondCalories ?? new List<int>()),
                    Third = dto.Third,
                    ThirdCalories = string.Join(",", dto.ThirdCalories ?? new List<int>()),
                    Fourth = dto.Fourth,
                    FourthCalories = string.Join(",", dto.FourthCalories ?? new List<int>()),
                    TotalCalories = dto.TotalCalories
                });
            }
            await _context.SaveChangesAsync();

            // 🔹 PG OK
            try
            {
                await System.IO.File.AppendAllTextAsync(logFile,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] OK     postgres upsert | rows={dtoList.Count}{Environment.NewLine}");
            }
            catch { }

            // 2) FIRESTORE tek-doc (days/all) merge
            var monthGroups = dtoList.GroupBy(x =>
            {
                var (yyyyMM, _) = SplitDate(x.Date);
                return new { x.CityId, x.MealType, yyyyMM };
            });

            foreach (var mg in monthGroups)
            {
                // 🔹 TX begin (tek doc)
                try
                {
                    await System.IO.File.AppendAllTextAsync(logFile,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] TX-BEG single-doc | cityId={mg.Key.CityId} mealType={mg.Key.MealType} month={mg.Key.yyyyMM}{Environment.NewLine}");
                }
                catch { }

                var docRef = DaysSingleDocRef(mg.Key.CityId, mg.Key.MealType, $"{mg.Key.yyyyMM}-01");

                var dayGroups = mg.GroupBy(x => SplitDate(x.Date).yyyyMMdd)
                                  .ToDictionary(g => g.Key, g => g.Select(ToFsItem).ToList());

                await _firestore.RunTransactionAsync(async tx =>
                {
                    var snap = await tx.GetSnapshotAsync(docRef);

                    var daysMap = new Dictionary<string, object>();
                    if (snap.Exists && snap.ContainsField("daysMap"))
                    {
                        if (snap.GetValue<object>("daysMap") is IDictionary<string, object> map)
                        {
                            foreach (var kv in map)
                            {
                                if (kv.Value is IDictionary<string, object> d)
                                    daysMap[kv.Key] = d.ToDictionary(k => k.Key, v => v.Value);
                                else if (kv.Value is Dictionary<string, object> dd)
                                    daysMap[kv.Key] = dd;
                            }
                        }
                    }

                    foreach (var kv in dayGroups)
                    {
                        var dateKey = kv.Key;
                        var incomingItems = kv.Value;

                        var existingItems = daysMap.TryGetValue(dateKey, out var dayEntry)
                            ? ReadItemsFromDayEntry(dayEntry)
                            : new List<Dictionary<string, object>>();

                        var merged = MergeItems(existingItems, incomingItems, maxItemsPerDay: 30);

                        daysMap[dateKey] = new Dictionary<string, object>
                        {
                            ["items"] = merged,
                            ["count"] = merged.Count,
                            ["updatedAt"] = Timestamp.FromDateTime(DateTime.UtcNow)
                        };
                    }

                    var baseDoc = new Dictionary<string, object>
                    {
                        ["cityId"] = mg.Key.CityId,
                        ["mealType"] = mg.Key.MealType,
                        ["month"] = mg.Key.yyyyMM,
                        ["daysMap"] = daysMap,
                        ["updatedAt"] = Timestamp.FromDateTime(DateTime.UtcNow)
                    };

                    tx.Set(docRef, baseDoc, SetOptions.Overwrite);
                });

                // 🔹 TX ok
                try
                {
                    await System.IO.File.AppendAllTextAsync(logFile,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] TX-OK  single-doc | cityId={mg.Key.CityId} mealType={mg.Key.MealType} month={mg.Key.yyyyMM}{Environment.NewLine}");
                }
                catch { }
            }

            // 3) FIRESTORE: city-collection / TurkishMonth / (breakfast|dinner)
            var targetCityMap = GetTargetCityMap();
            var cultureTr = new CultureInfo("tr-TR");

            var filtered = dtoList
                .Where(d => targetCityMap.ContainsKey(d.CityId))
                .Where(d => GetMealCollectionName(d.MealType) != null)
                .ToList();

            if (filtered.Count > 0)
            {
                var batch = _firestore.StartBatch();
                var logLines = new List<string>();

                foreach (var dto in filtered)
                {
                    // ✅ Tarihi parse et
                    var dt = DateTime.Parse(dto.Date, CultureInfo.InvariantCulture);

                    var cityCollection = targetCityMap[dto.CityId];
                    var monthDoc = GetTurkishMonthName(dt, cultureTr);
                    var mealCollection = GetMealCollectionName(dto.MealType)!;

                    // ✅ Gün tek hane ise 0’sız, ay her zaman 2 hane
                    var dateStr = $"{dt:yyyy-MM}-{dt.Day}";   // örn: 2025-08-9 veya 2025-08-19

                    var docRef = _firestore
                        .Collection(cityCollection)
                        .Document(monthDoc)
                        .Collection(mealCollection)
                        .Document(dateStr);

                    // 📊 Kalori bilgilerini hazırla (List<int> -> String)
                    var caloriesMap = new Dictionary<string, object?>();
                    bool hasCalories = false;

                    // "main" -> dto.Second (ana yemek)
                    if (dto.SecondCalories != null && dto.SecondCalories.Any(c => c > 0))
                    {
                        caloriesMap["main"] = string.Join(" / ", dto.SecondCalories.Where(c => c > 0));
                        hasCalories = true;
                    }
                    // "soup" -> dto.First (çorba)
                    if (dto.FirstCalories != null && dto.FirstCalories.Any(c => c > 0))
                    {
                        caloriesMap["soup"] = string.Join(" / ", dto.FirstCalories.Where(c => c > 0));
                        hasCalories = true;
                    }
                    // "third" -> dto.Third (pilav/makarna)
                    if (dto.ThirdCalories != null && dto.ThirdCalories.Any(c => c > 0))
                    {
                        caloriesMap["third"] = string.Join(" / ", dto.ThirdCalories.Where(c => c > 0));
                        hasCalories = true;
                    }
                    // "fourth" -> dto.Fourth (içecek/tatlı)
                    if (dto.FourthCalories != null && dto.FourthCalories.Any(c => c > 0))
                    {
                        caloriesMap["fourth"] = string.Join(" / ", dto.FourthCalories.Where(c => c > 0));
                        hasCalories = true;
                    }

                    var data = new Dictionary<string, object?>
                    {
                        ["date"] = dateStr,
                        ["main"] = dto.Second,
                        ["soup"] = dto.First,
                        ["third"] = dto.Third,
                        ["fourth"] = dto.Fourth
                    };

                    // ✅ Kalori varsa ekle (OPSIYONEL)
                    if (hasCalories)
                    {
                        data["calories"] = caloriesMap;
                    }

                    batch.Set(docRef, data, SetOptions.Overwrite);

                    logLines.Add(
                        $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] CITYSET " +
                        $"CityId={dto.CityId} ({cityCollection}), " +
                        $"Date={dateStr}, MealType={dto.MealType} → {cityCollection}/{monthDoc}/{mealCollection} " +
                        $"| main='{dto.Second}', soup='{dto.First}', third='{dto.Third}', fourth='{dto.Fourth}', calories={hasCalories}"
                    );
                }

                await batch.CommitAsync();

                // 🔹 Batch ok + detay satırları
                try
                {
                    await System.IO.File.AppendAllTextAsync(logFile,
                        $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] BATCH-OK city/month/meal | items={filtered.Count}{Environment.NewLine}");
                    await System.IO.File.AppendAllLinesAsync(logFile, logLines); // ✅ tek çağrı, duplicate yok
                }
                catch { }
            }

            // 🔹 END
            try
            {
                await System.IO.File.AppendAllTextAsync(logFile,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] END    kaydet{Environment.NewLine}");
            }
            catch { }

            return Ok(new { message = "✅ Menü kaydedildi (Postgres + Firestore tek-doc/month, daysMap merge + city/month/meal ek yazım)." });
        }

















        /* ----------------- YARDIMCI FONKSİYONLAR ----------------- */

        private static Dictionary<int, string> GetTargetCityMap()
        {
            // plaka -> collection adı (lowercase, Türkçe karakterler dahil)
            return new Dictionary<int, string>
    {
        { 1,  "adana" },
        { 6,  "ankara" },
        { 7,  "antalya" },
        { 16, "bursa" },
        { 17, "çanakkale" },
        { 19, "çorum" },
        { 20, "denizli" },
        { 26, "eskişehir" },
        { 32, "isparta" },
        { 33, "mersin" },      // (İçel) Mersin'in plakası 33
        { 34, "istanbul" },
        { 35, "izmir" },
        { 36, "kars" },
        { 38, "kayseri" },
        { 41, "kocaeli" },
        { 42, "konya" },
        { 44, "malatya" },
        { 48, "muğla" },
        { 50, "nevşehir" },
        { 53, "rize" },
        { 54, "sakarya" },
        { 59, "tekirdağ" },
        { 63, "şanlıurfa" },
        { 70, "karaman" },
        { 78, "karabük" },
        { 81, "düzce" },
        { 10, "balıkesir" },
        { 43, "kütahya" },
        { 28, "giresun" },
        { 25, "erzurum" },
        { 55, "samsun" },
        { 61, "trabzon" }


    };
        }

        private static string GetTurkishMonthName(DateTime date, System.Globalization.CultureInfo cultureTr)
        {
            // "Ağustos" gibi baş harfi büyük ay adı
            var monthLower = date.ToString("MMMM", cultureTr); // "ağustos"
            return cultureTr.TextInfo.ToTitleCase(monthLower); // "Ağustos"
        }

        /// <summary>
        /// Breakfast/Dinner koleksiyon adı. Diğer öğünler için null döndürür (yazmayız).
        /// Burayı kendi MealType tipine göre uyarlayabilirsin.
        /// </summary>
        // 0 = kahvaltı, 1 = akşam, diğerleri yazılmasın
        private static string? GetMealCollectionName(int mealType) => mealType switch
        {
            0 => "breakfast",
            1 => "dinner",
            _ => null
        };

        // İleride string/enum gelirse de çalışsın diye:
        private static string? GetMealCollectionName(string? mealType)
        {
            var s = (mealType ?? "").ToLowerInvariant();
            if (s.Contains("kahval") || s.Contains("breakfast")) return "breakfast";
            if (s.Contains("akşam") || s.Contains("aksam") || s.Contains("dinner")) return "dinner";
            return null;
        }









        
        [HttpGet("liste")]
        public async Task<IActionResult> Liste([FromQuery] int cityId, [FromQuery] int mealType)
        {
            var today = DateTime.Today;
            var startOfMonth = new DateTime(today.Year, today.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

            var data = await _context.Menus
                .Where(m => m.CityId == cityId && m.MealType == mealType)
                .ToListAsync();

            var menus = data
                .Where(m =>
                    DateTime.TryParse(m.Date, out var date) &&
                    date >= startOfMonth && date <= endOfMonth)
                .OrderBy(m => DateTime.Parse(m.Date))
                .Select(m => new
                {
                    m.Id,
                    m.Date,
                    m.MealType,
                    m.CityId,
                    m.First,
                    m.FirstCalories,
                    m.Second,
                    m.SecondCalories,
                    m.Third,
                    m.ThirdCalories,
                    m.Fourth,
                    m.FourthCalories,
                    m.TotalCalories
                })
                .ToList();

            // ✅ LOG yaz
            try
            {
                string logPath = @"C:\Logs\MenuListeLog.txt"; // 📁 Dilediğin path'e yaz
                Directory.CreateDirectory(Path.GetDirectoryName(logPath)!); // klasör yoksa oluştur
                string logText = $"[{DateTime.Now}] cityId: {cityId}, mealType: {mealType}\n" +
                                 JsonConvert.SerializeObject(menus, Formatting.Indented) + "\n\n";

                await System.IO.File.AppendAllTextAsync(logPath, logText);
            }
            catch (Exception ex)
            {
                // Log hatası varsa yut, çünkü response'ı bozmasın
            }

            return Ok(menus);
        }



        [HttpGet("liste-mobil")]
        public async Task<IActionResult> ListeMobil([FromQuery] int cityId, [FromQuery] int mealType)
        {
            // 🔐 Mobil uygulama için özel x-api-key kontrolü
            if (!Request.Headers.TryGetValue("x-api-key", out var apiKey) || apiKey != "MOBIL_KYK_KEY_2025")
            {
                return Unauthorized("Geçersiz mobil api anahtarı");
            }

            var today = DateTime.Today;
            var startOfMonth = new DateTime(today.Year, today.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

            var data = await _context.Menus
                .Where(m => m.CityId == cityId && m.MealType == mealType)
                .ToListAsync();

            var menus = data
                .Where(m =>
                    DateTime.TryParse(m.Date, out var date) &&
                    date >= startOfMonth && date <= endOfMonth)
                .OrderBy(m => DateTime.Parse(m.Date))
                .Select(m => new
                {
                    m.Id,
                    m.Date,
                    m.MealType,
                    m.CityId,
                    m.First,
                    m.FirstCalories,
                    m.Second,
                    m.SecondCalories,
                    m.Third,
                    m.ThirdCalories,
                    m.Fourth,
                    m.FourthCalories,
                    m.TotalCalories
                })
                .ToList();

            return Ok(menus);
        }





        [Authorize]
        [HttpGet("bekleyen-yemekler")]
        public IActionResult BekleyenYemekler()
        {
            var foodSet = _context.FoodDictionary
                .Select(f => f.Name.ToLower().Trim())
                .ToHashSet();

            var candidates = new HashSet<string>();

            var allMenus = _context.Menus.ToList();

            foreach (var menu in allMenus)
            {
                void CheckField(string? names, string? calories)
                {
                    if (string.IsNullOrWhiteSpace(names)) return;

                    var yemekler = names.Split('/')
                        .Select(x => x.Trim())
                        .Where(x => !string.IsNullOrEmpty(x))
                        .ToList();

                    var kaloriList = (calories ?? "").Split(',')
                        .Select(x => x.Trim())
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .ToList();

                    for (int i = 0; i < yemekler.Count; i++)
                    {
                        // Eğer karşılığı olan kalori eksikse (yok veya 0) ve dictionary’de de yoksa
                        bool calorieMissing = i >= kaloriList.Count || kaloriList[i] == "0";
                        bool notInDictionary = !foodSet.Contains(yemekler[i].ToLower());

                        if (calorieMissing && notInDictionary)
                        {
                            candidates.Add(yemekler[i]);
                        }
                    }
                }

                CheckField(menu.First, menu.FirstCalories);
                CheckField(menu.Second, menu.SecondCalories);
                CheckField(menu.Third, menu.ThirdCalories);
                CheckField(menu.Fourth, menu.FourthCalories);
            }

            var result = candidates.Select(name => new { name }).ToList();

            return Ok(result);
        }



        [Authorize]
        [HttpPost("eksik-guncelle")]
        public async Task<IActionResult> EksikGuncelle([FromBody] FoodDictionary input)
        {
            // 1️⃣ Eğer sözlükte yoksa, ekle
            var exists = await _context.FoodDictionary
                .AnyAsync(f => f.Name.ToLower() == input.Name.ToLower());

            if (!exists)
            {
                _context.FoodDictionary.Add(input);
                await _context.SaveChangesAsync();
            }

            var allMenus = await _context.Menus.ToListAsync();
            string target = input.Name.Trim();

            void UpdateField(ref string? names, ref string? calories)
            {
                if (string.IsNullOrWhiteSpace(names)) return;

                var yemekler = names.Split('/').Select(x => x.Trim()).ToList();
                var kaloriList = (calories ?? "").Split(',').ToList();

                for (int i = 0; i < yemekler.Count; i++)
                {
                    if (yemekler[i].Equals(target, StringComparison.OrdinalIgnoreCase))
                    {
                        if (kaloriList.Count <= i)
                        {
                            while (kaloriList.Count <= i) kaloriList.Add("0");
                        }
                        kaloriList[i] = input.Calories.ToString();
                    }
                }

                calories = string.Join(",", kaloriList);
            }

            foreach (var menu in allMenus)
            {
                bool changed = false;

                var f1 = menu.First; var c1 = menu.FirstCalories;
                UpdateField(ref f1, ref c1);
                if (f1 != menu.First || c1 != menu.FirstCalories)
                {
                    menu.FirstCalories = c1;
                    changed = true;
                }

                var f2 = menu.Second; var c2 = menu.SecondCalories;
                UpdateField(ref f2, ref c2);
                if (f2 != menu.Second || c2 != menu.SecondCalories)
                {
                    menu.SecondCalories = c2;
                    changed = true;
                }

                var f3 = menu.Third; var c3 = menu.ThirdCalories;
                UpdateField(ref f3, ref c3);
                if (f3 != menu.Third || c3 != menu.ThirdCalories)
                {
                    menu.ThirdCalories = c3;
                    changed = true;
                }

                var f4 = menu.Fourth; var c4 = menu.FourthCalories;
                UpdateField(ref f4, ref c4);
                if (f4 != menu.Fourth || c4 != menu.FourthCalories)
                {
                    menu.FourthCalories = c4;
                    changed = true;
                }

                if (changed)
                {
                    // toplam kaloriyi de güncelle
                    var calories = new List<string> { c1, c2, c3, c4 }
                        .SelectMany(s => s?.Split(',') ?? Array.Empty<string>())
                        .Select(s => int.TryParse(s, out var v) ? v : 0)
                        .ToList();
                    menu.TotalCalories = calories.Sum();
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "✅ Güncelleme tamamlandı." });
        }










    }
}