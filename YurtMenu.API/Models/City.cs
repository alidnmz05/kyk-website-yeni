using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YurtMenu.API.Models
{
    public class City
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)] // 👈 Manuel ID için gerekli
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        // Şehir adı örneğin: "İstanbul", "Ankara"
        public string Name { get; set; } = string.Empty;

        // Bu şehirle ilişkili tüm menüler (1:N)
        public ICollection<Menu>? Menus { get; set; }
    }
}