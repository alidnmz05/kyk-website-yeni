using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YurtMenu.API.Data;
using YurtMenu.API.Models;

namespace YurtMenu.API.Controllers
{
    [ApiController]
    [Route("api/food")]
    public class FoodDictionaryController : ControllerBase
    {
        private readonly AppDbContext _context;

        public FoodDictionaryController(AppDbContext context)
        {
            _context = context;
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var list = await _context.FoodDictionary.ToListAsync();
                return Ok(list);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Yemekler alınırken hata oluştu.", error = ex.Message });
            }
        }

        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            try
            {
                var food = await _context.FoodDictionary.FindAsync(id);
                if (food == null)
                    return NotFound(new { message = "Yemek bulunamadı.", id });

                return Ok(food);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Yemek getirilirken hata oluştu.", error = ex.Message });
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] FoodDictionary model)
        {
            try
            {
                if (await _context.FoodDictionary.AnyAsync(f => f.Name == model.Name))
                    return Conflict(new { message = "Aynı isimde bir yemek zaten mevcut.", name = model.Name });

                _context.FoodDictionary.Add(model);
                await _context.SaveChangesAsync();
                return Ok(model);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Yemek eklenirken hata oluştu.", error = ex.Message });
            }
        }

        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] FoodDictionary model)
        {
            try
            {
                var existing = await _context.FoodDictionary.FindAsync(id);
                if (existing == null)
                    return NotFound(new { message = "Güncellenecek yemek bulunamadı.", id });

                existing.Name = model.Name;
                existing.Calories = model.Calories;
                existing.MealType = model.MealType;

                await _context.SaveChangesAsync();
                return Ok(existing);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Yemek güncellenirken hata oluştu.", error = ex.Message });
            }
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var existing = await _context.FoodDictionary.FindAsync(id);
                if (existing == null)
                    return NotFound(new { message = "Silinecek yemek bulunamadı.", id });

                _context.FoodDictionary.Remove(existing);
                await _context.SaveChangesAsync();
                return Ok(new { message = "Yemek silindi.", id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Yemek silinirken hata oluştu.", error = ex.Message });
            }
        }
    }
}
