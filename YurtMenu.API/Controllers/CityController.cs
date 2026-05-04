using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YurtMenu.API.Data;
using YurtMenu.API.Models;

namespace YurtMenu.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CityController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CityController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Tüm şehirleri listele (ID'ye göre sıralı)
        /// GET: api/city
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllCities()
        {
            try
            {
                var cities = await _context.Cities
                    .OrderBy(c => c.Id)
                    .Select(c => new
                    {
                        c.Id,
                        c.Name
                    })
                    .ToListAsync();

                return Ok(cities);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Şehirler yüklenirken hata oluştu.", error = ex.Message });
            }
        }

        /// <summary>
        /// ID'ye göre tek şehir getir
        /// GET: api/city/{id}
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCityById(int id)
        {
            try
            {
                var city = await _context.Cities.FindAsync(id);

                if (city == null)
                {
                    return NotFound(new { message = "Şehir bulunamadı." });
                }

                return Ok(new { city.Id, city.Name });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Şehir yüklenirken hata oluştu.", error = ex.Message });
            }
        }

        /// <summary>
        /// Yeni şehir ekle (Manuel ID ile)
        /// POST: api/city
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> AddCity([FromBody] AddCityRequest request)
        {
            try
            {
                // ID validation
                if (request.Id <= 0)
                {
                    return BadRequest(new { message = "Geçerli bir şehir ID'si girin (1 veya daha büyük)." });
                }

                // Name validation
                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return BadRequest(new { message = "Şehir adı boş olamaz." });
                }

                // Şehir adı length kontrolü
                if (request.Name.Trim().Length > 100)
                {
                    return BadRequest(new { message = "Şehir adı 100 karakterden uzun olamaz." });
                }

                // ID duplicate kontrolü
                var existingCityById = await _context.Cities
                    .FirstOrDefaultAsync(c => c.Id == request.Id);

                if (existingCityById != null)
                {
                    return BadRequest(new { message = $"Bu ID ({request.Id}) zaten kullanılıyor." });
                }

                // Name duplicate kontrolü
                var existingCityByName = await _context.Cities
                    .FirstOrDefaultAsync(c => c.Name.ToLower() == request.Name.Trim().ToLower());

                if (existingCityByName != null)
                {
                    return BadRequest(new { message = "Bu şehir adı zaten mevcut." });
                }

                // Yeni şehir oluştur
                var city = new City
                {
                    Id = request.Id,
                    Name = request.Name.Trim()
                };

                _context.Cities.Add(city);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Şehir başarıyla eklendi.",
                    city = new { city.Id, city.Name }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Şehir eklenirken hata oluştu.", error = ex.Message });
            }
        }

        /// <summary>
        /// Şehir güncelle
        /// PUT: api/city/{id}
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCity(int id, [FromBody] UpdateCityRequest request)
        {
            try
            {
                // Name validation
                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return BadRequest(new { message = "Şehir adı boş olamaz." });
                }

                // Şehir adı length kontrolü
                if (request.Name.Trim().Length > 100)
                {
                    return BadRequest(new { message = "Şehir adı 100 karakterden uzun olamaz." });
                }

                // Şehir var mı kontrolü
                var city = await _context.Cities.FindAsync(id);
                if (city == null)
                {
                    return NotFound(new { message = "Şehir bulunamadı." });
                }

                // Name duplicate kontrolü (kendisi hariç)
                var existingCity = await _context.Cities
                    .FirstOrDefaultAsync(c => c.Name.ToLower() == request.Name.Trim().ToLower() && c.Id != id);

                if (existingCity != null)
                {
                    return BadRequest(new { message = "Bu şehir adı zaten kullanılıyor." });
                }

                // Güncelle
                city.Name = request.Name.Trim();
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Şehir başarıyla güncellendi.",
                    city = new { city.Id, city.Name }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Şehir güncellenirken hata oluştu.", error = ex.Message });
            }
        }

        /// <summary>
        /// Şehir sil
        /// DELETE: api/city/{id}
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCity(int id)
        {
            try
            {
                var city = await _context.Cities.FindAsync(id);
                if (city == null)
                {
                    return NotFound(new { message = "Şehir bulunamadı." });
                }

                // İlişkili veriler var mı kontrolü (opsiyonel)
                // Örneğin: Bu şehre bağlı yurtlar varsa silinmesin
                // var hasRelatedData = await _context.Yurts.AnyAsync(y => y.CityId == id);
                // if (hasRelatedData)
                // {
                //     return BadRequest(new { message = "Bu şehre bağlı yurtlar olduğu için silinemez." });
                // }

                _context.Cities.Remove(city);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Şehir başarıyla silindi." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Şehir silinirken hata oluştu.", error = ex.Message });
            }
        }

        /// <summary>
        /// Şehir adına göre arama
        /// GET: api/city/search?name={searchTerm}
        /// </summary>
        [HttpGet("search")]
        public async Task<IActionResult> SearchCities([FromQuery] string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    return BadRequest(new { message = "Arama terimi boş olamaz." });
                }

                var cities = await _context.Cities
                    .Where(c => c.Name.ToLower().Contains(name.ToLower()))
                    .OrderBy(c => c.Id)
                    .Select(c => new
                    {
                        c.Id,
                        c.Name
                    })
                    .ToListAsync();

                return Ok(cities);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Arama yapılırken hata oluştu.", error = ex.Message });
            }
        }

        /// <summary>
        /// Şehir istatistikleri
        /// GET: api/city/stats
        /// </summary>
        [HttpGet("stats")]
        public async Task<IActionResult> GetCityStats()
        {
            try
            {
                var totalCities = await _context.Cities.CountAsync();
                var lastAddedCity = await _context.Cities
                    .OrderByDescending(c => c.Id)
                    .FirstOrDefaultAsync();

                return Ok(new
                {
                    totalCities,
                    lastAddedCity = lastAddedCity != null ? new { lastAddedCity.Id, lastAddedCity.Name } : null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "İstatistikler yüklenirken hata oluştu.", error = ex.Message });
            }
        }
    }

    #region DTO Classes

    /// <summary>
    /// Şehir ekleme için DTO
    /// </summary>
    public class AddCityRequest
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// Şehir güncelleme için DTO
    /// </summary>
    public class UpdateCityRequest
    {
        public string Name { get; set; } = string.Empty;
    }

    #endregion
}