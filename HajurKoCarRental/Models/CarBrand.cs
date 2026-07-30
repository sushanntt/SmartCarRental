using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HajurKoCarRental.Models
{
    public class CarBrand
    {

        public Guid Id { get; set; }

        [Required]
        public string BrandName { get; set; }

        public string PhotoUrl { get; set; }
        [NotMapped]
        public IFormFile Photo { get; set; }
    }
}