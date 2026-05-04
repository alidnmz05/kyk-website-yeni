using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc;

namespace YurtMenu.API.Controllers
{
    [ApiController]
    [Route("api/mobile/cities")]
    public class MobileCityController : ControllerBase
    {
        private readonly FirestoreDb _firestore;
        private const string CitiesCollection = "cities";

        public MobileCityController(FirestoreDb firestore)
        {
            _firestore = firestore;
        }

        // GET: api/mobile/cities
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var snapshot = await _firestore.Collection(CitiesCollection).GetSnapshotAsync();

            var cities = snapshot.Documents.Select(doc => new
            {
                Id = doc.Id,
                Name = doc.ContainsField("name") ? doc.GetValue<string>("name") : ""
            });

            return Ok(cities);
        }

        // POST: api/mobile/cities
        // Body: { "id": 6, "name": "Ankara" }
        [HttpPost]
        public async Task<IActionResult> AddOrUpdate([FromBody] CityDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest("Name is required.");

            var docRef = _firestore.Collection(CitiesCollection)
                                    .Document(dto.Id.ToString());

            await docRef.SetAsync(new { name = dto.Name });
            return Ok(new { message = "City saved", dto.Id, dto.Name });
        }

        // DELETE: api/mobile/cities/6
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _firestore.Collection(CitiesCollection)
                            .Document(id.ToString())
                            .DeleteAsync();

            return Ok(new { message = "City deleted", id });
        }
    }

    public class CityDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }
}
